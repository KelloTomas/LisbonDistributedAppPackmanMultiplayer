using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Timers;

namespace Shared
{
	public interface IServiceClient : ICommands
	{
		//IServiceClient(string pID);
		void GameStarted(string serverPId, List<Client> clients, Game game);
		void MessageReceive(string pId, string msg);
		void UpdateGame(Game game);
		void CrashWithMonster();
		/*
		TcpChannel _channel;
		string _pId;
		string _serverPId;
		Timer _timer;
		List<Client> _clients;

		public ServiceClient(string pID)
		{
			_pId = pID;
		}

		public void GameStarted(string serverPId, List<Client> clients)
		{
			_clients = clients;
			_serverPId = serverPId;
		}

		public void UpdateGame(List<Player> players)
		{

		}

		public void MessageReceive(int id, string msg)
		{
			throw new NotImplementedException();
		}
		public void MessageSend(string msg)
		{
			throw new NotImplementedException();
		}

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
		*/
	}
}
 