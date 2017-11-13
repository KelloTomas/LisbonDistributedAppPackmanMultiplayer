using System.Collections.Generic;

namespace Shared
{
	public interface IServiceClient : ICommands
	{
		void GameStarted(string serverPId, int clientId, List<Client> clients, Game game);
		void MessageReceive(int[] vectorClock, int pId, string msg);
		void GameUpdate(Game game);
		void GameEnded(bool win);
	}
}
