using System;
using System.Threading;

namespace CommonTypes
{
	public class Frozens
	{

		private int _frozenCount = 0;
		private bool _isFrozen = false;
		private delegate void FreezeDelegate(Delegate v, params object[] parameters);
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
				while(_frozenCount > 0) {
					Monitor.Pulse(this);
					_frozenCount--;
				}
				_isFrozen = false;
				//Monitor.Pulse(this);
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
	}
}
