using CommonTypes;
using Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Windows.Forms;



namespace pacmanClient
{
	internal partial class Form1 : Form
	{
		#region private...
		private Delays _delays = new Delays();
		private string filename = null;
		private string _serverPId;
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

		internal void GameEnded(bool win)
		{
			BeginInvoke(new MethodInvoker(delegate
			{
				label2.Visible = true;
				label2.Text = win ? "You win" : "Game Over";
				_state = State.Dead;
			}));
		}

		private Dictionary<string, IServiceClientWithState> _clients = new Dictionary<string, IServiceClientWithState>();
		private int _score = 0;
		#endregion

		#region constructor...
		public Form1(string[] args)
		{
			InitializeComponent();
			label2.Visible = false;
			_pId = args[0];
			string myURL = args[1];
			_serverPId = args[2];
			string serverURL = args[3];
			int mSec = int.Parse(args[4]);
			if (args.Count() == 6)
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
			serviceClient = new ServiceClientWithState(this, _delays);
			RemotingServices.Marshal(serviceClient, Shared.Shared.ParseUrl(URLparts.Link, myURL));

			/* get service */
			server = (IServiceServer)Activator.GetObject(
				typeof(IServiceServer),
				serverURL);
			try
			{
				_delays.SendWithDelay(_serverPId, (Action<string, string>)server.RegisterPlayer, new object[] { _pId, myURL });
			}
			catch
			{
				Console.WriteLine("Cant connect to server");
			}
		}
		#endregion

		#region controller handler
		private void KeyIsDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Left)
			{
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
			if (e.KeyCode == Keys.Enter)
			{
				tbMsg.Enabled = true; tbMsg.Focus();
			}
		}

		private void KeyIsUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Left
			|| e.KeyCode == Keys.Right
			|| e.KeyCode == Keys.Up
			|| e.KeyCode == Keys.Down)
			{
				_direction = Direction.No;
			}
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			if (_state == State.Dead)
			{
				_timer.Stop();
				_timer.Dispose();
			}
			else
			{
				Console.WriteLine("Round: " + _roundId + " Sending direction: ", _direction.ToString());
				_delays.SendWithDelay(_serverPId, (Action<string, int, Direction>)server.SetMove, new object[] { _pId, _roundId++, _direction });
			}
		}

		private void TbMsg_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				foreach (KeyValuePair<string, IServiceClientWithState> client in _clients)
				{
					//client.MessageReceive(_pId, tbMsg.Text);
                    _delays.SendWithDelay(client.Key, (Action<string, string>)client.Value.MessageReceive, new object[] { _pId, tbMsg.Text });

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
				lock (this)
				{
					foreach (var coin in game.Coins)
					{
						coins.Add(DrawNewCharacterToGame(Controls, Properties.Resources.coinPNG, CharactersSize.coin));
					}
					foreach (var player in game.Players)
					{
						players.Add(DrawNewCharacterToGame(Controls, Properties.Resources.Right, CharactersSize.Player));
					}
					foreach (var monster in game.Monsters)
					{
						monsters.Add(DrawNewCharacterToGame(Controls, Properties.Resources.red_guy, CharactersSize.Monster));
					}
					foreach (var obsticle in game.Obsticles)
					{
						DrawObsticle(Controls, obsticle);
					}
					UpdateCoinPosition(game, true);
				}
			}));
			lock (this)
			{
				foreach (Client client in clients)
				{
					if (client.PId == _pId)
						continue;
					_clients.Add(client.PId, (IServiceClientWithState)Activator.GetObject(
					typeof(IServiceClient),
					client.URL));
				}
				_timer.Start();
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

		private void DrawObsticle(Control.ControlCollection Controls, Obsticle o)
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

		public void GameUpdate(Game game)
		{
			_game = game;
			Console.WriteLine("Updating game, round: " + game.RoundId);
			lock (this)
			{
				UpdatePlayersPosition(game);
				UpdateMonsterPosition(game);
				UpdateCoinPosition(game);
			}
		}

		private void UpdatePlayersPosition(Game game)
		{
			for (int i = 0; i < game.Players.Count; i++)
			{
				UpdatePlayerPosition(game, i);
			}
		}

		private void UpdateMonsterPosition(Game game)
		{
			for (int i = 0; i < game.Monsters.Count; i++)
			{
				monsters.ElementAt(i).Left = game.Monsters.ElementAt(i).X;
				monsters.ElementAt(i).Top = game.Monsters.ElementAt(i).Y;
			}
		}

		private void UpdateCoinPosition(Game game, bool forceRedraw = false)
		{
			if (coins.Count != game.Coins.Count || forceRedraw)
			{
				int i;
				for (i = 0; i < game.Coins.Count; i++)
				{
					coins.ElementAt(i).Left = game.Coins.ElementAt(i).X;
					coins.ElementAt(i).Top = game.Coins.ElementAt(i).Y;
				}
				for (int x = i; x < coins.Count; x++)
				{
					RemoveCharacterFromForm(coins.ElementAt(x));
				}
				while (i < coins.Count)
					coins.RemoveAt(i);
			}
		}

		private void UpdatePlayerPosition(Game game, int i)
		{
			if (game.Players.ElementAt(i).Key == _pId)
				if (game.Players.ElementAt(i).Value.Score != _score)
				{
					_score = game.Players.ElementAt(i).Value.Score;
					BeginInvoke(new MethodInvoker(delegate
					{
						label1.Text = "Score: " + _score;
					}));
				}
			players.ElementAt(i).Left = game.Players.ElementAt(i).Value.X;
			players.ElementAt(i).Top = game.Players.ElementAt(i).Value.Y;
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
			Close();
		}
		#endregion

		#region Control service...
		internal void GlobalStatus()
		{
			Console.WriteLine("Global Status:");
			foreach (var client in _clients)
			{
				Console.WriteLine("client: " + client.Key + " is in state " + (client.Value == null ? "offline" : "online"));
			}
		}
		internal void InjectDelay(string pId, int mSecDelay)
		{
            _delays.AddDelay(pId, mSecDelay);
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
