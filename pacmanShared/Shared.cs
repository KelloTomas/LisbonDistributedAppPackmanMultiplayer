using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Shared
{
	[Serializable]
	public class Position
	{
		public int Y { get; set; }
		public int X { get; set; }
	}

	[Serializable]
	public class Character : Position
	{
		public Direction Direction { get; set; }
	}

	[Serializable]
	public class Game
	{
		public Dictionary<string, Character> Players { get; set; }
		public List<Character> Monsters { get; set; }
		public List<Position> Coins { get; set; }
	}

	[Serializable]
	public class Client
	{
		public Client(string pId, string URL)
		{
			PId = pId;
			this.URL = URL;
		}

		public string PId { get; private set; }
		public string URL { get; private set; }
	}

	[Serializable]
	public enum Direction
	{
		No,
		Up,
		Down,
		Left,
		Right
	}

	public enum URLparts
	{
		IP,
		Port,
		Link
	}

	public static class Shared
	{
		public static string ParseUrl(URLparts part, string URL)
		{
			string pattern;
			switch (part)
			{
				case URLparts.IP:
					pattern = ".*//(.*):.*";
					break;
				case URLparts.Port:
					pattern = ".*:(.*)/.*";
					break;
				case URLparts.Link:
				default:
					pattern = ".*:.*/(.*)";
					break;
			}
			Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
			Match m = rgx.Match(URL);
			string ret = m.Groups[1].ToString();
			return ret;
		}
	}
}