using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Timers;

namespace pacmanClient
{
	public class ServiceClient : MarshalByRefObject, IServiceClient
	{
		string _serverPId;
		Form1 _form;

		public ServiceClient(string pID, Form1 form)
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

		public void GlobalStatus()
		{
			throw new NotImplementedException();
		}

		public void InjectDelay(string PID, int mSecDelay)
		{
			throw new NotImplementedException();
		}

		public void Freez()
		{
			throw new NotImplementedException();
		}

		public void UnFreez()
		{
			throw new NotImplementedException();
		}

		public string LocalState(int roundId)
		{
			throw new NotImplementedException();
		}
#endregion
	}
}
 