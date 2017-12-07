
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace pacmanClient
{
	internal class Message
	{
		public int[] Clock;
		public int Id = 0;
		public string Msg = "";
	}

	internal class MessageQueue
	{
		#region private fields
		private int[] vectorClock;
		private int[] initVectorClock;
		private Collection<Message> messages = new Collection<Message>();
		private Collection<Message> pendingMessages = new Collection<Message>();
		private readonly int MAX_MESSAGES = 15;
		private int _numberOfClients;
		private int _myId;
		#endregion

		#region constructor...
		internal MessageQueue(int numberOfClients, int myId)
		{
			_myId = myId;
			_numberOfClients = numberOfClients;
			vectorClock = new int[numberOfClients];
			initVectorClock = new int[numberOfClients];
			for (int i = 0; i < numberOfClients; i++)
			{
				if (myId == -1)
					vectorClock[i] = -1; // client joined chat later, so values are unknow
				else
					vectorClock[i] = 0;
				initVectorClock[i] = 0;
			}
			for (int i = 0; i<MAX_MESSAGES;i++)
			{
				messages.Add(new Message() { Clock = vectorClock });
			}
		}

		internal void Init(int clientId, int[] clientVectorClocks)
		{
			// if client connected later. Need to get sequence number of others.
			vectorClock[clientId] = clientVectorClocks[clientId];
			// init vector clock to highests values
			// after all init clients he will find missing ID and assign
			// sequence number to continue
			int countMissingValues = 0;
			int missingValueId = 0;
			for (int i = 0; i < clientVectorClocks.Length; i++)
			{
				if(clientVectorClocks[i] > initVectorClock[i])
					initVectorClock[i] = clientVectorClocks[i];
				if(vectorClock[i] == -1)
				{
					countMissingValues++;
					missingValueId = i;
				}
			}
			// if only one value is missing, than it is my
			if(countMissingValues == 1)
			{
				_myId = missingValueId;
				vectorClock[_myId] = initVectorClock[_myId];
			}
			
		}
		#endregion

		#region internal methods...
		internal int GetMyId()
		{
			return _myId;
		}

		internal void NewMessage(int[] clocks, int id, string msg)
		{
			Console.Write("Clocks state:");
			foreach (int c in clocks)
			{
				Console.Write(" " + c);
			}
			Console.WriteLine();
			Message m = new Message() { Clock = clocks, Msg = msg, Id = id };
			if (IsValidMsg(m))
			{
				Console.WriteLine("New MSG");
				AddMessage(m);
				CheckPendingMessages();
			}
			else
			{
				Console.WriteLine("To pending");
				pendingMessages.Add(m);
			}
		}

		internal int[] GetVectorClock()
		{
			return vectorClock;
		}

		internal string GetAllMessages()
		{
			string text = "";
			foreach (Message m in messages)
			{
				text += m.Msg + "\r\n";
			}
			return text;
		}
		#endregion

		#region private methods
		private void CheckPendingMessages()
		{
			foreach (Message m in pendingMessages)
			{
				if (IsValidMsg(m))
				{
					AddMessage(m);
					pendingMessages.Remove(m);
					CheckPendingMessages();
					return;
				}
			}
		}

		private void AddMessage(Message m)
		{
			messages.RemoveAt(0);
			messages.Add(m);
		}

		private bool IsValidMsg(Message newMessage)
		{
			lock (this)
			{
				for (int i = 0; i < _numberOfClients; i++)
				{
					if (i == _myId)
						continue; // do not control with my seq number
					if (i == newMessage.Id)
					{
						// if new client connected insted of old crashed. One or more messages can missing
						// So I am not controlling order of messages from sending client(expectin TCP/IP FIFO)
						continue;
					}
					if (vectorClock[i] < newMessage.Clock[i])
					{
						Console.Write("index: " + i + " msg error - ");
						foreach (int c in newMessage.Clock)
						{
							Console.Write(" " + c);
						}
						Console.WriteLine();
						return false;
					}
				}
				vectorClock[newMessage.Id] = newMessage.Clock[newMessage.Id];
				return true;
			}
		}

		#endregion
	}
}
