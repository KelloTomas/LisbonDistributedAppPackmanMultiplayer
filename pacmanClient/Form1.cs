using Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
		private Direction _direction;
		private List<PictureBox> players = new List<PictureBox>();
		private List<PictureBox> monsters = new List<PictureBox>();
		private int _roundId = 0;
		private Game _game;
		private ServiceClient serviceClient;
		private TcpChannel channel;
		private System.Timers.Timer _timer;
		private static IServiceServer server;
		private string _pId;
		private Dictionary<string, IServiceClient> _clients = new Dictionary<string, IServiceClient>();
		#endregion

		#region constructor...
		public Form1(string[] args) {
			InitializeComponent();
            label2.Visible = false;
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
			serviceClient = new ServiceClient(_pId, this);
			RemotingServices.Marshal(serviceClient, Shared.Shared.ParseUrl(URLparts.Link, myURL));

			/* get service */
			server = (IServiceServer)Activator.GetObject(
				typeof(IServiceServer),
				serverURL);
			if (!server.RegisterPlayer(_pId, myURL))
			{
				//ToDo cant connect, server full
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
			/*
			try
			{
			*/
			server.SetMove(_pId, _roundId++, _direction);
			/*
			}
			catch (Exception)
			{

			}
			*/
        }

        private void TbMsg_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
				// foreach ToDo
				foreach (IServiceClient client in _clients.Values)
				{
					client.MessageReceive(_pId, tbMsg.Text);
				}
                tbChat.Text += "\r\n" + tbMsg.Text;
				tbMsg.Clear();
				tbMsg.Enabled = false;
				this.Focus();
            }
        }
#endregion

		public void GameStarted(string serverPId, List<Client> clients, Game game)
		{
			_game = game;
			_timer.Start();
			foreach (Client client in clients)
			{
				if (client.PId == _pId)
					continue;
				_clients.Add(client.PId, (IServiceClient)Activator.GetObject(
				typeof(IServiceClient),
				client.URL));
			}
		}

		private void RemoveCharacterFromForm(PictureBox picture)
		{
			picture.Dispose();
		}

		private PictureBox DrawNewCharacterToGame(Control.ControlCollection Controls, Bitmap picture)
		{
			PictureBox ghost = new System.Windows.Forms.PictureBox();

			((ISupportInitialize)(ghost)).BeginInit();
			ghost.BackColor = Color.Transparent;
			ghost.Image = picture;
			//ghost.Location = new Point(180, 73);
			//ghost.Name = "redGhost";
			ghost.Size = new Size(30, 30);
			ghost.SizeMode = PictureBoxSizeMode.Zoom;
			//ghost.TabIndex = 1;
			//ghost.TabStop = false;
			//ghost.Tag = "ghost";
			Controls.Add(ghost);
			((ISupportInitialize)(ghost)).EndInit();
			return ghost;
		}

		public void UpdateGame(Game game)
		{
			int i;
			for (i = 0; i < game.Players.Count; i++)
			{
				if (i > players.Count)
					DrawNewCharacterToGame(Controls, Properties.Resources.red_guy);
				players.ElementAt(i).Left = game.Players.ElementAt(i).Value.X;
				players.ElementAt(i).Top = game.Players.ElementAt(i).Value.Y;
			}
			while(players.Count > i)
			{
				RemoveCharacterFromForm(players.ElementAt(i));
				players.RemoveAt(i);
			}
			i = 0;
			for (i = 0; i < game.Monsters.Count; i++)
			{
				if (i > monsters.Count)
					DrawNewCharacterToGame(Controls, Properties.Resources.Left);
				monsters.ElementAt(i).Left = game.Monsters.ElementAt(i).X;
				monsters.ElementAt(i).Top = game.Monsters.ElementAt(i).Y;
			}
			while (monsters.Count > i)
			{
				RemoveCharacterFromForm(monsters.ElementAt(i));
				monsters.RemoveAt(i);
			}

			if (_game.Coins.Count != game.Coins.Count)
			{
				//ToDo update coins
			}
		}

		public void MessageReceive(string pId, string msg)
		{
			BeginInvoke(new MethodInvoker(delegate
			{
				tbChat.Text += $"{pId}: {msg}\r\n";
			}));
		}
    }
}
