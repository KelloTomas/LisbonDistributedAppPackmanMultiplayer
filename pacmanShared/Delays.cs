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
		public void AddDelay(string pId, int length)
		{
			if (length == 0)
				_delays.Remove(pId);
			else
				_delays[pId] = length;
		}

		public void SendWithDelay(string pId, Delegate v, params object[] parameters)
		{
			send = SendDelay;
			send.BeginInvoke(pId, v, parameters, null, null);
		}

		public void SendDelay(string pId, Delegate v, params object[] parameters)
		{
			if (_delays.ContainsKey(pId))
			{
				object[] p = new object[parameters.Length];
				for (int i = 0; i < parameters.Length; i++)
				{
					p[i] = DeepClone(parameters[i]);
				}
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
