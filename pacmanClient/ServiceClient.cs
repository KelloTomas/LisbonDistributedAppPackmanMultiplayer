using CommonTypes;
using Shared;
using System;
using System.Collections.Generic;

namespace pacmanClient
{
	public class ServiceClientWithState : MarshalByRefObject, IServiceClientWithState
	{
		#region private fields...
		private Form1 _form;
		private Frozens _frozens;
		public State State { get; set; }

		#endregion

		#region Constructor...
		internal ServiceClientWithState(Form1 form, Frozens frozens)
		{
			_form = form;
			_frozens = frozens;
			State = State.Playing;
		}
		#endregion

		#region IServiceClient
		public void ClientDisconnect(string pid)
		{
			_frozens.Freeze((Action<string>)_form.ClientDisconnect, new object[] { pid });
		}
		public void ClientConnect(string pid, string URL)
		{
			_frozens.Freeze((Action<string, string>)_form.ClientConnect, new object[] { pid , URL});
		}
		// init funkcion. Called ones after start, so program has no delays.
		public int GetChatId()
		{
			return _form.GetChatId();
		}
		// init funkcion. Called ones after start, so program has no delays.
		public int[] GetVectorClocks()
		{
			return _form.GetVectorClocks();
		}
		public void MessageReceive(int[] vectorClock, int pId, string msg)
		{
			_frozens.Freeze((Action< int[] , int , string >)_form.MessageReceive, new object[] { vectorClock, pId, msg });
		}

		public void GameStarted(string serverPId, int myId, List<Client> clients, Game game)
		{
			_frozens.Freeze((Action<string, int, List<Client>, Game>)_form.GameStarted, new object[] { serverPId, myId, clients, game });
		}

		public void GameUpdate(Game game)
		{
			_frozens.Freeze((Action<Game>)_form.GameUpdate, new object[] { game });
		}

		public void GameEnded(bool win)
		{
			_form.GameEnded(win);
			_frozens.Freeze((Action<bool>)_form.GameEnded, new object[] { win });
		}
		public object[] ImAlive() {
			return null;
		}
		#endregion

		#region IController

		public void Crash()
		{
			_form.Crash();
		}

		public void GlobalStatus()
		{
			_form.GlobalStatus();
		}

		public void InjectDelay(string PID, int mSecDelay)
		{
			_form.InjectDelay(PID, mSecDelay);
		}

		public void Freez()
		{
			_form.Freez();
		}

		public void UnFreez()
		{
			_form.UnFreez();
		}

		public string LocalState(int roundId)
		{
			return _form.LocalState(roundId);
		}
		#endregion
	}
}
