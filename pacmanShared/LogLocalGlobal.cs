using System;
using Shared;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace CommonTypes
{
	public static class LogLocalGlobal
	{
		public static void GlobalStatus(Game game)
		{
			Console.WriteLine("Global Status:");
			if (game == null)
				Console.WriteLine("Game not started");
			foreach (var client in game.Players)
			{
				Console.WriteLine("client: " + client.Key + " is in state " + (client.Value.state == State.Disconnected ? "offline" : "online"));
			}
		}

		public static string LocalState(Game game, string pId)
		{
			string output = "";
			if (game == null)
			{
				output = "Game not started";
			}
			else
			{
				foreach (var monster in game.Monsters)
				{
					output += "M, " + monster.X + ", " + monster.Y + "\n\r";
				}
				foreach (var player in game.Players)
				{
					output += player.Key + ", " + player.Value.state + ", " + player.Value.X + ", " + player.Value.Y + "\n\r";
				}
			}
			StreamWriter sw = new StreamWriter("LocalState-" + pId + "-" + game.RoundId);
			sw.Write(output);
			sw.Close();
			return output;
		}
	}
}
