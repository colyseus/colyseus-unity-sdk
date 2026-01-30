using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Colyseus.Schema;
using GameDevWare.Serialization;
using NativeWebSocket;
using UnityEngine;

namespace Colyseus
{
    using Decode = Schema.Utils.Decode;
    using Encode = Schema.Utils.Encode;

    public delegate void NoArgsEventHandler();

    /// <summary>
    ///     Delegate function for when <see cref="Client" /> leaves this room.
    /// </summary>
    /// <param name="code">Reason for closure</param>
    public delegate void CloseWithCodeEventHandler(int code); // , string reason

    /// <summary>
    ///     Delegate function for when some error has been triggered in the room.
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="message">Error message</param>
    public delegate void ErrorEventHandler(int code, string message);

    /// <summary>
    ///     Interface for functions expected of any <see cref="Room{T}"></see>.
    /// </summary>
    public interface IRoom
    {
        event CloseWithCodeEventHandler OnLeave;

        /// <summary>
        ///     Connection task
        /// </summary>
        /// <returns>Task that completes upon connection (or failure to connect)</returns>
        Task Connect();

        /// <summary>
        ///     Disconnection task
        /// </summary>
        /// <param name="consented">True if by user's choice, false otherwise</param>
        /// <returns>Task that completes upon Leaving</returns>
        Task Leave(bool consented);
    }

    [Serializable]
    public class ReconnectionToken
    {
        public string RoomId;
        public string Token;
    }

    /// <summary>
    ///     Configuration options for automatic reconnection behavior
    /// </summary>
    [Serializable]
    public class ReconnectionOptions
    {
        /// <summary>
        ///     The maximum number of reconnection attempts.
        /// </summary>
        public int MaxRetries = 15;

        /// <summary>
        ///     The minimum delay between reconnection attempts (in milliseconds).
        /// </summary>
        public int MinDelay = 100;

        /// <summary>
        ///     The maximum delay between reconnection attempts (in milliseconds).
        /// </summary>
        public int MaxDelay = 5000;

        /// <summary>
        ///     The minimum uptime of the room before reconnection attempts can be made (in milliseconds).
        /// </summary>
        public int MinUptime = 5000;

        /// <summary>
        ///     The current number of reconnection attempts.
        /// </summary>
        public int RetryCount = 0;

        /// <summary>
        ///     The initial delay between reconnection attempts (in milliseconds).
        /// </summary>
        public int Delay = 100;

        /// <summary>
        ///     Whether the room is currently reconnecting.
        /// </summary>
        public bool IsReconnecting = false;

        /// <summary>
        ///     The maximum number of enqueued messages to buffer.
        /// </summary>
        public int MaxEnqueuedMessages = 10;

        /// <summary>
        ///     Buffer for messages sent while connection is not open.
        ///     These messages will be sent once the connection is re-established.
        /// </summary>
        public List<byte[]> EnqueuedMessages = new List<byte[]>();

        /// <summary>
        ///     The function to calculate the delay between reconnection attempts.
        /// </summary>
        public Func<int, int, int> Backoff = ExponentialBackoff;

        /// <summary>
        ///     Default exponential backoff function.
        /// </summary>
        /// <param name="attempt">The current attempt number.</param>
        /// <param name="delay">The initial delay between reconnection attempts.</param>
        /// <returns>The delay between reconnection attempts.</returns>
        public static int ExponentialBackoff(int attempt, int delay)
        {
            return (int)Math.Floor(Math.Pow(2, attempt) * delay);
        }
    }

    public class Room<T> : IRoom where T : Schema.Schema
    {
        /// <summary>
        ///     Delegate for room state changes
        /// </summary>
        /// <param name="state">The state change received</param>
        /// <param name="isFirstState">Flag if first state received</param>
        public delegate void StateChangeEventHandler(T state, bool isFirstState);

        /// <summary>
        ///     Reference to the room's WebSocket Connection
        /// </summary>
        public Connection Connection;

        /// <summary>
        ///     Room ID
        /// </summary>
        public string RoomId;

        /// <summary>
        ///     Room name
        /// </summary>
        public string Name;

        /// <summary>
        ///     Dictionary of the message handlers that have been provided to the room
        /// </summary>
        protected Dictionary<string, IMessageHandler> OnMessageHandlers =
            new Dictionary<string, IMessageHandler>();

        /// <summary>
        ///     Reference to the Serializer this room uses, determined and then generated based on the <see cref="SerializerId" />
        /// </summary>
        internal ISerializer<T> Serializer;

        /// <summary>
        ///     ID to determine which kind of serializer this room uses (<see cref="SchemaSerializer{T}" /> or
        ///     <see cref="FossilDeltaSerializer" />)
        /// </summary>
        public string SerializerId;

        /// <summary>
        ///     The room's session ID
        /// </summary>
        public string SessionId;

        /// <summary>
        ///     Reconnection Token for this room session. (must be provided for client.Reconnect())
        /// </summary>
        public ReconnectionToken ReconnectionToken;

        /// <summary>
        ///     Configuration options for automatic reconnection behavior.
        /// </summary>
        public ReconnectionOptions Reconnection = new ReconnectionOptions();

        /// <summary>
        ///     Timestamp when the room was joined (used for minUptime check).
        /// </summary>
        protected long JoinedAtTime = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Room{T}" /> class.
        ///     It synchronizes state automatically with the server and send and receive messaes.
        /// </summary>
        /// <param name="name">The Room identifier</param>
        public Room(string name)
        {
            Name = name;

            OnLeave += (code) => Destroy();

#if UNITY_EDITOR
            // Register handler for editor Room Inspector message type capture
            // Uses reflection to avoid assembly reference from runtime to editor
            OnMessage<Dictionary<string, object>>("__playground_message_types", (messageTypes) =>
            {
                try
                {
                    var editorAssembly = System.Reflection.Assembly.Load("Colyseus.Editor");
                    if (editorAssembly != null)
                    {
                        var captureType = editorAssembly.GetType("Colyseus.Editor.RoomMessageType");
                        if (captureType != null)
                        {
                            var field = captureType.GetField("CapturedMessageTypes",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            if (field != null)
                            {
                                var dict = field.GetValue(null) as Dictionary<string, Dictionary<string, object>>;
                                if (dict != null && !string.IsNullOrEmpty(RoomId))
                                {
                                    dict[RoomId] = messageTypes;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Silently ignore if editor assembly is not available
                }
            });
#endif
        }

        /// <summary>
        ///     Getter for the <see cref="Room{T}" />'s current state
        /// </summary>
        public T State
        {
            get { return Serializer.GetState(); }
        }

        [Obsolete(".Id is deprecated. Please use .RoomId instead.")]
        public string Id
        {
            get { return RoomId; }
        }

        /// <summary>
        ///     Occurs when <see cref="Client" /> leaves this room.
        /// </summary>
        public event CloseWithCodeEventHandler OnLeave;

        /// <summary>
        ///     Implementation of <see cref="IRoom.Connect" />
        /// </summary>
        /// <returns>Response from <see cref="Connection"></see>.Connect()</returns>
        public async Task Connect()
        {
            await Connection.Connect();
        }

        /// <summary>
        ///     Leave the room
        /// </summary>
        /// <param name="consented">If the user agreed to this disconnection</param>
        /// <returns>Connection closure depending on user consent</returns>
        public async Task Leave(bool consented = true)
        {
            if (!Connection.IsOpen) {
                return;
            }

            if (RoomId != null)
            {
                if (consented)
                {
                    await Connection.Send(new[] {Protocol.LEAVE_ROOM});
                }
                else
                {
                    await Connection.Close();
                }
            }
            else
            {
                OnLeave?.Invoke((int)WebSocketCloseCode.Normal);
            }
        }

        // Internal OnJoin event. It is used by Client.cs during matchmaking.
        internal event NoArgsEventHandler OnJoin;

        // <summary>
        ///     Occurs when the room connection is dropped unexpectedly.
		///     Use to notify the user that a reconnection is being made.
        /// </summary>
        public event CloseWithCodeEventHandler OnDrop;

        /// <summary>
        ///     Occurs when automatically reconnected to the room after a connection drop.
        /// </summary>
        public event NoArgsEventHandler OnReconnect;

        /// <summary>
        ///     Occurs when some error has been triggered in the room.
        /// </summary>
        public event ErrorEventHandler OnError;

        /// <summary>
        ///     Occurs after applying the patched state on this <see cref="Room{T}" />.
        /// </summary>
        public event StateChangeEventHandler OnStateChange;

		/// <summary>
		///     Called by the <see cref="Client" /> upon connection to a room
		/// </summary>
		/// <param name="connection">The connection created by the client</param>
		public void SetConnection(Connection connection)
		{
	        Connection = connection;

	        Connection.OnClose += code => {
				if (
					code == (int) CloseCode.NO_STATUS_RECEIVED ||
					code == (int) CloseCode.ABNORMAL_CLOSURE ||
					code == (int) CloseCode.GOING_AWAY ||
					code == (int) CloseCode.MAY_TRY_RECONNECT
				) {
					OnDrop?.Invoke(code);
					HandleReconnection();

				} else {
					OnLeave?.Invoke(code);
				}
	        };

	        // TODO: expose WebSocket error code!
	        // Connection.OnError += (code, message) => OnError?.Invoke(code, message);

	        Connection.OnError += message => this.OnError?.Invoke(0, message);
	        Connection.OnMessage += bytes => this.ParseMessage(bytes);
        }

        /// <summary>
        ///     Response to state changes received as messages
        /// </summary>
        /// ///
        /// <remarks>Invokes everything subscribed to <see cref="OnStateChange" /></remarks>
        /// <param name="encodedState">Byte array of the new state data</param>
        /// <param name="offset">Offset to provide the room's <see cref="Serializer" /></param>
        public void SetState(byte[] encodedState, int offset)
        {
            Serializer.SetState(encodedState, offset);
            OnStateChange?.Invoke(Serializer.GetState(), true);
        }

        /// <summary>
        ///     Send a message by number type, without payload
        /// </summary>
        /// <param name="type">Message type</param>
        public async Task Send(byte type)
        {
            byte[] bytes = new[] {Protocol.ROOM_DATA, type};

            // If connection is not open, buffer the message
            if (!Connection.IsOpen)
            {
                EnqueueMessage(bytes);
                return;
            }

            await Connection.Send(bytes);
        }

        /// <summary>
        ///     Send a message by number type with payload
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="message">Message payload</param>
        public async Task Send(byte type, object message)
        {
            MemoryStream serializationOutput = new MemoryStream();
            MsgPack.Serialize(message, serializationOutput, SerializationOptions.SuppressTypeInformation);

            byte[] initialBytes = {Protocol.ROOM_DATA, type};
            byte[] encodedMessage = serializationOutput.ToArray();

            byte[] bytes = new byte[initialBytes.Length + encodedMessage.Length];
            Buffer.BlockCopy(initialBytes, 0, bytes, 0, initialBytes.Length);
            Buffer.BlockCopy(encodedMessage, 0, bytes, initialBytes.Length, encodedMessage.Length);

            // If connection is not open, buffer the message
            if (!Connection.IsOpen)
            {
                EnqueueMessage(bytes);
                return;
            }

            await Connection.Send(bytes);
        }

        /// <summary>
        ///     Send a message by string type, without payload
        /// </summary>
        /// <param name="type">Message type</param>
        public async Task Send(string type)
        {
            byte[] encodedType = Encoding.UTF8.GetBytes(type);
            byte[] initialBytes = Encode.getInitialBytesFromEncodedType(encodedType, Protocol.ROOM_DATA);

            byte[] bytes = new byte[initialBytes.Length + encodedType.Length];
            Buffer.BlockCopy(initialBytes, 0, bytes, 0, initialBytes.Length);
            Buffer.BlockCopy(encodedType, 0, bytes, initialBytes.Length, encodedType.Length);

            // If connection is not open, buffer the message
            if (!Connection.IsOpen)
            {
                EnqueueMessage(bytes);
                return;
            }

            await Connection.Send(bytes);
        }

        /// <summary>
        ///     Send a message by string type with payload
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="message">Message payload</param>
        public async Task Send(string type, object message)
        {
            MemoryStream serializationOutput = new MemoryStream();
            MsgPack.Serialize(message, serializationOutput, SerializationOptions.SuppressTypeInformation);

            byte[] encodedType = Encoding.UTF8.GetBytes(type);
            byte[] initialBytes = Encode.getInitialBytesFromEncodedType(encodedType, Protocol.ROOM_DATA);
            byte[] encodedMessage = serializationOutput.ToArray();

            byte[] bytes = new byte[encodedType.Length + encodedMessage.Length + initialBytes.Length];
            Buffer.BlockCopy(initialBytes, 0, bytes, 0, initialBytes.Length);
            Buffer.BlockCopy(encodedType, 0, bytes, initialBytes.Length, encodedType.Length);
            Buffer.BlockCopy(encodedMessage, 0, bytes, initialBytes.Length + encodedType.Length, encodedMessage.Length);

            // If connection is not open, buffer the message
            if (!Connection.IsOpen)
            {
                EnqueueMessage(bytes);
                return;
            }

            await Connection.Send(bytes);
        }

        /// <summary>
        ///     Send a message by number type with raw bytes payload
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="bytes">Message payload</param>
        public async Task SendBytes(byte type, byte[] bytes)
        {
            byte[] initialBytes = { Protocol.ROOM_DATA_BYTES, type };

            byte[] bytesToSend = new byte[initialBytes.Length + bytes.Length];
            Buffer.BlockCopy(initialBytes, 0, bytesToSend, 0, initialBytes.Length);
            Buffer.BlockCopy(bytes, 0, bytesToSend, initialBytes.Length, bytes.Length);

            // If connection is not open, buffer the message
            if (!Connection.IsOpen)
            {
                EnqueueMessage(bytesToSend);
                return;
            }

            await Connection.Send(bytesToSend);
        }

        /// <summary>
        ///     Send a message by string type with raw bytes payload
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="bytes">Message payload</param>
        public async Task SendBytes(string type, byte[] bytes)
        {
            byte[] encodedType = Encoding.UTF8.GetBytes(type);
            byte[] initialBytes = Encode.getInitialBytesFromEncodedType(encodedType, Protocol.ROOM_DATA_BYTES);

            byte[] bytesToSend = new byte[encodedType.Length + bytes.Length + initialBytes.Length];
            Buffer.BlockCopy(initialBytes, 0, bytesToSend, 0, initialBytes.Length);
            Buffer.BlockCopy(encodedType, 0, bytesToSend, initialBytes.Length, encodedType.Length);
            Buffer.BlockCopy(bytes, 0, bytesToSend, initialBytes.Length + encodedType.Length, bytes.Length);

            // If connection is not open, buffer the message
            if (!Connection.IsOpen)
            {
                EnqueueMessage(bytesToSend);
                return;
            }

            await Connection.Send(bytesToSend);
        }

        /// <summary>
        ///     Method to add new message handlers to the room
        /// </summary>
        /// <param name="type">The type of message received</param>
        /// <param name="handler"></param>
        /// <typeparam name="MessageType">The type of object this message should respond with</typeparam>
        public void OnMessage<MessageType>(string type, Action<MessageType> handler)
        {
            OnMessageHandlers.Add(type, new MessageHandler<MessageType>
            {
                Action = handler
            });
        }

        /// <summary>
        ///     Method to add new message handlers to the room
        /// </summary>
        /// <param name="type">The type of message received</param>
        /// <param name="handler"></param>
        /// <typeparam name="MessageType">The type of object this message should respond with</typeparam>
        public void OnMessage<MessageType>(byte type, Action<MessageType> handler)
        {
            OnMessageHandlers.Add("i" + type, new MessageHandler<MessageType>
            {
                Action = handler
            });
        }

        /// <summary>
        ///     The function that will be called when the <see cref="Connection" /> receives a message
        /// </summary>
        /// <param name="bytes">The message as provided from the <see cref="Connection" /></param>
        protected async void ParseMessage(byte[] bytes)
        {
            byte code = bytes[0];

            if (code == Protocol.JOIN_ROOM)
            {
                int offset = 1;

                var reconnectionToken = Encoding.UTF8.GetString(bytes, offset + 1, bytes[offset]);
                offset += reconnectionToken.Length + 1;

                SerializerId = Encoding.UTF8.GetString(bytes, offset + 1, bytes[offset]);
                offset += SerializerId.Length + 1;

                if (SerializerId == "schema")
                {
                    try
                    {
                        Serializer = new SchemaSerializer<T>();
                    }
                    catch (Exception e)
                    {
                        DisplaySerializerErrorHelp(e,
                            "Consider using the \"schema-codegen\" and providing the same room state for matchmaking instead of \"" +
                            typeof(T).Name + "\"");
                    }
                }
                else if (SerializerId == "fossil-delta")
                {
                    Debug.LogError(
                        "FossilDelta Serialization has been deprecated. It is highly recommended that you update your code to use the Schema Serializer. Otherwise, you must use an earlier version of the Colyseus plugin");
                }
                else
                {
                    Serializer = (ISerializer<T>) new NoneSerializer();
                }

                if (bytes.Length > offset)
                {
	                try {
		                Serializer.Handshake(bytes, offset);
	                }
	                catch (Exception e)
	                {
		                await Leave(false);
		                OnError?.Invoke(ErrorCode.SCHEMA_MISMATCH, e.Message);
		                return;
	                }
                }

                ReconnectionToken = new ReconnectionToken()
                {
                    RoomId = RoomId,
                    Token = reconnectionToken
                };

                if (JoinedAtTime == 0)
                {
                    // First time joining
                    JoinedAtTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    OnJoin?.Invoke();
                }
                else
                {
                    // Successful reconnection
                    Debug.Log("[Colyseus reconnection]: Reconnection successful!");
                    Reconnection.IsReconnecting = false;
                    OnReconnect?.Invoke();
                }

                // Acknowledge JOIN_ROOM
                await Connection.Send(new[] {Protocol.JOIN_ROOM});

                // Flush any enqueued messages that were buffered while disconnected
                await FlushEnqueuedMessages();
            }
            else if (code == Protocol.ERROR)
            {
                Iterator it = new Iterator {Offset = 1};
                float errorCode = Decode.DecodeNumber(bytes, it);
                string errorMessage = Decode.DecodeString(bytes, it);
                OnError?.Invoke((int) errorCode, errorMessage);
            }
            else if (code == Protocol.LEAVE_ROOM)
            {
                await Leave();
            }
            else if (code == Protocol.ROOM_STATE)
            {
	            SetState(bytes, 1);
            }
            else if (code == Protocol.ROOM_STATE_PATCH)
            {
                Patch(bytes, 1);
            }
            else if (code == Protocol.ROOM_DATA || code == Protocol.ROOM_DATA_BYTES)
            {
                IMessageHandler handler = null;
                object type;

                Iterator it = new Iterator {Offset = 1};

                if (Decode.NumberCheck(bytes, it))
                {
                    type = Decode.DecodeNumber(bytes, it);
                    OnMessageHandlers.TryGetValue("i" + type, out handler);
                }
                else
                {
                    type = Decode.DecodeString(bytes, it);
                    OnMessageHandlers.TryGetValue(type.ToString(), out handler);
                }

                if (handler != null)
                {
                    object message = null;

                    if ( code == Protocol.ROOM_DATA )
                    {
						Debug.Log($"[Room] Received message: {type}");
                        //
                        // MsgPack deserialization can be optimized:
                        // https://github.com/deniszykov/msgpack-unity3d/issues/23
                        //
                        message = bytes.Length > it.Offset
                            ? MsgPack.Deserialize(handler.Type,
                                new MemoryStream(bytes, it.Offset, bytes.Length - it.Offset, false))
                            : null;
                    }
                    else if ( code == Protocol.ROOM_DATA_BYTES )
                    {
                        message = new byte[bytes.Length - it.Offset];
                        Buffer.BlockCopy(bytes, it.Offset, (byte[])message, 0, bytes.Length - it.Offset);
                    }

                    handler.Invoke(message);
                }
                else
                {
                    Debug.LogWarning("room.OnMessage not registered for: '" + type + "'");
                }
            }
        }

        /// <summary>
        ///     Update the state with just the new changes to the state
        /// </summary>
        /// <remarks>Invokes everything subscribed to <see cref="OnStateChange" /></remarks>
        /// <param name="delta">The updates to the state</param>
        /// <param name="offset">Offset to provide the room's <see cref="Serializer" /></param>
        protected void Patch(byte[] delta, int offset)
        {
            Serializer.Patch(delta, offset);
            OnStateChange?.Invoke(Serializer.GetState(), false);
        }

        /// <summary>
        ///     Helper function to display errors with de-serializing messages from server
        /// </summary>
        /// <param name="e">Exception information</param>
        /// <param name="helpMessage">Additional information to display</param>
        /// <exception cref="Exception">Throws <paramref name="e" /></exception>
        protected void DisplaySerializerErrorHelp(Exception e, string helpMessage)
        {
            Debug.LogWarning("The serializer from the server is: '" + SerializerId + "'. " + helpMessage);
            throw e;
        }

		private void HandleReconnection()
		{
			// Check minimum uptime before allowing reconnection
			long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (currentTime - JoinedAtTime < Reconnection.MinUptime)
			{
				Debug.Log($"[Colyseus reconnection]: Room not up long enough for auto-reconnect (min uptime: {Reconnection.MinUptime}ms)");
				OnLeave?.Invoke((int)CloseCode.ABNORMAL_CLOSURE);
				return;
			}

			if (!Reconnection.IsReconnecting)
			{
				Reconnection.RetryCount = 0;
				Reconnection.IsReconnecting = true;
			}

			_ = RetryReconnection();
		}

		private async Task RetryReconnection()
		{
			Reconnection.RetryCount++;

			int delay = Math.Min(Reconnection.MaxDelay,
				Math.Max(Reconnection.MinDelay,
					Reconnection.Backoff(Reconnection.RetryCount, Reconnection.Delay)));

			Debug.Log($"[Colyseus reconnection]: Will retry in {delay / 1000f:F1} seconds...");
			await Task.Delay(delay);

			try
			{
				Debug.Log($"[Colyseus reconnection]: Re-establishing sessionId '{SessionId}' with roomId '{RoomId}'... (attempt {Reconnection.RetryCount} of {Reconnection.MaxRetries})");
				await Connection.Reconnect(ReconnectionToken.Token);
			}
			catch (Exception e)
			{
				Debug.Log($"[Colyseus reconnection]: Reconnect failed - {e.Message}");

				if (Reconnection.RetryCount < Reconnection.MaxRetries)
				{
					_ = RetryReconnection();
				}
				else
				{
					Debug.Log("[Colyseus reconnection]: Max retries reached. Giving up.");
					Reconnection.IsReconnecting = false;
					OnLeave?.Invoke((int)CloseCode.ABNORMAL_CLOSURE);
				}
			}
		}

		/// <summary>
		///     Enqueue a message to be sent when the connection is re-established.
		/// </summary>
		/// <param name="data">The message data to enqueue</param>
		private void EnqueueMessage(byte[] data)
		{
			Reconnection.EnqueuedMessages.Add(data);
			if (Reconnection.EnqueuedMessages.Count > Reconnection.MaxEnqueuedMessages)
			{
				Reconnection.EnqueuedMessages.RemoveAt(0);
			}
		}

		/// <summary>
		///     Flush all enqueued messages after reconnection.
		/// </summary>
		private async Task FlushEnqueuedMessages()
		{
			if (Reconnection.EnqueuedMessages.Count == 0) return;

			foreach (var message in Reconnection.EnqueuedMessages)
			{
				await Connection.Send(message);
			}
			Reconnection.EnqueuedMessages.Clear();
		}

		private void Destroy()
		{
			Serializer.Teardown();
		}
    }
}
