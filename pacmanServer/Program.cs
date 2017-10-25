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
			Console.WriteLine($"Starting server on {myURL}, link: {link}");
			RemotingServices.Marshal(serviceServer, link);
			Console.WriteLine("Write \"q\" to quit");
			while (Console.ReadLine() != "q") ;
		}

		private void _timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			Console.WriteLine("timer elapsed");
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
				player.Direction = Direction.No;
			}

			//move ghosts
			_game.Monsters.ElementAt(1).X += _game.Monsters.ElementAt(1).Direction == Direction.Left? -_speed : _speed;
			_game.Monsters.ElementAt(2).X += _game.Monsters.ElementAt(2).Direction == Direction.Left ? -_speed : _speed;

			/* if the red ghost hits the picture box 4 then wereverse the speed
			if (redGhost.Bounds.IntersectsWith(pictureBox1.Bounds))
				ghost1 = -ghost1;
			*/
			if (_game.Monsters.ElementAt(1).X < 20)
				_game.Monsters.ElementAt(1).Direction = Direction.Right;
			/* if the red ghost hits the picture box 3 we reverse the speed
			else if (redGhost.Bounds.IntersectsWith(pictureBox2.Bounds))
				ghost1 = -ghost1;
			*/
			if (_game.Monsters.ElementAt(1).X < 200)
				_game.Monsters.ElementAt(1).Direction = Direction.Left;
			/* if the yellow ghost hits the picture box 1 then wereverse the speed
			if (yellowGhost.Bounds.IntersectsWith(pictureBox3.Bounds))
				ghost2 = -ghost2;
			*/
			if (_game.Monsters.ElementAt(1).X < 20)
				_game.Monsters.ElementAt(1).Direction = Direction.Right;
			/* if the yellow chost hits the picture box 2 then wereverse the speed
			else if (yellowGhost.Bounds.IntersectsWith(pictureBox4.Bounds))
				ghost2 = -ghost2;
			*/
			if (_game.Monsters.ElementAt(1).X < 200)
				_game.Monsters.ElementAt(1).Direction = Direction.Left;
			//moving ghosts and bumping with the walls end
			//for loop to check walls, ghosts and points

			/* ToDo
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
			Console.WriteLine("Game updated");
			foreach (IServiceClient client in _clientsDict.Values)
			{
				Console.WriteLine("sending game to clinet");
				client.UpdateGame(_game);
			}
		}

		public bool RegisterPlayer(string pId, string clientURL)
		{
			if (_numPlayers == _maxNumPlayers)
			{
				Console.WriteLine($"SERVER FULL and playeer {pId} with url {clientURL} try to connect");
				return false;
			}
			Console.WriteLine($"Playeer {pId} with url {clientURL} is connected");
			/* get service */
			_clientsList.Add(new Client(pId, clientURL));
			_clientsDict.Add(pId, (IServiceClient)Activator.GetObject(
				typeof(IServiceClient),
				clientURL));

			if (++_numPlayers == _maxNumPlayers)
			{
				Console.WriteLine("Game starting");

				ThreadStart ts = new ThreadStart(this.BroadcastGameStart);
				Thread t = new Thread(ts);
				t.Start();
			}
			return true;
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
