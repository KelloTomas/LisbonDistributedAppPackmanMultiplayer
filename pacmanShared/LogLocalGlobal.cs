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
		public static string LocalState(Game game, string pId)
		{
			if (game == null)
			{
				return "Game not started";
			}
			else
			{
				string output = "State in round: " + game.RoundId + "\n\r";
				foreach (var monster in game.Monsters)
				{
					output += "M, " + monster.X + ", " + monster.Y + "\n\r";
				}
				foreach (var player in game.Players)
				{
					output += player.Key + ", " + player.Value.state + ", " + player.Value.X + ", " + player.Value.Y + "\n\r";
				}
				foreach (var coin in game.Coins)
				{
					output += "C, " + coin.X + ", " + coin.Y + "\n\r";
				}
				return output;
			}
		}
	}
}
