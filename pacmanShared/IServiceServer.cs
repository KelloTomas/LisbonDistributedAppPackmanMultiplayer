using System;

namespace Shared
{
	public interface IServiceServer : ICommands  {

		bool RegisterPlayer(string pId, string clientURL);

		void SetMove(string pId, int roundId, Direction direction);

	}
}