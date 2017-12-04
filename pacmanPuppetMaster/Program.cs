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
		private Dictionary<string, string> urls = new Dictionary<string, string>();
		private Dictionary<string, ICommands> activators = new Dictionary<string, ICommands>();
		private Dictionary<string, IServicePCS> PCSDic = new Dictionary<string, IServicePCS>();
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
		private void ReadInstFromFile()
		{
			while (ParseCommand(reader.ReadLine())) ;
		}

		private bool ParseCommand(string command)
		{
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
					Task.Run(() => StartServerParalel(PCSURL, mSec, numOfPlayers));
					break;
				case "StartClient":
					string clientPId = parts[1];
					string PCSURL2 = parts[2];
					string clientURL = parts[3];
					string clientMSec = parts[4];
					string filename = parts.Count() == 6 ? null : parts[6];
					Task.Run(() => StartClientParal(clientPId, PCSURL2, clientURL, clientMSec, filename));
					break;
				case "GlobalStatus":
					Task.Run(() => GlobalStatusParal());
					break;
				case "Crash":
					processPId = parts[1];
					Task.Run(() => CrashParal(processPId));
					break;
				case "Freeze":
					processPId = parts[1];
					Task.Run(() => FreezParal(processPId));
					break;
				case "Unfreeze":
					processPId = parts[1];
					Task.Run(() => UnfreezParal(processPId));
					break;
				case "InjectDelay":
					string srcPID = parts[1];
					string dstPID = parts[2];
					Task.Run(() => InjectDelayParal(srcPID, dstPID));
					break;
				case "LocalState":
					processPId = parts[1];
					Task.Run(() => LocalStateParal(processPId, parts));
					break;
				case "Wait":
					Thread.Sleep(int.Parse(parts[1]));
					break;
				default:
					Console.WriteLine("Unknow command " + command);
					break;
			}
			return true;
		}

		private void GlobalStatusParal()
		{
			Console.WriteLine("Getting global status");
			Parallel.ForEach(activators.Values, connection =>
			{
				connection.GlobalStatus();
			});
		}

		private void CrashParal(string processPId)
		{
			ICommands program;
			if (!TryGetProgram(processPId, out program))
			{
				Console.WriteLine("cant find client to get local state");
				return;
			}
			program.Crash();
			activators.Remove(processPId);
			urls.Remove(processPId);
		}

		private void LocalStateParal(string processPId, string[] parts)
		{
			ICommands program;
			int round = int.Parse(parts[2]);
			Console.WriteLine("Trying to get state");
			if (!TryGetProgram(processPId, out program))
			{
				Console.WriteLine("cant find client to get local state");
				return;
			}
			string output;
			try
			{
				output = program.LocalState(round);
			}
			catch (Exception)
			{
				// for example if client crash during response
				return;
			}
			Console.WriteLine("State");
			Console.WriteLine(output);
			StreamWriter sw = new StreamWriter("LocalState-" + processPId + "-" + round);
			sw.Write(output);
			sw.Close();
			}

			private void InjectDelayParal(string srcPID, string dstPID)
			{
				ICommands program;
				if (!TryGetProgram(srcPID, out program))
				{
					Console.WriteLine("cant find client to inject delay");
					return;
				}
				program.InjectDelay(dstPID, 1000);
			}

			private void UnfreezParal(string processPId)
			{
				ICommands program;
				if (!TryGetProgram(processPId, out program))
				{
					Console.WriteLine("cant find client to unfreez");
					return;
				}
				program.UnFreez();
			}

			private void FreezParal(string processPId)
			{
				ICommands program;
				if (!TryGetProgram(processPId, out program))
				{
					Console.WriteLine("cant find client to freez");
					return;
				}
				program.Freez();
			}
			private bool TryGetProgram(string processPId, out ICommands program)
			{
				int retry = 0;
				while (!activators.TryGetValue(processPId, out program))
				{
					Thread.Sleep(100);
					if (retry++ >= 5)
					{
						return false;
					}
				}
				return true;
			}

			private void StartClientParal(string clientPId, string PCSURL, string clientURL, string clientMSec, string filename)
			{
				CheckPCSConnection(PCSDic, PCSURL);
				PCSDic[PCSURL].StartClient(clientPId + " " + clientURL + " " + serverPId + " " + serverURL + " " + clientMSec + " " + filename);

				ICommands program = (ICommands)Activator.GetObject(typeof(ICommands), clientURL);
				CheckProgram(program, clientPId, clientURL);
			}

			private void StartServerParalel(string PCSURL, string mSec, string numOfPlayers)
			{
				CheckPCSConnection(PCSDic, PCSURL);
				PCSDic[PCSURL].StartServer(serverPId + " " + serverURL + " " + mSec + " " + numOfPlayers);

				ICommands program = (ICommands)Activator.GetObject(typeof(ICommands), serverURL);
				CheckProgram(program, serverPId, serverURL);
			}

			private void CheckProgram(ICommands program, string clientPId, string clientURL)
			{
				lock (this)
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
			}

			private void CheckPCSConnection(Dictionary<string, IServicePCS> dic, string PCSURL2)
			{
				lock (this)
				{
					if (!dic.ContainsKey(PCSURL2))
					{
						Console.WriteLine("Connecting to new PCS");
						dic[PCSURL2] = (IServicePCS)Activator.GetObject(typeof(IServicePCS), PCSURL2);
					}
				}
			}
			#endregion
		}
	}
