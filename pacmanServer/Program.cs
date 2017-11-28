using CommonTypes;
using Shared;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Timers;
using System.Diagnostics;
using System.IO;

namespace pacmanServer
{
	internal class Program
	{
		#region private fields...
		private Queue client_queue = new Queue();
		private Delays delays = new Delays();
		private Frozens _frozens = new Frozens();
		private Obsticle Board;
		private string _pId;
		private int _maxNumPlayers;
		private int _numPlayers = 0;
		private Game _game = new Game();
		private TcpChannel channel;
		private const int _speed = 5;
		private System.Timers.Timer _timer;
		private bool full = false;
		private ServiceServer serviceServer;
		private Rectangle b1 = new Rectangle();
		private Rectangle b2 = new Rectangle();
		private int clientId;
		private bool _secondary = false;
		private object[] _last = null;
		private List<Client> _clientsList = new List<Client>();
		private Dictionary<string, IServiceClient> _clientsDict = new Dictionary<string, IServiceClient>();
		private delegate void UpdateGameDelegate(KeyValuePair<string, IServiceClient> client, Game game);
		UpdateGameDelegate asyncGameUpdate;
		private delegate void WaitEnqueuedClientsDelegate();
		WaitEnqueuedClientsDelegate waitClients;
		private delegate object[] WaitPrimaryServerFailDelegate();
		WaitPrimaryServerFailDelegate waitFail;
		IServiceServer _primary_server = null;
		#endregion
		#region start program...
		static void Main(string[] args)
		{
			new Program().Init(args);
		}
		void Init(string[] args)
		{
			Board = new Obsticle();
			Board.Corner2.X = 320;
			Board.Corner2.Y = 280;
			Board.Corner1.X = 0;
			Board.Corner1.Y = 40;

			_pId = args[0];
			string myURL = args[1];
			int mSec = int.Parse(args[2]);
			_maxNumPlayers = int.Parse(args[3]);

			if (args.Length == 5 && args[4].Equals("secondary"))
			{
				_secondary = true;
				waitFail = GetGamePrimaryServer;
				_primary_server = (IServiceServer)Activator.GetObject(typeof(IServiceServer), myURL);
				_timer = new System.Timers.Timer() { AutoReset = true, Enabled = false, Interval = 3000 };
				_timer.Elapsed += Timer_Elapsed_Secondary;
				_timer.Start();
				lock (this)
				{
					Console.WriteLine("Secondary Server Mode : Waiting for primary server to fail...");
					Monitor.Wait(this);
					Console.WriteLine("_last size  = "+ _last.Length);
					_game =(Game) _last[0];
					_clientsDict = (Dictionary<string, IServiceClient>)_last[1];
					_clientsList = (List<Client>)_last[2];
					/*_frozens = (Frozens )_last[3];
					delays = (Delays)_last[4];*/
					// do this to unregister the channel
					IChannel[] regChannels = ChannelServices.RegisteredChannels;
					foreach(IChannel ch in regChannels)
					{
						//Console.WriteLine("CHANNEL : " + ch.ChannelName);
						if (ch.ChannelName.Equals("tcp"))
						{
							ChannelServices.UnregisterChannel(ch);
						}
					}

					//channel = new TcpChannel(int.Parse(Shared.Shared.ParseUrl(URLparts.Port, myURL)));
					//ChannelServices.UnregisterChannel(channel);

				}
				_timer.Stop();
				_timer.Close();

			}
			waitClients = WaitEnqueuedClients;
			_timer = new System.Timers.Timer() { AutoReset = true, Enabled = false, Interval = mSec };
			_timer.Elapsed += Timer_Elapsed;
			if(_secondary)
			{
				Console.WriteLine("Starting...");
				_timer.Start();
				_secondary = false;
			}
			waitClients.BeginInvoke(null, null);

			lock (this)
			{
				/* set channel */
				try
				{
					channel = new TcpChannel(int.Parse(Shared.Shared.ParseUrl(URLparts.Port, myURL)));
					ChannelServices.RegisterChannel(channel, false);
				}
				catch
				{
					Console.WriteLine("CATCH!!!!");
					
					//ChannelServices.UnregisterChannel(channel);
				}
				/*set service */
				serviceServer = new ServiceServer(this, _frozens);
				string link = Shared.Shared.ParseUrl(URLparts.Link, myURL);
				Console.WriteLine("Starting server on " + myURL + ", link: " + link);
				RemotingServices.Marshal(serviceServer, link);
			}
			string programArguments = _pId + " " + myURL + " " + mSec + " " + _maxNumPlayers + " secondary";
			string ExeFileNameServer = "pacmanServer.exe";
			Process.Start(Path.Combine("..\\..\\..\\pacmanServer\\bin\\Debug", ExeFileNameServer),
												programArguments);
			while (true)
			{
				Console.ReadLine();
			}
		}
		#endregion

		#region RoundRobin program flow...
		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			//Console.WriteLine("Flora du skrata du");
			_game.RoundId++;
			Console.WriteLine("Round: " + _game.RoundId + " Updating...");
			UpdateGame();
			SendUpdatedGameToClients(_game);
		}
		private void Timer_Elapsed_Secondary(object sender, ElapsedEventArgs e)
		{
			if (_secondary)
			{
				Console.Write("Response ");
				IAsyncResult asyncResult = waitFail.BeginInvoke(null, null);
				if (!asyncResult.AsyncWaitHandle.WaitOne(2000)) //timeout replicate new server
				{
					lock (this)
					{
						Monitor.PulseAll(this);
					}

				}
				else
				{
					_last = waitFail.EndInvoke(asyncResult);					
				}
			}
		}
		#endregion

		#region Server service...

		public object[] ImAlive()
		{
			return new object[] { _game, _clientsDict, _clientsList/*, _frozens, delays*/};
		}
		public void SetMove(string pId, int roundId, Direction direction)
		{
			_game.Players[pId].Direction = direction;
			Console.WriteLine("Player " + pId + " : " + direction);
		}

		public void RegisterPlayer(string pId, string clientURL)
		{
			if ((_numPlayers < _maxNumPlayers )&& (full == true))
			{
				lock (this)
				{
					lock (client_queue.SyncRoot)
					{
						Console.WriteLine("SERVER1 FULL and playeer " + pId + " with url " + clientURL + " try to connect");
						object[] toEnqueue = { pId, clientURL };
						client_queue.Enqueue(toEnqueue);
					}
					Monitor.Pulse(this);
				}
			}
			else
			{
				if (_numPlayers == _maxNumPlayers)
				{
					lock (this)
					{
						lock (client_queue.SyncRoot)
						{
							Console.WriteLine("SERVER2 FULL and playeer " + pId + " with url " + clientURL + " try to connect");
							object[] toEnqueue = { pId, clientURL };
							client_queue.Enqueue(toEnqueue);
						}
					}
				}
				else
				{
					lock (this)
					{
						Console.WriteLine("Playeer " + pId + " with url " + clientURL + " is connected");

						_game.Players.Add(pId, new CharacterWithScore() { X = 8, Y = 40 * (_game.Players.Count + 1) });

						/* get service */
						_clientsList.Add(new Client(pId, clientURL));
						_clientsDict.Add(pId, (IServiceClient)Activator.GetObject(
							typeof(IServiceClient),
							clientURL));
						if (++_numPlayers == _maxNumPlayers)
						{
							GameStart();
						}
						else
						{
							Console.WriteLine("Waiting for " + (_maxNumPlayers - _numPlayers) + " more players");
						}
						if (_numPlayers == _maxNumPlayers)
						{
							Console.WriteLine("SERVER FULL!");
							full = true;
						}
					}
				}
			}
		}
		#endregion

		#region Control service...
		public void Crash()
		{
			//ChannelServices.UnregisterChannel(channel);
			Environment.Exit(1);
		}

		public void InjectDelay(string pId, int mSecDelay)
		{
			delays.AddDelay(pId, mSecDelay);
		}

		public void Freez()
		{
			_frozens.Freez();
		}
		public void UnFreez()
		{
			_frozens.UnFreez();
		}
		#endregion

		#region Log local and global state...
		internal void GlobalStatus()
		{
			LogLocalGlobal.GlobalStatus(_game);
		}

		internal string LocalState()
		{
			return LogLocalGlobal.LocalState(_game, _pId);
		}
		#endregion

		#region private methods...
		private object[] GetGamePrimaryServer()
		{
			return _primary_server.ImAlive();
		}
		private void UpdateGame()
		{
			lock (this)
			{
				UpdateCharactersPosition(_game.Players.Values);
				UpdateCharactersPosition(_game.Monsters);
				foreach (var player in _game.Players)
				{
					if (player.Value.state == State.Playing)
					{
						if (CheckIntersectionWithBorder(player.Value))
						{
							Console.WriteLine("Player :" + player.Key + " has direction " + player.Value.Direction.ToString());
							UpdateCharactersPosition(player.Value, true); // move player back
							Console.WriteLine("Player :" + player.Key + " has position " + player.Value.X + " , " + player.Value.Y);
						}
						else
						{
							if (CheckIntersectionWithMonster(player.Value) || CheckIntersectionWithObsticle(player.Value, CharactersSize.Player))
							{
								player.Value.X = -CharactersSize.Player;
								player.Value.Y = 0;
								player.Value.state = State.Dead;
							}
							else
							{
								if (CheckIntersectionWithCoins(player.Value))
								{
									player.Value.Score++;
									if (_game.Coins.Count == 0)
									{
										foreach (var client in _clientsDict)
										{
											delays.SendWithDelay(client.Key, (Action<bool>)client.Value.GameEnded, new object[] { true });
										}
									}
								}
							}
						}
					}
				}
				foreach (Character monster in _game.Monsters)
				{
					if (CheckIntersectionWithObsticleOrBorder(monster, CharactersSize.Monster))
					{
						switch (monster.Direction)
						{
							case Direction.UP:
								monster.Direction = Direction.DOWN;
								break;
							case Direction.DOWN:
								monster.Direction = Direction.UP;
								break;
							case Direction.RIGHT:
								monster.Direction = Direction.LEFT;
								break;
							case Direction.LEFT:
								monster.Direction = Direction.RIGHT;
								break;
						}
					}
				}
			}
		}

		private bool CheckIntersectionWithMonster(CharacterWithScore player)
		{
			foreach (Character monster in _game.Monsters)
			{
				if (CheckIntersection(player, CharactersSize.Player, monster, CharactersSize.Monster))
				{
					return true;
				}
			}
			return false;
		}

		private void SendUpdatedGameToClients(Game game)
		{
			asyncGameUpdate = AsyncGameUpdate;
			foreach (var client in _clientsDict)
			{
				AsyncGameUpdate(client, game); // try to use begin invoke
			}
		}
		private void BroadcastClientDisconnect(string pid)
		{
			clientId = 0;
			foreach (KeyValuePair<string, IServiceClient> client in _clientsDict)
			{
				if (client.Key.Equals(pid))
				{
					continue;
				}
				delays.SendWithDelay(client.Key, (Action<string>)client.Value.ClientDisconnect, new object[] { pid });
			}
		}
		private void BroadcastClientConnect(string pid, string URL)
		{
			clientId = 0;
			foreach (KeyValuePair<string, IServiceClient> client in _clientsDict)
			{
				if (client.Key.Equals(pid))
				{
					continue;
				}
				delays.SendWithDelay(client.Key, (Action<string, string>)client.Value.ClientConnect, new object[] { pid , URL});
			}
		}
		private void AsyncGameUpdate(KeyValuePair<string, IServiceClient> client, Game game)
		{
			IAsyncResult asyncResult;
			asyncResult = delays.SendWithDelay(client.Key, (Action<Game>)client.Value.GameUpdate, new object[] { game });
			if (!asyncResult.AsyncWaitHandle.WaitOne(5000, false)) // timeout 5 seconds
			{
				lock (this)
				{
                    CharacterWithScore player;
					if (_game.Players.TryGetValue(client.Key, out player))
					{
						player.X = -CharactersSize.Player;
						player.Y = 0;
						player.state = State.Disconnected;
						//_game.Players.Remove(client.Key);
					}
					if (_clientsDict.ContainsKey(client.Key))
					{
						Console.WriteLine("Timeout! Player " + client.Key + " disconnected.");
						_clientsDict.Remove(client.Key);
						foreach (var c in _clientsList)
						{

							if (c.PId.Equals(client.Key))
							{
								_clientsList.Remove(c);
								break;
							}
						}
						clientId--;
						_numPlayers--;
						BroadcastClientDisconnect(client.Key);
						if(client_queue.Count > 0)
							Monitor.Pulse(this);
						Console.WriteLine("PULSEEEEEE");
					}
				}
			}
		}
		private void WaitEnqueuedClients()
		{
			lock (this)
			{
				while (true)
				{
					Console.WriteLine("Waiting....");
					Monitor.Wait(this);
					Console.WriteLine("CONNECTING NEW PLAYER....");
					NewPlayerConnect();
				}
			}
		}
		private void NewPlayerConnect()
		{
			object[] obj = null;
			lock (client_queue.SyncRoot)
			{
				if(client_queue.Count > 0)
					obj = (object[])client_queue.Dequeue();
			}
			lock (this)
			{
				if (obj != null)
				{
					Console.WriteLine((string)obj[0] + "   ,         " + (string)obj[1]);
					_clientsList.Add(new Client((string)obj[0], (string)obj[1]));
					IServiceClient service = (IServiceClient)Activator.GetObject(typeof(IServiceClient), (string)obj[1]);
					_clientsDict.Add((string)obj[0], service);
					foreach (var client in _game.Players)
					{
						if (client.Value.state == State.Disconnected)
						{
							_game.Players.Remove(client.Key);
							_game.Players.Add((string)obj[0], new CharacterWithScore());
							break;
						}
					}
                    CharacterWithScore player;
					if (_game.Players.TryGetValue((string)obj[0], out player))
					{
						player.X = 8;
						player.Y = 40;
						player.state = State.Playing;
						_numPlayers++;
						delays.SendWithDelay((string)obj[0], (Action<string, int, List<Client>, Game>)service.GameStarted, new object[] { _pId, clientId++, _clientsList, _game });
						BroadcastClientConnect((string)obj[0], (string)obj[1]);
					}
					else
					{
						Console.WriteLine("Someone took your spot while atempting to connect");
					}
				}
			}
		}
		private bool CheckIntersectionWithCoins(CharacterWithScore player)
		{
			for (int i = 0; i < _game.Coins.Count; i++)
			{
				if (CheckIntersection(player, CharactersSize.Player, new Obsticle(_game.Coins.ElementAt(i), new Position(CharactersSize.coin))))
				{
					_game.Coins.RemoveAt(i);
					return true;
				}
			}
			return false;
		}

		private bool CheckIntersectionWithObsticle(Position character, int characterSize)
		{
			foreach (Obsticle obsticle in _game.Obsticles)
			{
				if (CheckIntersection(character, characterSize, obsticle))
				{
					return true;
				}
			}
			return false;
		}

		private bool CheckIntersectionWithObsticleOrBorder(Position character, int characterSize)
		{
			foreach (Obsticle obsticle in _game.Obsticles)
			{
				if (CheckIntersection(character, characterSize, obsticle))
				{
					return true;
				}
			}
			if (CheckIntersectionWithBorder(character))
			{
				return true;
			}
			return false;
		}

		private void UpdateCharactersPosition(ICollection<CharacterWithScore> characters)
		{
			foreach (CharacterWithScore character in characters)
			{
				if(character.state == State.Playing)
					UpdateCharactersPosition(character);
			}
		}

		private void UpdateCharactersPosition(ICollection<Character> characters)
		{
			foreach (Character character in characters)
			{
				UpdateCharactersPosition(character);
			}
		}

		private void UpdateCharactersPosition(Character character, bool back = false)
		{
			switch (character.Direction)
			{
				case Direction.DOWN:
					character.Y += _speed * (back ? -1 : 1);
					break;
				case Direction.UP:
					character.Y -= _speed * (back ? -1 : 1);
					break;
				case Direction.LEFT:
					character.X -= _speed * (back ? -1 : 1);
					break;
				case Direction.RIGHT:
					character.X += _speed * (back ? -1 : 1);
					break;
				case Direction.NO:
				default:
					break;
			}
			//Console.WriteLine("Position x , y : " + character.X + " , " + character.Y);
		}

		private bool CheckIntersectionWithBorder(Position character)
		{
			if (character.X < Board.Corner1.X ||
				character.X > Board.Corner1.X + Board.Corner2.X ||
				character.Y < Board.Corner1.Y ||
				character.Y > Board.Corner1.Y + Board.Corner2.Y)
				return true;
			else
				return false;
		}

		private bool CheckIntersection(Position p1, int p1Size, Position p2, int p2Size)
		{
			b2.X = p2.X;
			b2.Y = p2.Y;
			b2.Width = p2Size;
			b2.Height = p2Size;
			return CheckIntersection(p1, p1Size, b2);
		}

		private bool CheckIntersection(Position character, int characterSize, Obsticle obsticle)
		{
			b2.X = obsticle.Corner1.X;
			b2.Y = obsticle.Corner1.Y;
			b2.Width = obsticle.Corner2.X;
			b2.Height = obsticle.Corner2.Y;
			return CheckIntersection(character, characterSize, b2);
		}

		private bool CheckIntersection(Position character, int characterSize, Rectangle obsticle)
		{
			b1.X = character.X;
			b1.Y = character.Y;
			b1.Width = characterSize;
			b1.Height = characterSize;
			bool intersec = b1.IntersectsWith(b2);
			return intersec;
		}

		private void GameStart()
		{
			Console.WriteLine("Game starting");
			_game.Obsticles.Add(new Obsticle() { Corner1 = new Position() { X = 88, Y = 40 }, Corner2 = new Position() { X = 15, Y = 95 } });
			_game.Obsticles.Add(new Obsticle() { Corner1 = new Position() { X = 248, Y = 40 }, Corner2 = new Position() { X = 15, Y = 95 } });
			_game.Obsticles.Add(new Obsticle() { Corner1 = new Position() { X = 128, Y = 240 }, Corner2 = new Position() { X = 15, Y = 95 } });
			_game.Obsticles.Add(new Obsticle() { Corner1 = new Position() { X = 288, Y = 240 }, Corner2 = new Position() { X = 15, Y = 95 } });
			for (int i = 0; i < 9; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					Position coin = new Position() { X = 8 + i * 40, Y = 40 + j * 40 };
					if (!CheckIntersectionWithObsticleOrBorder(coin, CharactersSize.coin))
					{
						if (!CheckIntersectionWithPlayer(coin, CharactersSize.coin))
						{
							_game.Coins.Add(coin);
						}
					}
				}
			}

			_game.Monsters.Add(new Character() { X = 301, Y = 72, Direction = Direction.LEFT });
			_game.Monsters.Add(new Character() { X = 180, Y = 73, Direction = Direction.LEFT });
			_game.Monsters.Add(new Character() { X = 221, Y = 273, Direction = Direction.LEFT });

			ThreadStart ts = new ThreadStart(BroadcastGameStart);
			Thread t = new Thread(ts);
			t.Start();
		}

		private bool CheckIntersectionWithPlayer(Position p, int pSize)
		{
			foreach (Position player in _game.Players.Values)
			{
				if (CheckIntersection(player, CharactersSize.Player, p, pSize))
					return true;
			}
			return false;
		}

		private void BroadcastGameStart()
		{
			clientId = 0;
			foreach (KeyValuePair<string, IServiceClient> client in _clientsDict)
			{
				delays.SendWithDelay(client.Key, (Action<string, int, List<Client>, Game>)client.Value.GameStarted, new object[] { _pId, clientId++, _clientsList, _game });
			}
			_timer.Start();
		}
		#endregion
	}
}
