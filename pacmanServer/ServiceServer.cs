using pacmanServer;
using Shared;
using System;

namespace CommonTypes
{
	public class ServiceServer : MarshalByRefObject, IServiceServer
	{
		private Program _program;
		public ServiceServer(Program program)
		{
			_program = program;
		}
		public bool RegisterPlayer(string pId, string clientURL)
		{
			return _program.RegisterPlayer(pId, clientURL);
		}

		public void SetMove(string pId, int roundId, Direction direction)
		{
			_program.SetMove(pId, roundId, direction);
		}

        #region IController
        public void Crash()
        {
            _program.Crash();
        }
        public void GlobalStatus()
		{
            _program.GlobalStatus();
		}

		public string LocalState(int roundId)
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

		public void InjectDelay(string PID, int mSecDelay)
		{
            _program.InjectDelay(PID, mSecDelay);
		}
#endregion
	}
}