using System.Collections.Generic;

namespace Shared
{
	public interface IServiceClient : ICommands
	{
		void GameStarted(string serverPId, List<Client> clients, Game game);
		void MessageReceive(string pId, string msg);
		void GameUpdate(Game game);
		void GameEnded(bool win);
	}
}
 