using System;
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

		private int _frozenCount = 0;
		private bool _isFrozen = false;
		FreezeDelegate freeze;

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
				}
			}
		}

		public void UnFreez()
		{
			lock (this)
			{
				while (_frozenCount > 0)
				{
					Monitor.Pulse(this);
					_frozenCount--;
				}
				_isFrozen = false;
			}
		}

		public void Freeze(Delegate v, params object[] parameters)
		{
			freeze = Freez;
			freeze.BeginInvoke(v, parameters, null, null);
		}

		public void Freez(Delegate v, params object[] parameters)
		{
			IsFrozen();
			v.DynamicInvoke(parameters);
		}

		public void AddDelay(string pId, int length)
		{
			if (length == 0)
				_delays.Remove(pId);
			else
				_delays[pId] = length;
		}

		public IAsyncResult SendWithDelay(string pId, Delegate v, params object[] parameters)
		{
			send = SendDelay;
			IAsyncResult asyncResult = send.BeginInvoke(pId, v, parameters, null, null);
			return asyncResult;
		}

		public void SendDelay(string pId, Delegate v, params object[] parameters)
		{
			IsFrozen();
			if (_delays.ContainsKey(pId))
			{
				object[] p = new object[parameters.Length];
				for (int i = 0; i < parameters.Length; i++)
				{
					p[i] = DeepClone(parameters[i]);
				}
				Console.WriteLine(_delays[pId]);
				Thread.Sleep(_delays[pId]);
				v.DynamicInvoke(p);
			}
			else
			{
				v.DynamicInvoke(parameters);
			}
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
