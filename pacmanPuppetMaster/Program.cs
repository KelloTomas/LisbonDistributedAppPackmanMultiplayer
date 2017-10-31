using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;

namespace PuppetMaster
{
	class Program
	{
		StreamReader reader = null;
		Timer timer;
		List<Process> process = new List<Process>();
        Dictionary<string, string> urls = new Dictionary<string, string>();
        Dictionary<string, ICommands> activators = new Dictionary<string, ICommands>();
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
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, true);
            timer = new Timer
			{
				AutoReset = false
			};
			timer.Elapsed += Timer_Elapsed;
			if (args.Count() >= 1)
			{
				Console.WriteLine("Reading file: " + args[0]);
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
            ICommands program;
            string processPId;
            if (string.IsNullOrWhiteSpace(command))
				return false;
			Console.WriteLine("Starting: " + command);
			string[] parts = command.Split(' ');
			switch (parts[0])
			{
				case "q":
					foreach (Process proc in process)
					{
						// window can be already closed
						try
						{
							proc.CloseMainWindow();
							proc.Close();
						}
						catch (InvalidOperationException)
						{
						}
					}
					Environment.Exit(1);
					return false;
				case "StartServer":
					serverPId = parts[1];
					serverURL = parts[3];
					string mSec = parts[4];
					string numOfPlayers = parts[5];
					Console.WriteLine(ExeFileNameServer + " " + serverPId + " " + serverURL + " " + mSec + " " + numOfPlayers);
                    process.Add(Process.Start(Path.Combine("..\\..\\..\\pacmanServer\\bin\\Debug", ExeFileNameServer),
																	serverPId + " " + serverURL + " " + mSec + " " + numOfPlayers));
                    urls.Add(serverPId, serverURL);
                    program = (ICommands)Activator.GetObject(typeof(ICommands), serverURL);
                    if (program == null)
                    {
                        Console.WriteLine("Could not locate process");
                    }
                    else
                    {
                        activators.Add(serverPId, program);
                    }
                    break;
				case "StartClient":
					string clientPId = parts[1];
					string clientURL = parts[3];
					string clientMSec = parts[4];
					string filename = parts.Count() == 6 ? null : parts[6];
					Console.WriteLine(ExeFileNameClient + " " + clientPId + " " + clientURL + " " + serverPId + " " + serverURL + " " + clientMSec + " " + filename);
                    process.Add(Process.Start(Path.Combine("..\\..\\..\\pacmanClient\\bin\\Debug", ExeFileNameClient),
																	clientPId + " " + clientURL + " " + serverPId + " " + serverURL + " " + clientMSec + " " + filename));
                    urls.Add(clientPId, clientURL);
                    program = (ICommands)Activator.GetObject(typeof(ICommands), clientURL);
                    if (program == null)
                    {
                        Console.WriteLine("Could not locate process");
                    }
                    else
                    {
                        activators.Add(clientPId, program);
                    }
                    break;
				case "GlobalStatus":
                    foreach(KeyValuePair<string, ICommands> pair in activators)
                    {
                        Console.WriteLine(pair.Key);
                        pair.Value.GlobalStatus();   
                    }
                    break;
				case "Crash":
                    processPId = parts[1];
                    if(activators.TryGetValue(processPId, out program))
                    {
                        program.Crash();
                    }
                    break;
				case "Freeze":
					break;
				case "Unfreeze":
					break;
				case "InjectDelay":
                    string srcPID = parts[1];
                    string dstPID = parts[2];
                    if(activators.TryGetValue(srcPID, out program))
                        program.InjectDelay(dstPID, 3000);
					break;
				case "LocalState":
					break;
                case "Wait":
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
				default:
					Console.WriteLine("Unknow command " + command);
                    break;
			}
			return true;
		}
	}
}
