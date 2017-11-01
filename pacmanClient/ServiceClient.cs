using Shared;
using System;
using System.Collections.Generic;

namespace pacmanClient
{
	public class ServiceClientWithState : MarshalByRefObject, IServiceClientWithState
	{
		//string _serverPId;
		Form1 _form;

        public ServiceClientWithState()
        {
            State = State.Playing;
        }

		public State State { get; set; }

		public ServiceClientWithState(string pID, Form1 form)
		{
			_form = form;
		}

		public void GameStarted(string serverPId, List<Client> clients, Game game)
		{
			_form.GameStarted(serverPId, clients, game);
		}

		public void MessageReceive(string pId, string msg)
		{
			_form.MessageReceive(pId, msg);
		}

		public void UpdateGame(Game game)
		{
			_form.UpdateGame(game);
		}

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

		public void CrashWithMonster()
		{
			_form.CrashWithMonster();
		}

		#endregion
	}
}
 