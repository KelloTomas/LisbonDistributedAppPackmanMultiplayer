﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
	public interface ICommands
	{
		void GlobalStatus();
		void InjectDelay(string PID, int mSecDelay);
		void Freez();
		void UnFreez();
		string LocalState(int roundId);
	}
}