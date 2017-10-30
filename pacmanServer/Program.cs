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
			Console.WriteLine("Starting server on " + myURL + ", link: " + link);
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
                foreach (Obsticle obsticle in _game.Obsticles)
                {
				    if(CheckIntersection(monster, Shared.CharactersSize.Monster, obsticle))
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
			foreach (var client in _clientsDict)
			{
				client.Value.UpdateGame(_game);
			}
			foreach(var player in _game.Players.Values)
			{
				player.Direction = Direction.No;
			}
		}

        private System.Windows.Forms.PictureBox b1 = new System.Windows.Forms.PictureBox();
        private System.Windows.Forms.PictureBox b2 = new System.Windows.Forms.PictureBox();
        private bool CheckIntersection(Character monster, int size, Obsticle obsticle)
        {
            b1.Left = monster.X;
            b1.Top = monster.Y;
            b1.Width = size;
            b1.Height = size;
            b2.Left = obsticle.Corner1.X;
            b2.Top = obsticle.Corner1.Y;
            b2.Width = obsticle.Corner2.X;
            b2.Height = obsticle.Corner2.Y;
            bool intersec =  b1.Bounds.IntersectsWith(b2.Bounds);
            if (intersec)
            {
                Console.WriteLine(b1.Left + " " + b1.Top + " " + b1.Width + " " + b1.Height);
                Console.WriteLine(b2.Left + " " + b2.Top + " " + b2.Width + " " + b2.Height);
            }
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
