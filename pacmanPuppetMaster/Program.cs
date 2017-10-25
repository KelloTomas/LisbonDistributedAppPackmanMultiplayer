using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PuppetMaster
{
	class Program
	{
		StreamReader reader = null;
		Timer timer;
		List<System.Diagnostics.Process> process = new List<System.Diagnostics.Process>();
		const string ExeFileNameServer = "pacmanServer.exe";
		const string ExeFileNameClient = "pacmanClient.exe";
		int moreWait = 0;
		string serverURL = "";
		private string serverPId;

		static void Main(string[] args)
		{
			new Program().Init(args);
		}

		void Init(string[] args)
		{
			timer = new Timer
			{
				AutoReset = false
			};
			timer.Elapsed += Timer_Elapsed;
			if (args.Count() >= 1)
			{
				Console.WriteLine($"Reading file: {args[0]}");
				reader = new StreamReader(args[0]);
				ReadInstFromFile();
			}
			else
			{
				Console.WriteLine("No filename in arguments");
			}
			while (true)
			{
				ParseCommand(Console.ReadLine());
			}
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (moreWait > 0)
			{
				timer.Interval = moreWait;
				moreWait = 0;
				timer.Start();
				return;
			}
			ReadInstFromFile();
		}

		private void ReadInstFromFile()
		{
			while (ParseCommand(reader.ReadLine()));
		}
		bool ParseCommand(string command)
		{
			if (string.IsNullOrWhiteSpace(command))
				return false;
			Console.WriteLine($"Starting: {command}");
			string[] parts = command.Split(' ');
			switch (parts[0])
			{
				case "q":
					foreach (System.Diagnostics.Process proc in process)
					{
						// window can be already closed
						try
						{
						proc.CloseMainWindow();
						proc.Close();
						}
						catch (System.InvalidOperationException)
						{

						}
					}
					Environment.Exit(1);
					return false;
				case "wait_t":
					if (timer.Enabled)
					{
						moreWait += int.Parse(parts[1]);
					}
					else
					{
						timer.Interval = int.Parse(parts[1]);
						timer.Start();
					}
					return false;
				case "StartServer":
					serverPId = parts[1];
					serverURL = parts[3];
					string mSec = parts[4];
					string numOfPlayers = parts[5];
					Console.WriteLine($"{ExeFileNameServer} {serverPId} {serverURL} {mSec} {numOfPlayers}");
					process.Add(System.Diagnostics.Process.Start(Path.Combine("C:\\Users\\kellotom\\source\\repos\\packmanMultiplayer\\pacmanServer\\bin\\Debug", ExeFileNameServer),
																	$"{serverPId} {serverURL} {mSec} {numOfPlayers}"));
					break;
				case "StartClient":
					string clientPId = parts[1];
					string clientURL = parts[3];
					string clientMSec = parts[4];
					string filename = parts.Count() == 6 ? null : parts[6];
					Console.WriteLine($"{ExeFileNameClient} {clientPId} {clientURL} {serverPId} {serverURL} {clientMSec} {filename}");
					process.Add(System.Diagnostics.Process.Start(Path.Combine("C:\\Users\\kellotom\\source\\repos\\packmanMultiplayer\\pacmanClient\\bin\\Debug", ExeFileNameClient),
																	$"{clientPId} {clientURL} {serverPId} {serverURL} {clientMSec} {filename}"));
					break;
				case "GlobalStatus":
					break;
				case "Crash":
					break;
				case "Freeze":
					break;
				case "Unfreeze":
					break;
				case "InjectDelay":
					break;
				case "LocalState":
					break;
				case "Wait":
					break;
				default:
					Console.WriteLine($"Unknow command {command}");
					return false;
			}
			return true;
		}
	}
}
