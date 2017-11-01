using CommonTypes;
using Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace pacmanServer
{
	public class Program
	{
		#region private fields...
		Obsticle Board;
		int _delay = 0;
		string _pId;
		int _maxNumPlayers;
		int _numPlayers = 0;
		Game _game = new Game();
		private TcpChannel channel;
		const int _speed = 5;
		System.Timers.Timer _timer;
		ServiceServer serviceServer;
		private List<Client> _clientsList = new List<Client>();
		private Dictionary<string, int> _delays = new Dictionary<string, int>();
		private Dictionary<string, int> _delays_count = new Dictionary<string, int>();
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
			_timer.Elapsed += _timer_Elapsed;

			/* set channel */
			channel = new TcpChannel(int.Parse(Shared.Shared.ParseUrl(URLparts.Port, myURL)));
			ChannelServices.RegisterChannel(channel, true);

			/*set service */
			serviceServer = new ServiceServer(this);
			string link = Shared.Shared.ParseUrl(URLparts.Link, myURL);
			Console.WriteLine("Starting server on " + myURL + ", link: " + link);
			RemotingServices.Marshal(serviceServer, link);
			Console.WriteLine("Write \"q\" to quit");
			while (Console.ReadLine() != "q") ;
		}
		private void _timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			ICollection<CharacterWithScore> x = _game.Players.Values;
			UpdateCharactersPosition(_game.Players.Values);
			UpdateCharactersPosition(_game.Monsters);
			
			foreach (var player in _game.Players)
			{
				if (CheckIntersectionWithObsticleOrBorder(player.Value))
				{
					UpdateCharactersPosition(player.Value, true); // move player back
				}
				else
				{
					if (CheckIntersectionWithMonster(player.Value))
					{
						player.Value.X = -CharactersSize.Player;
						player.Value.Y = 0;
						_clientsDict[player.Key].CrashWithMonster();
					}
					else
						if(CheckIntersectionWithCoins(player.Value))
						{
							player.Value.Score++;
						}
				}
			}

			foreach (Character monster in _game.Monsters)
			{
				if (CheckIntersectionWithObsticleOrBorder(monster))
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
			UpdateGame();
			foreach(var player in _game.Players.Values)
			{
				player.Direction = Direction.No;
			}
		}

		private bool CheckIntersectionWithMonster(CharacterWithScore player)
		{
			foreach(Character monster in _game.Monsters)
			{
				if (CheckIntersection(player, monster))
				{
					return true;
				}
			}
			return false;
		}


		private void UpdateGame()
		{
			int sec = 0;
			foreach (var client in _clientsDict)
			{
				if (_delays.TryGetValue(client.Key, out sec))
				{
					//_delays_count.TryGetValue(client.Key, out count);
					//count = (count - (int)_timer.Interval) % sec;
					_delays_count[client.Key] = (_delays_count[client.Key] - (int)_timer.Interval) % sec;
					Console.WriteLine(_delays_count[client.Key]);
					
					if (_delays_count[client.Key] > _timer.Interval) {
						continue;
					}
					_delays_count[client.Key] = sec;
					// Console.WriteLine(_delays_count[client.Key]);
				}
				client.Value.UpdateGame(_game);
			}
		}

		private bool CheckIntersectionWithCoins(CharacterWithScore player)
		{
			//ToDo
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

		private bool CheckIntersectionWithObsticleOrBorder(Character character)
		{
			foreach (Obsticle obsticle in _game.Obsticles)
			{
				if (CheckIntersection(character, CharactersSize.Monster, obsticle))
				{
					return true;
				}
				if (CheckIntersectionWithBorder(character))
				{
					return true;
				}
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
					character.Y += _speed * (back?-1:1);
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

		private bool CheckIntersectionWithBorder(Character character)
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
		private bool CheckIntersection(Character player, Character monster)
		{
			b2.X = monster.X;
			b2.Y = monster.Y;
			b2.Width = CharactersSize.Monster;
			b2.Height = CharactersSize.Monster;
			return CheckIntersection(player, CharactersSize.Player, b2);
		}
		private bool CheckIntersection(Character character, int characterSize, Obsticle obsticle)
		{
			b2.X = obsticle.Corner1.X;
			b2.Y = obsticle.Corner1.Y;
			b2.Width = obsticle.Corner2.X;
			b2.Height = obsticle.Corner2.Y;
			return CheckIntersection(character, characterSize, b2);
		}
		private bool CheckIntersection(Character character, int characterSize, Rectangle obsticle)
		{
			b1.X = character.X;
			b1.Y = character.Y;
			b1.Width = characterSize;
			b1.Height = characterSize;
			bool intersec = b1.IntersectsWith(b2);
			return intersec;
		}

		public bool RegisterPlayer(string pId, string clientURL)
		{
			if (_numPlayers == _maxNumPlayers)
			{
				Console.WriteLine("SERVER FULL and playeer " + pId + " with url " + clientURL + " try to connect");
				return false;
			}
			Console.WriteLine("Playeer " + pId + " with url " + clientURL + " is connected");

			_game.Players.Add(pId, new CharacterWithScore() { X = 8, Y = 40 * (_game.Players.Count + 1) });

			/* get service */
			_clientsList.Add(new Client(pId, clientURL));
			_clientsDict.Add(pId, (IServiceClient)Activator.GetObject(
				typeof(IServiceClient),
				clientURL));

			if (++_numPlayers == _maxNumPlayers)
			{
				StartGame();
			}
			return true;
		}

		private void StartGame()
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
					_game.Coins.Add(new Position() { X = 8 + i * 40, Y = 40 + j * 40 });
				}
			}

			_game.Monsters.Add(new Character() { X = 301, Y = 72, Direction = Direction.Left });
			_game.Monsters.Add(new Character() { X = 180, Y = 73, Direction = Direction.Left });
			_game.Monsters.Add(new Character() { X = 221, Y = 273, Direction = Direction.Left });

			ThreadStart ts = new ThreadStart(this.BroadcastGameStart);
			Thread t = new Thread(ts);
			t.Start();
		}

		private void BroadcastGameStart()
		{
			foreach (IServiceClient client in _clientsDict.Values)
			{
				client.GameStarted(_pId, _clientsList, _game);
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
		public void InjectDelay(string PID, int mSecDelay)
		{
			_delays.Add(PID, mSecDelay);
			_delays_count.Add(PID, mSecDelay);
			_delay = mSecDelay;
			
		}
	}
}
