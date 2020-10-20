using System;
using System.IO;
using System.Collections.Generic;
// using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GameDevWare.Serialization;
using UnityEngine;

namespace Colyseus
{
	public delegate void ColyseusOpenEventHandler();
	public delegate void ColyseusCloseEventHandler(NativeWebSocket.WebSocketCloseCode code);
	public delegate void ColyseusErrorEventHandler(int code, string message);

    public interface IRoom
	{
		event ColyseusCloseEventHandler OnLeave;

		Task Connect();
		Task Leave(bool consented);
	}

	public class Room<T> : IRoom
	{
		public delegate void RoomOnMessageEventHandler(object message);
		public delegate void RoomOnStateChangeEventHandler(T state, bool isFirstState);

		public string Id;
		public string Name;
		public string SessionId;

		public Connection Connection;

		public string SerializerId;
		protected ISerializer<T> serializer;

		/// <summary>
		/// Occurs when the <see cref="Client"/> successfully connects to the <see cref="Room"/>.
		/// </summary>
		public event ColyseusOpenEventHandler OnJoin;

		/// <summary>
		/// Occurs when some error has been triggered in the room.
		/// </summary>
		public event ColyseusErrorEventHandler OnError;

		/// <summary>
		/// Occurs when <see cref="Client"/> leaves this room.
		/// </summary>
		public event ColyseusCloseEventHandler OnLeave;

		/// <summary>
		/// Occurs after applying the patched state on this <see cref="Room"/>.
		/// </summary>
		public event RoomOnStateChangeEventHandler OnStateChange;

		protected Dictionary<string, IMessageHandler> OnMessageHandlers = new Dictionary<string, IMessageHandler>();

		private Schema.Encoder Encode = Schema.Encoder.GetInstance();
		private Schema.Decoder Decode = Schema.Decoder.GetInstance();

		/// <summary>
		/// Initializes a new instance of the <see cref="Room"/> class.
		/// It synchronizes state automatically with the server and send and receive messaes.
		/// </summary>
		/// <param name="client">
		/// The <see cref="Client"/> client connection instance.
		/// </param>
		/// <param name="name">The name of the room</param>
		public Room (string name)
		{
			Name = name;
		}

		public async Task Connect()
		{
            await Connection.Connect();
		}

		public void SetConnection (Connection connection)
		{
			Connection = connection;

			Connection.OnClose += (code) => OnLeave?.Invoke(code);

			// TODO: expose WebSocket error code!
			// Connection.OnError += (code, message) => OnError?.Invoke(code, message);

			Connection.OnError += (message) => OnError?.Invoke(0, message);
			Connection.OnMessage += (bytes) => ParseMessage(bytes);
        }

		public void SetState(byte[] encodedState, int offset)
		{
			serializer.SetState(encodedState, offset);
			OnStateChange?.Invoke (serializer.GetState(), true);
		}

		public T State
		{
			get { return serializer.GetState(); }
		}

		/// <summary>
		/// Leave the room.
		/// </summary>
		public async Task Leave (bool consented = true)
		{
			if (Id != null) {
				if (consented)
				{
					await Connection.Send(new byte[] { Protocol.LEAVE_ROOM });
				}
				else
				{
					await Connection.Close();
				}

			} else if (OnLeave != null) {
				OnLeave?.Invoke (NativeWebSocket.WebSocketCloseCode.Normal);
			}
		}

		/// <summary>
		/// Send a message by number type, without payload
		/// </summary>
		/// <param name="type">Message type</param>
		public async Task Send (byte type)
		{
			await Connection.Send(new byte[] { Protocol.ROOM_DATA, type });
		}
		/// <summary>
		/// Send a message by number type with payload
		/// </summary>
		/// <param name="type">Message type</param>
		/// <param name="message">Message payload</param>
		public async Task Send(byte type, object message)
		{
			var serializationOutput = new MemoryStream();
			MsgPack.Serialize(message, serializationOutput, SerializationOptions.SuppressTypeInformation);

			byte[] initialBytes = { Protocol.ROOM_DATA, type };
			byte[] encodedMessage = serializationOutput.ToArray();

			byte[] bytes = new byte[initialBytes.Length + encodedMessage.Length];
			Buffer.BlockCopy(initialBytes, 0, bytes, 0, initialBytes.Length);
			Buffer.BlockCopy(encodedMessage, 0, bytes, initialBytes.Length, encodedMessage.Length);

			await Connection.Send(bytes);
		}

		/// <summary>
		/// Send a message by string type, without payload
		/// </summary>
		/// <param name="type">Message type</param>
		public async Task Send(string type)
		{
			byte[] encodedType = System.Text.Encoding.UTF8.GetBytes(type);
			byte[] initialBytes = Encode.getInitialBytesFromEncodedType(encodedType);

			byte[] bytes = new byte[initialBytes.Length + encodedType.Length];
			Buffer.BlockCopy(initialBytes, 0, bytes, 0, initialBytes.Length);
			Buffer.BlockCopy(encodedType, 0, bytes, initialBytes.Length, encodedType.Length);

			await Connection.Send(bytes);
		}

		/// <summary>
		/// Send a message by string type with payload
		/// </summary>
		/// <param name="type">Message type</param>
		/// <param name="message">Message payload</param>
		public async Task Send(string type, object message)
		{
			var serializationOutput = new MemoryStream();
			MsgPack.Serialize(message, serializationOutput, SerializationOptions.SuppressTypeInformation);

			byte[] encodedType = System.Text.Encoding.UTF8.GetBytes(type);
			byte[] initialBytes = Encode.getInitialBytesFromEncodedType(encodedType);
			byte[] encodedMessage = serializationOutput.ToArray();

			byte[] bytes = new byte[encodedType.Length + encodedMessage.Length + initialBytes.Length];
			Buffer.BlockCopy(initialBytes, 0, bytes, 0, initialBytes.Length);
			Buffer.BlockCopy(encodedType, 0, bytes, initialBytes.Length, encodedType.Length);
			Buffer.BlockCopy(encodedMessage, 0, bytes, initialBytes.Length + encodedType.Length, encodedMessage.Length);

			await Connection.Send(bytes);
		}

		public void OnMessage<MessageType>(string type, Action<MessageType> handler)
		{
			OnMessageHandlers.Add(type, new MessageHandler<MessageType>
			{
				Action = handler
			});
		}

		public void OnMessage<MessageType>(byte type, Action<MessageType> handler)
		{
			OnMessageHandlers.Add("i" + type.ToString(), new MessageHandler<MessageType>
			{
				Action = handler
			});
		}

		public void OnMessage<MessageType>(Action<MessageType> handler) where MessageType : Schema.Schema, new()
		{
			OnMessageHandlers.Add("s" + typeof(MessageType), new MessageHandler<MessageType>
			{
				Action = handler
			});
		}

		protected async void ParseMessage (byte[] bytes)
		{
			byte code = bytes[0];

			if (code == Protocol.JOIN_ROOM)
			{
				var offset = 1;

				SerializerId = System.Text.Encoding.UTF8.GetString(bytes, offset+1, bytes[offset]);
				offset += SerializerId.Length + 1;

				if (SerializerId == "schema")
				{
					try
					{
						serializer = new SchemaSerializer<T>();
					}
					catch (Exception e)
					{
						DisplaySerializerErrorHelp(e, "Consider using the \"schema-codegen\" and providing the same room state for matchmaking instead of \"" + typeof(T).Name + "\"");
					}

				} else if (SerializerId == "fossil-delta")
				{
					try
					{
						serializer = (ISerializer<T>)new FossilDeltaSerializer();
					} catch (Exception e)
					{
						DisplaySerializerErrorHelp(e, "Consider using \"IndexedDictionary<string, object>\" instead of \"" + typeof(T).Name + "\" for matchmaking.");
					}
				} else
				{
					try
					{
						serializer = (ISerializer<T>)new NoneSerializer();
					}
					catch (Exception e)
					{
						DisplaySerializerErrorHelp(e, "Consider setting state in the server-side using \"this.setState(new " + typeof(T).Name + "())\"");
					}
				}

				if (bytes.Length > offset)
				{
					serializer.Handshake(bytes, offset);
				}

				OnJoin?.Invoke();

				// Acknowledge JOIN_ROOM
				await Connection.Send(new byte[] { Protocol.JOIN_ROOM });
			}
			else if (code == Protocol.ERROR)
			{
				Schema.Iterator it = new Schema.Iterator { Offset = 1 };
				var errorCode = Decode.DecodeNumber(bytes, it);
				var errorMessage = Decode.DecodeString(bytes, it);
				OnError?.Invoke((int) errorCode, errorMessage);

			}
			else if (code == Protocol.ROOM_DATA_SCHEMA)
			{
				Schema.Iterator it = new Schema.Iterator { Offset = 1 };
				var typeId = Decode.DecodeNumber(bytes, it);

				Type messageType = Schema.Context.GetInstance().Get(typeId);
				var message = (Schema.Schema) Activator.CreateInstance(messageType);

				message.Decode(bytes, it);

				IMessageHandler handler = null;
				OnMessageHandlers.TryGetValue("s" + message.GetType(), out handler);

				if (handler != null)
				{
					handler.Invoke(message);
				}
				else
				{
					Debug.LogWarning("room.OnMessage not registered for Schema of type: '" + message.GetType() + "'");
				}
			}
			else if (code == Protocol.LEAVE_ROOM)
			{
				await Leave();

			}
			else if (code == Protocol.ROOM_STATE)
			{
				Debug.Log("ROOM_STATE");
				SetState(bytes, 1);
			}
			else if (code == Protocol.ROOM_STATE_PATCH)
			{
				Debug.Log("ROOM_STATE_PATCH");
				Patch(bytes, 1);
			}
			else if (code == Protocol.ROOM_DATA)
			{
				IMessageHandler handler = null;
				object type;

				Schema.Iterator it = new Schema.Iterator { Offset = 1 };

				if (Decode.NumberCheck(bytes, it))
				{
					type = Decode.DecodeNumber(bytes, it);
					OnMessageHandlers.TryGetValue("i" + type, out handler);

				} else
				{
					type = Decode.DecodeString(bytes, it);
					OnMessageHandlers.TryGetValue(type.ToString(), out handler);
				}

				if (handler != null)
				{
					//
					// MsgPack deserialization can be optimized:
					// https://github.com/deniszykov/msgpack-unity3d/issues/23
					//
					var message = (bytes.Length > it.Offset)
						? MsgPack.Deserialize(handler.Type, new MemoryStream(bytes, it.Offset, bytes.Length - it.Offset, false))
						: null;

					handler.Invoke(message);
				}
				else
				{
					Debug.LogWarning("room.OnMessage not registered for: '" + type + "'");
				}
			}
		}

		protected void Patch (byte[] delta, int offset)
		{
			serializer.Patch(delta, offset);
			OnStateChange?.Invoke(serializer.GetState(), false);
		}

		protected void DisplaySerializerErrorHelp(Exception e, string helpMessage)
		{
			Debug.LogWarning("The serializer from the server is: '" + SerializerId + "'. " + helpMessage);
			throw e;
		}
	}

}
