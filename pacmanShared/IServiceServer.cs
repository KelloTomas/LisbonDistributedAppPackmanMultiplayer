
namespace Shared
{
	public interface IServiceServer : ICommands
	{
		void RegisterPlayer(string pId, string clientURL);
		void SetMove(string pId, int roundId, Direction direction);
	}
}