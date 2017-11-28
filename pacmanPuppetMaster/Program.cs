using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Timers;
using Shared;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Threading.Tasks;

namespace PuppetMaster
{
	class Program
	{
		#region private fields...
		private StreamReader reader = null;
		private System.Timers.Timer timer;
		private Dictionary<string, string> urls = new Dictionary<string, string>();
		private Dictionary<string, ICommands> activators = new Dictionary<string, ICommands>();
		private Dictionary<string, IServicePCS> PCSDic = new Dictionary<string, IServicePCS>();
		private int moreWait = 0;
		private string serverURL = "";
		private string serverPId;
		#endregion

		#region start program...
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
		#endregion

		#region private methods...
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

		private bool ParseCommand(string command)
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

					CheckPCSConnection(PCSDic, PCSURL);
					PCSDic[PCSURL].StartServer(serverPId + " " + serverURL + " " + mSec + " " + numOfPlayers);

					program = (ICommands)Activator.GetObject(typeof(ICommands), serverURL);
					CheckProgram(program, serverPId, serverURL);
					break;
				case "StartClient":
					string clientPId = parts[1];
					string PCSURL2 = parts[2];
					string clientURL = parts[3];
					string clientMSec = parts[4];
					string filename = parts.Count() == 6 ? null : parts[6];

					CheckPCSConnection(PCSDic, PCSURL2);
					PCSDic[PCSURL2].StartClient(clientPId + " " + clientURL + " " + serverPId + " " + serverURL + " " + clientMSec + " " + filename);

					program = (ICommands)Activator.GetObject(typeof(ICommands), clientURL);
					CheckProgram(program, clientPId, clientURL);
					break;
				case "GlobalStatus":
					Console.WriteLine("Getting global status");
					Parallel.ForEach(activators.Values, connection =>
					{
						connection.GlobalStatus();
					});
					break;
				case "Crash":
					processPId = parts[1];
					if (activators.TryGetValue(processPId, out program))
					{
						//program.Crash();
						new Task(program.Crash).Start();
						activators.Remove(processPId);
						urls.Remove(processPId);
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
						program.InjectDelay(dstPID, 1000);
					break;
				case "LocalState":
					processPId = parts[1];
					int round;
					int.TryParse(parts[2], out round);
					if (activators.TryGetValue(processPId, out program))
						Console.WriteLine(program.LocalState(round));
					break;
				case "Wait":
					timer.Stop();
					Thread.Sleep(int.Parse(parts[1]));
					timer.Start();
					/*
					if (timer.Enabled)
					{
						moreWait += int.Parse(parts[1]);
					}
					else
					{
						timer.Interval = int.Parse(parts[1]);
						timer.Start();
					}*/
					return false;
				default:
					Console.WriteLine("Unknow command " + command);
					break;
			}
			return true;
		}

		private void CheckProgram(ICommands program, string clientPId, string clientURL)
		{
			urls.Add(clientPId, clientURL);
			if (program == null)
			{
				Console.WriteLine("Could not locate process");
			}
			else
			{
				activators.Add(clientPId, program);
			}
		}

		private void CheckPCSConnection(Dictionary<string, IServicePCS> dic, string PCSURL2)
		{
			if (!dic.ContainsKey(PCSURL2))
			{
				Console.WriteLine("Connecting to new PCS");
				dic[PCSURL2] = (IServicePCS)Activator.GetObject(typeof(IServicePCS), PCSURL2);
				Thread.Sleep(2000);
			}
		}
		#endregion
	}
}
