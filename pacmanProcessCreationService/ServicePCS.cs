using Shared;
using System;

namespace ProcessCreationService
{
	public class ServicePCS : MarshalByRefObject, IServicePCS
	{
		#region private fields...
		public Program _program;
		#endregion

		#region constructor...
		public ServicePCS(Program program)
		{
			_program = program;
		}
		#endregion

		#region IServicePCS
		public void StartServer(string programArguments)
		{
			_program.StartServer(programArguments);
		}
		public void StartSecondaryServer(string programArguments)
		{
			_program.StartSecondaryServer(programArguments);
		}
		public void StartClient(string programArguments)
		{
			_program.StartClient(programArguments);
		}

		public void QuitAllPrograms()
		{
			Console.WriteLine("Closing all programs");
			_program.QuitAllPrograms();
			Environment.Exit(1);
		}
		#endregion
	}
}