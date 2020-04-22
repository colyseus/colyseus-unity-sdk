using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameDevWare.Serialization;

using UnityEngine;

namespace Colyseus
{
	public interface IMessageHandler
	{
		Type Type { get; }
		void Invoke(object message);
	}

	public class MessageHandler<T> : IMessageHandler
	{
		public Action<T> Action;

		public void Invoke(object message)
		{
			Action.Invoke((T)message);
		}

		public Type Type
		{
			get { return typeof(T); }
		}
	}
}
