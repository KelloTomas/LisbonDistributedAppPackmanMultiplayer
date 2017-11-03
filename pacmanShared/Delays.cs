using System;
using Shared;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace CommonTypes
{
	public class Delays
	{
		private Dictionary<string, int> _delays = new Dictionary<string, int>();
        private delegate void SendDelegate(string pId, Delegate v, params object[] parameters);
        SendDelegate send;
		public void AddDelay(string pId, int length)
		{
			if (length == 0)
				_delays.Remove(pId);
			else
				_delays[pId] = length;
		}

		public void Freez()
		{
			_isFrozen = true;
		}

		public void IsFrozen()
		{
			lock (this)
			{
				if (_isFrozen)
				{
					_frozenCount++;
					Monitor.Wait(this);
					if (_frozenCount > 0)
					{
						_frozenCount--;
						Monitor.Pulse(this);
					}
				}
			}
		}

		public void UnFreez()
		{
			_isFrozen = false;
			Monitor.Pulse(this);
		}

		private int _frozenCount = 0;
		private bool _isFrozen = false;
		public void SendWithDelay(string pId, Delegate v, params object[] parameters)
		{
            /*
			 * 
			 * TOMAS when I make lock, then one of calls stop and
			 * no other updates are sended. Need to fix it
			 * 
			 * 
			new Thread(() =>
			{
				Thread.CurrentThread.IsBackground = true;
				lock (this)
				{
					if (_isFrozen)
					{
						_frozenCount++;
						Monitor.Wait(this);
					}
					if (_delays.ContainsKey(pId))
					{
						if (_frozenCount > 0)
						{
							_frozenCount--;
							Monitor.Pulse(this);
						}
						Thread.Sleep(_delays[pId]);
					}
					*/
            //v.DynamicInvoke(parameters);
            /*
                if (_frozenCount > 0)
                {
                    _frozenCount--;
                    Monitor.Pulse(this);
                }
            }
        }).Start();
        */
            send = SendDelay;
            send.BeginInvoke(pId, v, parameters, null, null);
		}
        public void SendDelay(string pId, Delegate v, params object[] parameters)
        {            
            if (_delays.ContainsKey(pId))
            {
                if (parameters[0].GetType().Equals(typeof(Game)))
                {
                    parameters = new object[] { DeepClone(parameters[0]) };
                }
                Thread.Sleep(_delays[pId]);
            }
            v.DynamicInvoke(parameters);
        }
        public static object DeepClone(object obj)
        {
            object objResult = null;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, obj);

                ms.Position = 0;
                objResult = bf.Deserialize(ms);
            }
            return objResult;
        }

    }
}
