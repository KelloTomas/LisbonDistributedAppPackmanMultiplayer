using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Timers;
using System.Diagnostics;
using Shared;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;

namespace PuppetMaster
{
	class Program
	{
		StreamReader reader = null;
		System.Timers.Timer timer;
		Dictionary<string, string> urls = new Dictionary<string, string>();
		Dictionary<string, ICommands> activators = new Dictionary<string, ICommands>();
		Dictionary<string, IServicePCS> PCSDic = new Dictionary<string, IServicePCS>();
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
			ChannelServices.RegisterChannel(channel, false);
			timer = new System.Timers.Timer
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
			while (ParseCommand(reader.ReadLine())) ;
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
					foreach (IServicePCS PCS in PCSDic.Values)
					{
						try
						{
							PCS.QuitAllPrograms();
						}
						catch
						{
						}
					}
					Environment.Exit(1);
					return false;
				case "StartServer":
					serverPId = parts[1];
					string PCSURL = parts[2];
					serverURL = parts[3];
					string mSec = parts[4];
					string numOfPlayers = parts[5];
					if(!PCSDic.ContainsKey(PCSURL))
					{
						Console.WriteLine("Connecting to new PCS");
						PCSDic[PCSURL] = (IServicePCS)Activator.GetObject(typeof(IServicePCS), PCSURL);
						Thread.Sleep(2000);
					}
					PCSDic[PCSURL].StartServer(serverPId + " " + serverURL + " " + mSec + " " + numOfPlayers);

					program = (ICommands)Activator.GetObject(typeof(ICommands), serverURL);
					urls.Add(serverPId, serverURL);
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
					string PCSURL2 = parts[2];
					string clientURL = parts[3];
					string clientMSec = parts[4];
					string filename = parts.Count() == 6 ? null : parts[6];
					if (!PCSDic.ContainsKey(PCSURL2))
					{
						Console.WriteLine("Connecting to new PCS");
						PCSDic[PCSURL2] = (IServicePCS)Activator.GetObject(typeof(IServicePCS), PCSURL2);
						Thread.Sleep(2000);
					}
					PCSDic[PCSURL2].StartClient(clientPId + " " + clientURL + " " + serverPId + " " + serverURL + " " + clientMSec + " " + filename);

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
					foreach (KeyValuePair<string, ICommands> pair in activators)
					{
						Console.WriteLine(pair.Key);
						pair.Value.GlobalStatus();
					}
					break;
				case "Crash":
					processPId = parts[1];
					if (activators.TryGetValue(processPId, out program))
					{
						program.Crash();
					}
					break;
				case "Freeze":
					processPId = parts[1];
					if (activators.TryGetValue(processPId, out program))
						program.Freez();
					break;
				case "Unfreeze":
					processPId = parts[1];
					if (activators.TryGetValue(processPId, out program))
						program.UnFreez();
					break;
				case "InjectDelay":
					string srcPID = parts[1];
					string dstPID = parts[2];
					if (activators.TryGetValue(srcPID, out program))
						program.InjectDelay(dstPID, 3000);
					break;
				case "LocalState":
					processPId = parts[1];
					int round;
					int.TryParse(parts[2], out round);
					if (activators.TryGetValue(processPId, out program))
						Console.WriteLine(program.LocalState(round));
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
