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
		public State State { get; set; }
		private Delays _delays;
		#endregion

		#region Constructor...
		internal ServiceClientWithState(Form1 form, Delays delays)
		{
			_form = form;
			_delays = delays;
			State = State.Playing;
		}
		#endregion

		#region IServiceClient
		public void MessageReceive(string pId, string msg)
		{
			_delays.IsFrozen();
			_form.MessageReceive(pId, msg);
		}

		public void GameStarted(string serverPId, List<Client> clients, Game game)
		{
			_delays.IsFrozen();
			_form.GameStarted(serverPId, clients, game);
		}

		public void GameUpdate(Game game)
		{
			_delays.IsFrozen();
			_form.GameUpdate(game);
		}

		public void GameEnded(bool win)
		{
			_delays.IsFrozen();
			_form.GameEnded(win);
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
			return _form.LocalState();
		}
		#endregion
	}
}
