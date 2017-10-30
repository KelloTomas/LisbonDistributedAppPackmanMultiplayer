using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Shared
{
    public static class CharactersSize
    {
        public static int Player = 25;
        public static int Monster = 30;
        public static int coin = 15;
    }

	[Serializable]
	public class Position
	{
        public Position()
        {
            Y = 0;
            X = 0;
        }
		public int Y { get; set; }
		public int X { get; set; }
	}

	[Serializable]
	public class Obsticle
	{
		public Position Corner1 { get; set; }
		public Position Corner2 { get; set; }
	}

	[Serializable]
	public class Character : Position
	{
        public Character()
        {
            Direction = Direction.No;
        }
		public Direction Direction { get; set; }
	}

	public interface IServiceClientWithState : IServiceClient
	{
		State State { get; set; }
	}

    [Serializable]
    public class Game
    {
        public Game()
        {
            Players = new Dictionary<string, Character>();
            Monsters = new List<Character>();
            Coins = new List<Position>();
            Obsticles = new List<Obsticle>();
        }
        public Dictionary<string, Character> Players { get; set; }
        public List<Character> Monsters { get; set; }
        public List<Position> Coins { get; set; }
        public List<Obsticle> Obsticles { get; set; }
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

	public enum State
	{
		Playing,
		Dead,
		Disconnected
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