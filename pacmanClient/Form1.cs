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
		private Frozens _frozens = new Frozens();
		private StreamReader reader = null;
		private string _serverPId;
		private Tuple<int, Direction> commandFromFile = new Tuple<int, Direction>(-1, Direction.NO);
		private Direction _direction = Direction.NO;
		private List<PictureBox> players = new List<PictureBox>();
		private List<PictureBox> monsters = new List<PictureBox>();
		private List<PictureBox> coins = new List<PictureBox>();
		private MessageQueue _messageQueue;

		private int _roundId = -1;
		private Game _game = null;
		private ServiceClientWithState serviceClient;
		private TcpChannel channel;
		private System.Timers.Timer _timer;
		private static IServiceServer server;
		private string _pId;


		private Dictionary<string, IServiceClientWithState> _clients = new Dictionary<string, IServiceClientWithState>();
		private int _score = 0;
		#endregion

		#region constructor...
		public Form1(string[] args)
		{
			InitializeComponent();
			label2.Visible = false;
			_pId = args[0];
			Text = _pId;
			string myURL = args[1];
			_serverPId = args[2];
			string serverURL = args[3];
			int mSec = int.Parse(args[4]);
			if (args.Count() == 6)
			{
				reader = new StreamReader(args[5]);
				ReadNextInstruction(reader);
			}
			_timer = new System.Timers.Timer() { Interval = mSec, AutoReset = true, Enabled = false };
			_timer.Elapsed += Timer_Tick;
			string tmp = Shared.Shared.ParseUrl(URLparts.Port, myURL);

			/* set channel */
			channel = new TcpChannel(int.Parse(tmp));
			ChannelServices.RegisterChannel(channel, false);

			/*set service */
			serviceClient = new ServiceClientWithState(this, _frozens);
			RemotingServices.Marshal(serviceClient, Shared.Shared.ParseUrl(URLparts.Link, myURL));

			/* get service */
			server = (IServiceServer)Activator.GetObject(
				typeof(IServiceServer),
				serverURL);
			try
			{
				_delays.SendWithDelay(_serverPId, (Action<string, string>) server.RegisterPlayer, new object[] { _pId, myURL });
			}
			catch
			{
				Console.WriteLine("Cant connect to server");
			}
		}

		private void ReadNextInstruction(StreamReader reader)
		{
			string[] line = reader.ReadLine().Split(',');
			if (line.Count() != 2)
			{
				commandFromFile = new Tuple<int, Direction>(-1, Direction.NO);
				return;
			}
			else
			{
				commandFromFile = new Tuple<int, Direction>(int.Parse(line[0]), (Direction)Enum.Parse(typeof(Direction), line[1]));
			}
		}
		#endregion

		#region controller handler
		private void KeyIsDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Left)
			{
				_direction = Direction.LEFT;
			}
			if (e.KeyCode == Keys.Right)
			{
				_direction = Direction.RIGHT;
			}
			if (e.KeyCode == Keys.Up)
			{
				_direction = Direction.UP;
			}
			if (e.KeyCode == Keys.Down)
			{
				_direction = Direction.DOWN;
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
				_direction = Direction.NO;
			}
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			lock (this)
			{
				_roundId++;
				Direction d;
				if (_roundId == commandFromFile.Item1)
				{
					d = commandFromFile.Item2;
					ReadNextInstruction(reader);
					Console.WriteLine("Round: " + _roundId + " Sending direction from file: " + d);
				}
				else
				{
					d = _direction;
					Console.WriteLine("Round: " + _roundId + " Sending direction: " + d);
				}
				_delays.SendWithDelay(_serverPId, (Action<string, int, Direction>)server.SetMove, new object[] { _pId, _roundId, d });
			}
		}

		private void MessageSend(object sender, KeyEventArgs e)
		{
			lock (this)
			{
				if (e.KeyCode == Keys.Enter)
				{
					int[] vectorClock = _messageQueue.GetVectorClock();
					_messageQueue.NewMessage(vectorClock, _messageQueue.GetMyId(), tbMsg.Text);
					foreach (KeyValuePair<string, IServiceClientWithState> client in _clients)
					{
						_delays.SendWithDelay(client.Key, (Action<int[], int, string>)client.Value.MessageReceive, new object[] { vectorClock , _messageQueue.GetMyId(), _pId + ": " + tbMsg.Text });
					}
					tbChat.Text = _messageQueue.GetAllMessages();
					tbMsg.Clear();
					tbMsg.Enabled = false;
					Focus();
				}
			}
		}
		#endregion

		#region Client service...
		public void GameStarted(string serverPId, int myId, List<Client> clients, Game game)
		{
			Console.WriteLine("game started");
			_messageQueue = new MessageQueue(clients.Count, myId);
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
						Console.WriteLine("Player "+ player.Key);
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
			Console.WriteLine("Updating game, round: " + game.RoundId);
			lock (this)
			{
				_game = game;
				BeginInvoke(new MethodInvoker(delegate
				{
					UpdatePlayersPosition(game);
					UpdateMonsterPosition(game);
					UpdateCoinPosition(game);
				}));
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
			{
				if (_timer.Enabled)
				{
					if (game.Players.ElementAt(i).Value.state == State.Dead)
						GameEnded(false);
				}
				if (game.Players.ElementAt(i).Value.Score != _score)
				{
					_score = game.Players.ElementAt(i).Value.Score;
					BeginInvoke(new MethodInvoker(delegate
					{
						label1.Text = "Score: " + _score;
					}));
				}
			}
			players.ElementAt(i).Left = game.Players.ElementAt(i).Value.X;
			players.ElementAt(i).Top = game.Players.ElementAt(i).Value.Y;
			switch (game.Players.ElementAt(i).Value.Direction)
			{
				case Direction.UP:
					players.ElementAt(i).Image = Properties.Resources.Up;
					break;
				case Direction.DOWN:
					players.ElementAt(i).Image = Properties.Resources.Down;
					break;
				case Direction.LEFT:
					players.ElementAt(i).Image = Properties.Resources.Left;
					break;
				case Direction.RIGHT:
					players.ElementAt(i).Image = Properties.Resources.Right;
					break;
			}
		}

		internal void MessageReceive(int[] vectorClock, int pId, string msg)
		{
			_messageQueue.NewMessage(vectorClock, pId, msg);
			BeginInvoke(new MethodInvoker(delegate
			{
				tbChat.Text = _messageQueue.GetAllMessages();
			}));
		}
		internal void ClientDisconnect(string pid)
		{
			lock (_clients)
			{
				_clients.Remove(pid);
			}
		}
		internal void ClientConnect(string pid, string URL)
		{
			lock (_clients)
			{
				_clients.Add(pid, (IServiceClientWithState)Activator.GetObject(
				typeof(IServiceClient),
				URL));
			}
		}
		internal void GameEnded(bool win)
		{
			_timer.Stop();
			_timer.Dispose();
			BeginInvoke(new MethodInvoker(delegate
			{
				label2.Visible = true;
				label2.Text = win ? "You win" : "Game Over";
			}));
		}
		#endregion

		#region Control service...
		internal void Crash()
		{
			Close();
		}

		internal void InjectDelay(string pId, int mSecDelay)
		{
			_delays.AddDelay(pId, mSecDelay);
		}

		internal void Freez()
		{
			_frozens.Freez();
		}

		internal void UnFreez()
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
	}
}
