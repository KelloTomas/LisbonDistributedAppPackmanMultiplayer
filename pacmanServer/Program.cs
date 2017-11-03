using CommonTypes;
using Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Timers;

namespace pacmanServer
{
	internal class Program
	{
		#region private fields...
		private Delays delays = new Delays();
		private Obsticle Board;
		private string _pId;
		private int _maxNumPlayers;
		private int _numPlayers = 0;
		private Game _game = new Game();
		private TcpChannel channel;
		private const int _speed = 5;
		private System.Timers.Timer _timer;
		private ServiceServer serviceServer;
		private List<Client> _clientsList = new List<Client>();
		private Dictionary<string, IServiceClient> _clientsDict = new Dictionary<string, IServiceClient>();
		private delegate void UpdateGameDelegate();
		#endregion

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
			_timer = new System.Timers.Timer() { AutoReset = true, Enabled = false, Interval = mSec };
			_timer.Elapsed += Timer_Elapsed;

			lock (this)
			{
				/* set channel */
				channel = new TcpChannel(int.Parse(Shared.Shared.ParseUrl(URLparts.Port, myURL)));
				ChannelServices.RegisterChannel(channel, true);
				/*set service */
				serviceServer = new ServiceServer(this, delays);
				string link = Shared.Shared.ParseUrl(URLparts.Link, myURL);
				Console.WriteLine("Starting server on " + myURL + ", link: " + link);
				RemotingServices.Marshal(serviceServer, link);
			}
			while (true)
			{
				Console.ReadLine();
			}
		}
		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			Console.WriteLine("Round: " + _game.RoundId + " Updating...");
			lock (this)
			{
				_game.RoundId++;
				ICollection<CharacterWithScore> x = _game.Players.Values;
				UpdateCharactersPosition(_game.Players.Values);
				UpdateCharactersPosition(_game.Monsters);

				foreach (var player in _game.Players)
				{
					if (CheckIntersectionWithObsticleOrBorder(player.Value, CharactersSize.Player))
					{
						UpdateCharactersPosition(player.Value, true); // move player back
					}
					else
					{
						if (CheckIntersectionWithMonster(player.Value))
						{
							player.Value.X = -CharactersSize.Player;
							player.Value.Y = 0;
							delays.SendWithDelay(player.Key, (Action<bool>)_clientsDict[player.Key].GameEnded, new object[] { false });
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
				foreach (Character monster in _game.Monsters)
				{
					if (CheckIntersectionWithObsticleOrBorder(monster, CharactersSize.Monster))
					{
						switch (monster.Direction)
						{
							case Direction.Up:
								monster.Direction = Direction.Down;
								break;
							case Direction.Down:
								monster.Direction = Direction.Up;
								break;
							case Direction.Right:
								monster.Direction = Direction.Left;
								break;
							case Direction.Left:
								monster.Direction = Direction.Right;
								break;
						}
					}
				}
			}
			SendUpdatedGameToClients(_game);
			foreach (var player in _game.Players.Values)
			{
				player.Direction = Direction.No;
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
			foreach (var client in _clientsDict)
			{
				delays.SendWithDelay(client.Key, (Action<Game>)client.Value.GameUpdate, new object[] { game });
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
			foreach (Character character in characters)
			{
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
				case Direction.Down:
					character.Y += _speed * (back ? -1 : 1);
					break;
				case Direction.Up:
					character.Y -= _speed * (back ? -1 : 1);
					break;
				case Direction.Left:
					character.X -= _speed * (back ? -1 : 1);
					break;
				case Direction.Right:
					character.X += _speed * (back ? -1 : 1);
					break;
				case Direction.No:
				default:
					break;
			}
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

		private Rectangle b1 = new Rectangle();
		private Rectangle b2 = new Rectangle();
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

		public void RegisterPlayer(string pId, string clientURL)
		{
			if (_numPlayers == _maxNumPlayers)
			{
				Console.WriteLine("SERVER FULL and playeer " + pId + " with url " + clientURL + " try to connect");
			}
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
			}
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

			_game.Monsters.Add(new Character() { X = 301, Y = 72, Direction = Direction.Left });
			_game.Monsters.Add(new Character() { X = 180, Y = 73, Direction = Direction.Left });
			_game.Monsters.Add(new Character() { X = 221, Y = 273, Direction = Direction.Left });

			ThreadStart ts = new ThreadStart(this.BroadcastGameStart);
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
			foreach (KeyValuePair<string, IServiceClient> client in _clientsDict)
			{
				delays.SendWithDelay(client.Key, (Action<string, List<Client>, Game>)client.Value.GameStarted, new object[] { _pId, _clientsList, _game });
			}
			_timer.Start();
		}

		public void SetMove(string pId, int roundId, Direction direction)
		{
			_game.Players[pId].Direction = direction;
		}
		public void Crash()
		{
			Environment.Exit(0);
		}
		public void GlobalStatus()
		{
			Console.WriteLine("Global Status: online");
		}
		public void InjectDelay(string pId, int mSecDelay)
		{
			delays.AddDelay(pId, mSecDelay);
		}
	}
}
