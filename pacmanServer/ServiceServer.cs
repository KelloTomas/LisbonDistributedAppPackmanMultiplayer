using CommonTypes;
using Shared;
using System;

namespace pacmanServer
{
	public class ServiceServer : MarshalByRefObject, IServiceServer
	{
		#region private fields...
		private Program _program;
		private Delays _delays;
		#endregion

		#region constructor...
		internal ServiceServer(Program program, Delays delays)
		{
			_program = program;
			_delays = delays;
		}
		#endregion

		#region IServiceServer
		public void RegisterPlayer(string pId, string clientURL)
		{
			_delays.IsFrozen();
			_program.RegisterPlayer(pId, clientURL);
		}

		public void SetMove(string pId, int roundId, Direction direction)
		{
			_delays.IsFrozen();
			_program.SetMove(pId, roundId, direction);
		}
		#endregion

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