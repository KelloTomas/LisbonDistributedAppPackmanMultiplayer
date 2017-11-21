
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
			for (int i = 0; i < numberOfClients; i++)
			{
				vectorClock[i] = 0;
			}
			for (int i = 0; i<MAX_MESSAGES;i++)
			{
				messages.Add(new Message() { Clock = vectorClock });
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
						if (vectorClock[i] + 1 != newMessage.Clock[i]) // order of msg of sender
						{
							Console.WriteLine("Sender BAD order detected - " + vectorClock[i] + ":" + newMessage.Clock[i]);
							return false;
						}
						continue;
					}
					if (vectorClock[i] < newMessage.Clock[i])
					{
						Console.WriteLine("index: " + i + "msg error - " + vectorClock[i] + ":" + newMessage.Clock[i]);
						return false;
					}
				}
				vectorClock[newMessage.Id]++;
				return true;
			}
		}
		#endregion
	}
}
