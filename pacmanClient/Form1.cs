using CommonTypes;
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



namespace pacman {
    public partial class Form1 : Form {
#region private...
		private Direction _direction;
		private List<PictureBox> players = new List<PictureBox>();
		private List<PictureBox> monsters = new List<PictureBox>();
		private int _roundId = 0;
		private Game _game;
		private ServiceClient serviceClient;
		private TcpChannel channel;
		private System.Timers.Timer timer;
		private IServiceServer server;
		private string _pId;
		private Dictionary<string, IServiceClient> _clients = new Dictionary<string, IServiceClient>();
		#endregion

		#region constructor...
		public Form1(string[] args) {
			_pId = args[1];
			string myURL = args[2];
			string serverPID = args[3];
			string serverURL = args[4];
			int mSec = int.Parse(args[5]);
			string filename = args[6];
			InitializeComponent();
            label2.Visible = false;

			timer = new System.Timers.Timer() { Interval = mSec, AutoReset = false, Enabled = false };
			timer.Elapsed += timer_Tick;
			channel = new TcpChannel(int.Parse(Shared.Shared.ParseUrl(URLparts.Port, myURL)));
			ChannelServices.RegisterChannel(channel, true);

			/*set service */
			serviceClient = new ServiceClient(_pId, this);
			RemotingServices.Marshal(serviceClient, "ServiceClient");

			/* get service */
			server = (IServiceServer)Activator.GetObject(
				typeof(IServiceServer),
				myURL);
			if (!server.RegisterPlayer(_pId, myURL))
			{
				//ToDo cant connect, server full
			}
		}
		#endregion

#region controller handler
		private void keyisdown(object sender, KeyEventArgs e) {
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

        private void keyisup(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Left
            || e.KeyCode == Keys.Right
            || e.KeyCode == Keys.Up
            || e.KeyCode == Keys.Down)
			{
				_direction = Direction.No;
			}
        }

        private void timer_Tick(object sender, EventArgs e) {
			server.SetMove(_pId, _roundId++, _direction);
        }

        private void tbMsg_KeyDown(object sender, KeyEventArgs e) {
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
			timer.Start();
			foreach (Client client in clients)
			{
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

			pacman.Image = Properties.Resources.Left;
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
