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
		int moreWait = 0;

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
			if (args.Count() == 2)
			{
				reader = new StreamReader(args[1]);
			}
			while(true)
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
			string[] parts = command.Split(' ');
			switch (parts[0])
			{
				case "wait_t":
					if(timer.Enabled)
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
					System.Diagnostics.Process.Start(Path.Combine("C:\\Users\\kellotom\\source\\repos\\packmanMultiplayer\\pacmanServer\\pacmanServer\\bin\\Debug", "pacmanServer.exe"),
																	command.Replace("wait_t ", ""));
					break;
				case "StartClient":
					System.Diagnostics.Process.Start(Path.Combine("C:\\Users\\kellotom\\source\\repos\\packmanMultiplayer\\pacmanServer\\pacmanServer\\bin\\Debug", "pacmanServer.exe"),
																	command.Replace("wait_t ", ""));
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
