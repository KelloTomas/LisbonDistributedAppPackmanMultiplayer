using CommonTypes;
using Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace ProcessCreationService
{
	public class Program
	{
		#region private fields...
		private TcpChannel channel;
		private ServicePCS servicePCS;
		List<Process> process = new List<Process>();
		const string ExeFileNameServer = "pacmanServer.exe";
		const string ExeFileNameClient = "pacmanClient.exe";
		static Program p;
#endregion

		public static void Main(string[] args)
		{
			p = new Program();
			p.Init(args);
			Console.ReadLine();
		}

		private void Init(string[] args)
		{
			if(args.Length == 0)
			{
				Console.WriteLine("Define PCS URL as argument");
				return;
			}
			string myURL = args[0];

			string link = Shared.Shared.ParseUrl(URLparts.Link, myURL);
			int port = int.Parse(Shared.Shared.ParseUrl(URLparts.Port, myURL));
			lock (this)
			{
				Console.WriteLine("Starting PCS on " + myURL + ", link: " + link + ", port: " + port);
				/* set channel */
				channel = new TcpChannel(port);
				ChannelServices.RegisterChannel(channel, true);

				/*set service */
				servicePCS = new ServicePCS(this);
				RemotingServices.Marshal(servicePCS, link);
			}
			while (Console.ReadLine() != "q") ;
		}

		public void QuitAllPrograms()
		{
			Console.WriteLine("Closing all windows");
			foreach (Process proc in process)
			{
				try
				{
					proc.CloseMainWindow();
					proc.Close();
				}
				catch (InvalidOperationException)
				{
				}
			}
		}

		public void StartServer(string programArguments)
		{
			Console.WriteLine(ExeFileNameServer + " " + programArguments);
			process.Add(Process.Start(Path.Combine("..\\..\\..\\pacmanServer\\bin\\Debug", ExeFileNameServer),
															programArguments));
		}

		public void StartClient(string programArguments)
		{
			Console.WriteLine(ExeFileNameServer + " " + programArguments);
			process.Add(Process.Start(Path.Combine("..\\..\\..\\pacmanClient\\bin\\Debug", ExeFileNameClient),
															programArguments));
		}
	}
}
