using Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace pacmanClient {
    public partial class Form1 : Form {
		#region private...
		private string filename = null;
		private Direction _direction = Direction.No;
		private List<PictureBox> players = new List<PictureBox>();
		private List<PictureBox> monsters = new List<PictureBox>();
		private List<PictureBox> coins = new List<PictureBox>();


		private int _roundId = 0;
		private Game _game;
		private State _state = State.Playing;
		private ServiceClientWithState serviceClient;
		private TcpChannel channel;
		private System.Timers.Timer _timer;
		private static IServiceServer server;
		private string _pId;
		private Dictionary<string, IServiceClientWithState> _clients = new Dictionary<string, IServiceClientWithState>();

		#endregion

		#region constructor...
		public Form1(string[] args) {
			InitializeComponent();
            label2.Visible = false;
            label1.Visible = false;
			/*
			Console.WriteLine("Hello");
			DrawNewCharacterToGame(Controls, Properties.Resources.red_guy);
			DrawNewCharacterToGame(Controls, Properties.Resources.red_guy).Left = 300;
			*/
			_pId = args[0];
			string myURL = args[1];
			string serverPID = args[2];
			string serverURL = args[3];
			int mSec = int.Parse(args[4]);
			if(args.Count() == 6)
			{
				filename = args[5];
			}

			_timer = new System.Timers.Timer() { Interval = mSec, AutoReset = true, Enabled = false };
			_timer.Elapsed += Timer_Tick;
			string tmp = Shared.Shared.ParseUrl(URLparts.Port, myURL);

			/* set channel */
			channel = new TcpChannel(int.Parse(tmp));
			ChannelServices.RegisterChannel(channel, true);

			/*set service */
			serviceClient = new ServiceClientWithState(_pId, this);
			RemotingServices.Marshal(serviceClient, Shared.Shared.ParseUrl(URLparts.Link, myURL));

			/* get service */
			server = (IServiceServer)Activator.GetObject(
				typeof(IServiceServer),
				serverURL);
			try
			{
				if (!server.RegisterPlayer(_pId, myURL))
				{
					throw new Exception();
				//ToDo cant connect, server full
				}
			}
			catch
			{
				Console.WriteLine("Cant connect to server");
			}
		}
		#endregion

		#region controller handler
		private void KeyIsDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Left) {
				_direction = Direction.Left;
            }
            if (e.KeyCode == Keys.Right)
			{
				_direction = Direction.Right;
			}
            if (e.KeyCode == Keys.Up)
			{
				_direction = Direction.Up;
			}
            if (e.KeyCode == Keys.Down)
			{
				_direction = Direction.Down;
			}
            if (e.KeyCode == Keys.Enter) {
                    tbMsg.Enabled = true; tbMsg.Focus();
               }
        }

        private void KeyIsUp(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Left
            || e.KeyCode == Keys.Right
            || e.KeyCode == Keys.Up
            || e.KeyCode == Keys.Down)
			{
				_direction = Direction.No;
			}
        }

        private void Timer_Tick(object sender, EventArgs e) {
			if (_state == State.Dead)
			{
				_timer.Stop();
				_timer.Dispose();
			}
			else
			{
				server.SetMove(_pId, _roundId++, _direction);
			}
        }

        private void TbMsg_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
				foreach (IServiceClient client in _clients.Values)
				{
					client.MessageReceive(_pId, tbMsg.Text);
				}
                tbChat.Text += "\r\n" + tbMsg.Text;
				tbMsg.Clear();
				tbMsg.Enabled = false;
				Focus();
            }
        }
		#endregion

		#region Client service...
		public void GameStarted(string serverPId, List<Client> clients, Game game)
		{
            Console.WriteLine("game started");
			_game = game;

			BeginInvoke(new MethodInvoker(delegate
			{
				foreach (var obsticle in game.Obsticles)
				{
					DrawObsticle(Controls, obsticle);
				}
			}));
			_timer.Start();
			foreach (Client client in clients)
			{
				if (client.PId == _pId)
					continue;
				_clients.Add(client.PId, (IServiceClientWithState)Activator.GetObject(
				typeof(IServiceClient),
				client.URL));
			}
		}

		private void RemoveCharacterFromForm(PictureBox picture)
		{
			picture.Dispose();
		}

		private PictureBox DrawNewCharacterToGame(Control.ControlCollection Controls, Bitmap picture, int size)
		{
			PictureBox ghost = new PictureBox();

			((ISupportInitialize)(ghost)).BeginInit();
			ghost.BackColor = Color.Transparent;
			ghost.Image = picture;
			ghost.Size = new Size(size, size);
			ghost.SizeMode = PictureBoxSizeMode.Zoom;
			/*
			ghost.Location = new Point(180, 73);
			ghost.Name = "redGhost";
			ghost.TabIndex = 1;
			ghost.TabStop = false;
			ghost.Tag = "ghost";
			*/

			Controls.Add(ghost);
			((ISupportInitialize)(ghost)).EndInit();
			return ghost;
		}

		void DrawObsticle(Control.ControlCollection Controls, Obsticle o)
		{
			PictureBox obsticle = new PictureBox();
			((ISupportInitialize)(obsticle)).BeginInit();
			obsticle.BackColor = Color.MidnightBlue;
			obsticle.Location = new Point(o.Corner1.X, o.Corner1.Y);
			obsticle.Size = new Size(o.Corner2.X, o.Corner2.Y);
			/*
			obsticle.TabIndex = 0;
			obsticle.Name = "pictureBox1";
			obsticle.TabStop = false;
			obsticle.Tag = "wall";
			*/
			Controls.Add(obsticle);
			((ISupportInitialize)(obsticle)).EndInit();
		}

		public void UpdateGame(Game game)
		{
			_game = game;
			int i;
			for (i = 0; i < game.Players.Count; i++)
			{
				if (i >= players.Count)
					BeginInvoke(new MethodInvoker(delegate
					{
						players.Add(DrawNewCharacterToGame(Controls, Properties.Resources.Right, Shared.CharactersSize.Player));
						UpdatePlayerPosition(game, i);
					}));
				else
					UpdatePlayerPosition(game, i);
			}
			while (players.Count > i)
			{
				Console.WriteLine("Removing character");
				RemoveCharacterFromForm(players.ElementAt(i));
				players.RemoveAt(i);
			}
			for (i = 0; i < game.Monsters.Count; i++)
			{
				if (i >= monsters.Count)
					BeginInvoke(new MethodInvoker(delegate
					{
						monsters.Add(DrawNewCharacterToGame(Controls, Properties.Resources.red_guy, Shared.CharactersSize.Monster));
					}));
				monsters.ElementAt(i).Left = game.Monsters.ElementAt(i).X;
				monsters.ElementAt(i).Top = game.Monsters.ElementAt(i).Y;
			}
			while (monsters.Count > i)
			{
				Console.WriteLine("Removing monster");
				RemoveCharacterFromForm(monsters.ElementAt(i));
				monsters.RemoveAt(i);
			}

			if (coins.Count != game.Coins.Count)
			{
				foreach(var coin in coins)
				{
					RemoveCharacterFromForm(coin);
				}
				coins.Clear();
				BeginInvoke(new MethodInvoker(delegate
				{
					for (i = 0; i < game.Coins.Count; i++)
					{
						coins.Add(DrawNewCharacterToGame(Controls, Properties.Resources.coint2, Shared.CharactersSize.coin));
						coins.ElementAt(i).Left = game.Coins.ElementAt(i).X;
						coins.ElementAt(i).Top = game.Coins.ElementAt(i).Y;
					}
				}));
			}
		}

		private void UpdatePlayerPosition(Game game, int i)
		{
			players.ElementAt(i).Left = game.Players.ElementAt(i).Value.X;
			players.ElementAt(i).Top = game.Players.ElementAt(i).Value.Y;
            //Console.WriteLine(game.Players.ElementAt(i).Value.Direction);
			switch (game.Players.ElementAt(i).Value.Direction)
			{
				case Direction.Up:
					players.ElementAt(i).Image = Properties.Resources.Up;
					break;
				case Direction.Down:
					players.ElementAt(i).Image = Properties.Resources.Down;
					break;
				case Direction.Left:
					players.ElementAt(i).Image = Properties.Resources.Left;
					break;
				case Direction.Right:
					players.ElementAt(i).Image = Properties.Resources.Right;
					break;
			}
		}

		public void MessageReceive(string pId, string msg)
		{
			BeginInvoke(new MethodInvoker(delegate
			{
				tbChat.Text += pId + ": " + msg + "\r\n";
			}));
		}

		internal void Crash()
		{
			_state = State.Dead;
		}
		#endregion

		#region Control service...
		internal void GlobalStatus()
		{
			foreach(var client in _clients)
			{
				Console.WriteLine("client: " + client.Key + " is in state " + (client.Value == null?"offline":"online"));
			}
		}
		internal void InjectDelay()
		{
			throw new NotImplementedException();
		}
		internal void Freez()
		{
			throw new NotImplementedException();
		}
		internal void UnFreez()
		{
			throw new NotImplementedException();
		}
		internal string LocalState()
		{
			string output = "";
			foreach (var monster in _game.Monsters)
			{
				output += "M, " + monster.X + ", " + monster.Y + "\n\r";
			}
			foreach (var player in _game.Players)
			{
				if (player.Key == _pId)
					output += _pId + ", " + _state + ", " + player.Value.X + ", " + player.Value.Y + "\n\r";
				else
					output += player.Key + ", " + _clients[player.Key].State + ", " + player.Value.X + ", " + player.Value.Y + "\n\r";
			}
			StreamWriter sw = new StreamWriter("LocalState-" + _pId + "-" + _roundId);
			sw.Write(output);
			sw.Close();
			return output;
		}
#endregion
	}
}
