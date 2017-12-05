using CommonTypes;
using Shared;
using System;

namespace pacmanServer
{
	public class ServiceServer : MarshalByRefObject, IServiceServer
	{
		#region private fields...
		private Program _program;
		private Delays _frozens;
		#endregion

		#region constructor...
		internal ServiceServer(Program program, Delays frozens)
		{
			_program = program;
			_frozens = frozens;
		}
		#endregion

		#region IServiceServer
		public void RegisterPlayer(string pId, string clientURL)
		{
			_frozens.Freeze((Action<string, string>)_program.RegisterPlayer, pId, clientURL);
		}

		public void SetMove(string pId, int roundId, Direction direction)
		{
			_frozens.Freeze((Action<string, int, Direction>)_program.SetMove, pId, roundId, direction);
		}
		public object[] ImAlive()
		{
			return (object[]) _frozens.FreezeWithReturn((Func<object[]>)_program.ImAlive, new object[] { });
			//return _program.ImAlive();
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
            return _program.LocalState(roundId);
		}

		public void Freez()
		{
			_program.Freez();
		}

		public void UnFreez()
		{
			_program.UnFreez();
		}

		public void InjectDelay(string PID, int mSecDelay)
		{
			_program.InjectDelay(PID, mSecDelay);
		}
		#endregion
	}
}