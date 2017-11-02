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
		
        private int frozen_count = 0;
        private bool Freezed = false;
		public void SendWithDelay(string pId, Delegate v, params object[] parameters)
		{
			//ToDo Create thread, whole communication take a lot of time, so whole function should be in new thread
            lock (this)
            {
                if (Freezed)
                {
                    frozen_count++;
                    Monitor.Wait(this);
                }
                if (_delays.ContainsKey(pId))
                {
                    Thread.Sleep(_delays[pId]);
                }
                v.DynamicInvoke(parameters);
                if (frozen_count > 0)
                    Monitor.Pulse(this);
            }
		}
	}
}
