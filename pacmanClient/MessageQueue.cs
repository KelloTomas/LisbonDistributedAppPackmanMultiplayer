
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace pacmanClient
{
	class Message
	{
		public int[] Clock;
		public int Id = 0;
		public string Msg = "";
	}
	internal class MessageQueue
	{
		int[] vectorClock;
		Collection<Message> messages = new Collection<Message>();
		Collection<Message> pendingMessages = new Collection<Message>();
		private readonly int MAX_MESSAGES = 10;
		private int _numberOfClients;
		private int _myId;

		internal int GetMyId()
		{
			return _myId;
		}
		internal MessageQueue(int numberOfClients, int myId)
		{
			_myId = myId;
			_numberOfClients = numberOfClients;
			vectorClock = new int[numberOfClients];
			for (int i = 0; i < numberOfClients; i++)
			{
				vectorClock[i] = 0;
			}
			messages.Add(new Message() { Clock = vectorClock});
		}

		internal void NewMessage(int[] clocks, int id, string msg)
		{
			lock(this)
			{
				Message m = new Message() { Clock = clocks, Msg = id + ": " + msg, Id =id };
				if (IsValidMsg(m))
				{
					AddMessage(m);
				}
				else
				{
					pendingMessages.Add(m);
				}
			}
		}

		private void CheckPendingMessages()
		{
			foreach(Message m in pendingMessages)
			{
				if (IsValidMsg(m))
				{
					AddMessage(m);
					return;
				}
			}
		}

		private void AddMessage(Message m)
		{
			if (messages.Count > MAX_MESSAGES)
				messages.RemoveAt(0);
			messages.Add(m);
			CheckPendingMessages();
		}

		private bool IsValidMsg(Message newMessage)
		{
			Message lastMsg = messages.Last();
			for (int i = 0; i < _numberOfClients; i++)
			{
				if (i == newMessage.Id)
					continue; // do not control his own msg
				if (newMessage.Clock[i] > lastMsg.Clock[i])
					return false;
			}
			return true;
		}

		internal int[] IncreaseVectorClock()
		{
			vectorClock[_myId]++;
			return vectorClock;
		}

		internal string GetAllMessages()
		{
			string text = "";
			foreach(Message m in messages)
			{
				text += m.Msg + "\r\n";
			}
			return text;
		}
	}
}
