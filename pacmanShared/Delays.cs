using System;
using System.Collections.Generic;
using System.Threading;

namespace CommonTypes
{
	public class Delays
	{
		private Dictionary<string, int> _delays = new Dictionary<string, int>();

		public void AddDelay(string pId, int length)
		{
			if (length == 0)
				_delays.Remove(pId);
			else
				_delays[pId] = length;
		}
		
		public void SendWithDelay(string pId, Delegate v, params object[] parameters)
		{
			//ToDo Create thread, whole communication take a lot of time, so whole function should be in new thread
			if (_delays.ContainsKey(pId))
			{
				Thread.Sleep(_delays[pId]);
			}
			v.DynamicInvoke(parameters);
		}
	}
}
