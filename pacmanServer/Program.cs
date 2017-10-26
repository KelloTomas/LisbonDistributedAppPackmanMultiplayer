using CommonTypes;
using Shared;
using System;
using System.Collections.Generic;
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
		List<Obsticle> obsticles = new List<Obsticle>();
		string _pId;
		int _maxNumPlayers;
		int _numPlayers = 0;
		Game _game = new Game();
		private TcpChannel channel;
		int _speed = 5;
		System.Timers.Timer _timer;
		ServiceServer serviceServer;
		private List<Client> _clientsList = new List<Client>();
		private Dictionary<string, IServiceClient> _clientsDict = new Dictionary<string, IServiceClient>();
		private Position monsterSize = new Position() { X = 30, Y = 30 };
		#endregion

		static void Main(string[] args)
		{
			new Program().Init(args);
		}

		void Init(string[] args)
		{
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
			Console.WriteLine($"Starting server on {myURL}, link: {link}");
			RemotingServices.Marshal(serviceServer, link);
			Console.WriteLine("Write \"q\" to quit");
			while (Console.ReadLine() != "q") ;
		}

		private void _timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			/* update characters positions */
			foreach (Character player in _game.Players.Values)
			{
				switch (player.Direction)
				{
					case Direction.Down:
						player.Y += _speed;
						break;
					case Direction.Up:
						player.Y -= _speed;
						break;
					case Direction.Left:
						player.X -= _speed;
						break;
					case Direction.Right:
						player.X += _speed;
						break;
					case Direction.No:
					default:
						break;
				}
			}

			//move ghosts
			foreach(Character monster in _game.Monsters)
			{
				switch(monster.Direction)
				{
					case Direction.Up:
						monster.Y -= _speed;
						break;
					case Direction.Down:
						monster.Y += _speed;
						break;
					case Direction.Right:
						monster.X += _speed;
						break;
					case Direction.Left:
						monster.X -= _speed;
						break;
				}
			}
			foreach (Character monster in _game.Monsters)
			{
				if(IntersectsWithObsticle(monster))
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
			/*
			_game.Monsters.ElementAt(2).X += _game.Monsters.ElementAt(2).Direction == Direction.Left ? -_speed : _speed;

			_game.Monsters.ElementAt(0).X += _speed;
			_game.Monsters.ElementAt(0).Y += ghost3y;

			if (_game.Monsters.ElementAt(0).X < boardLeft ||
				_game.Monsters.ElementAt(0).X > boardRight ||
				(_game.Monsters.ElementAt(0).Bounds.IntersectsWith(pictureBox1.Bounds)) ||
				(_game.Monsters.ElementAt(0).Bounds.IntersectsWith(pictureBox2.Bounds)) ||
				(_game.Monsters.ElementAt(0).Bounds.IntersectsWith(pictureBox3.Bounds)) ||
				(_game.Monsters.ElementAt(0).Bounds.IntersectsWith(pictureBox4.Bounds)))
			{
				ghost3x = -ghost3x;
			}
			if (_game.Monsters.ElementAt(0).Top < boardTop || _game.Monsters.ElementAt(0).Top + _game.Monsters.ElementAt(0).Height > boardBottom - 2)
			{
				ghost3y = -ghost3y;
			}
			*/


			/* if the red ghost hits the picture box 4 then wereverse the speed
			if (redGhost.Bounds.IntersectsWith(pictureBox1.Bounds))
				ghost1 = -ghost1;
			*/
			/* if the red ghost hits the picture box 3 we reverse the speed
			else if (redGhost.Bounds.IntersectsWith(pictureBox2.Bounds))
				ghost1 = -ghost1;
			*/
			/* if the yellow ghost hits the picture box 1 then wereverse the speed
			if (yellowGhost.Bounds.IntersectsWith(pictureBox3.Bounds))
				ghost2 = -ghost2;
			*/
			/* if the yellow chost hits the picture box 2 then wereverse the speed
			else if (yellowGhost.Bounds.IntersectsWith(pictureBox4.Bounds))
				ghost2 = -ghost2;
			*/
			//moving ghosts and bumping with the walls end
			//for loop to check walls, ghosts and points

			/* ToDo player contact	
			foreach (Control x in this.Controls)
			{
				// checking if the player hits the wall or the ghost, then game is over
				if (x is PictureBox && x.Tag == "wall" || x.Tag == "ghost")
				{
					if (((PictureBox)x).Bounds.IntersectsWith(pacman.Bounds))
					{
						pacman.Left = 0;
						pacman.Top = 25;
						label2.Text = "GAME OVER";
						label2.Visible = true;
						timer1.Stop();
					}
				}
				if (x is PictureBox && x.Tag == "coin")
				{
					if (((PictureBox)x).Bounds.IntersectsWith(pacman.Bounds))
					{
						this.Controls.Remove(x);
						score++;
						//TODO check if all coins where "eaten"
						if (score == total_coins)
						{
							//pacman.Left = 0;
							//pacman.Top = 25;
							label2.Text = "GAME WON!";
							label2.Visible = true;
							timer1.Stop();
						}
					}
				}
			}
			*/
			foreach (var client in _clientsDict)
			{
				client.Value.UpdateGame(_game);
			}
			foreach(var player in _game.Players.Values)
			{
				player.Direction = Direction.No;
			}
		}

		private bool IntersectsWithObsticle(Character monster)
		{
			Position p = new Position() { X = monster.X, Y = monster.Y };
			if (IsPointInAnyObsticle(p))
				return true;
			p.X += monsterSize.X;
			if (IsPointInAnyObsticle(p))
				return true;
			p.Y += monsterSize.Y;
			if (IsPointInAnyObsticle(p))
				return true;
			p.X -= monsterSize.X;
			if (IsPointInAnyObsticle(p))
				return true;
			return false;
		}

		private bool IsPointInAnyObsticle(Position p)
		{
			foreach(Obsticle obsticle in obsticles)
			{
				if (IsPointInObsticle(p, obsticle))
					return true;
			}
			return false;
		}

		private bool IsPointInObsticle(Position position, Obsticle obsticle)
		{
			if (position.X > obsticle.Corner1.X &&
				position.X < obsticle.Corner1.X + obsticle.Corner2.X &&
				position.Y > obsticle.Corner1.Y &&
				position.Y < obsticle.Corner1.Y + obsticle.Corner2.Y)
				return true;
			return false;
		}

		public bool RegisterPlayer(string pId, string clientURL)
		{
			if (_numPlayers == _maxNumPlayers)
			{
				Console.WriteLine($"SERVER FULL and playeer {pId} with url {clientURL} try to connect");
				return false;
			}
			Console.WriteLine($"Playeer {pId} with url {clientURL} is connected");

			_game.Players.Add(pId, new Character() { X = 8, Y = 40 * (_game.Players.Count + 1) });

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
	}
}
