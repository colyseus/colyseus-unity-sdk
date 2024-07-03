using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Colyseus.Schema;
using GameDevWare.Serialization;
using NativeWebSocket;
using UnityEngine;
using Type = System.Type;

// using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace Colyseus
{
    /// <summary>
    ///     Delegate function for when the <see cref="ColyseusClient" /> successfully connects to the
    ///     <see cref="ColyseusRoom{T}" />.
    /// </summary>
    public delegate void ColyseusOpenEventHandler();

    /// <summary>
    ///     Delegate function for when <see cref="ColyseusClient" /> leaves this room.
    /// </summary>
    /// <param name="code">Reason for closure</param>
    public delegate void ColyseusCloseEventHandler(int code);

    /// <summary>
    ///     Delegate function for when some error has been triggered in the room.
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="message">Error message</param>
    public delegate void ColyseusErrorEventHandler(int code, string message);

    /// <summary>
    ///     Interface for functions expected of any <see cref="ColyseusRoom{T}"></see>.
    /// </summary>
    public interface IColyseusRoom
    {
        event ColyseusCloseEventHandler OnLeave;

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

    public class ColyseusRoom<T> : IColyseusRoom
    {
        /// <summary>
        ///     Delegate for handling messages
        /// </summary>
        /// <remarks>Currently unused</remarks>
        /// <param name="message">Message data received</param>
        public delegate void RoomOnMessageEventHandler(object message);

        /// <summary>
        ///     Delegate for room state changes
        /// </summary>
        /// <param name="state">The state change received</param>
        /// <param name="isFirstState">Flag if first state received</param>
        public delegate void RoomOnStateChangeEventHandler(T state, bool isFirstState);

        private readonly ColyseusDecoder Decode = ColyseusDecoder.GetInstance();

        private readonly ColyseusEncoder Encode = ColyseusEncoder.GetInstance();

        /// <summary>
        ///     Reference to the room's WebSocket Connection
        /// </summary>
        public ColyseusConnection colyseusConnection;

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
        protected Dictionary<string, IColyseusMessageHandler> OnMessageHandlers =
            new Dictionary<string, IColyseusMessageHandler>();

        /// <summary>
        ///     Reference to the Serializer this room uses, determined and then generated based on the <see cref="SerializerId" />
        /// </summary>
        protected IColyseusSerializer<T> serializer;

        /// <summary>
        ///     ID to determine which kind of serializer this room uses (<see cref="ColyseusSchemaSerializer{T}" /> or
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
        ///     Initializes a new instance of the <see cref="ColyseusRoom{T}" /> class.
        ///     It synchronizes state automatically with the server and send and receive messaes.
        /// </summary>
        /// <param name="name">The Room identifier</param>
        public ColyseusRoom(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Getter for the <see cref="ColyseusRoom{T}" />'s current state
        /// </summary>
        public T State
        {
            get { return serializer.GetState(); }
        }

        [Obsolete(".Id is deprecated. Please use .RoomId instead.")]
        public string Id
        {
            get { return RoomId; }
        }

        /// <summary>
        ///     Occurs when <see cref="ColyseusClient" /> leaves this room.
        /// </summary>
        public event ColyseusCloseEventHandler OnLeave;

        /// <summary>
        ///     Implementation of <see cref="IColyseusRoom.Connect" />
        /// </summary>
        /// <returns>Response from <see cref="colyseusConnection"></see>.Connect()</returns>
        public async Task Connect()
        {
            await colyseusConnection.Connect();
        }

        /// <summary>
        ///     Leave the room
        /// </summary>
        /// <param name="consented">If the user agreed to this disconnection</param>
        /// <returns>Connection closure depending on user consent</returns>
        public async Task Leave(bool consented = true)
        {
            if (!colyseusConnection.IsOpen) {
                return;
            }

            if (RoomId != null)
            {
                if (consented)
                {
                    await colyseusConnection.Send(new[] {ColyseusProtocol.LEAVE_ROOM});
                }
                else
                {
                    await colyseusConnection.Close();
                }
            }
            else
            {
                OnLeave?.Invoke((int)WebSocketCloseCode.Normal);
            }
        }

        /// <summary>
        ///     Occurs when the <see cref="ColyseusClient" /> successfully connects to the <see cref="ColyseusRoom{T}" />.
        /// </summary>
        public event ColyseusOpenEventHandler OnJoin;

        /// <summary>
        ///     Occurs when some error has been triggered in the room.
        /// </summary>
        public event ColyseusErrorEventHandler OnError;

        /// <summary>
        ///     Occurs after applying the patched state on this <see cref="ColyseusRoom{T}" />.
        /// </summary>
        public event RoomOnStateChangeEventHandler OnStateChange;

        /// <summary>
        ///     Called by the <see cref="ColyseusClient" /> upon connection to a room
        /// </summary>
        /// <param name="colyseusConnection">The connection created by the client</param>
        public void SetConnection(ColyseusConnection colyseusConnection,  ColyseusRoom<T> room = null, Action devModeCloseCallback = null)
        {
	        room ??= this;
	        room.colyseusConnection = colyseusConnection;

	        room.colyseusConnection.OnClose += code =>
	        {
		        if (devModeCloseCallback == null || code == 1006)
		        {
			        room.OnLeave?.Invoke(code);
		        }
		        else
		        {
			        devModeCloseCallback();
		        }
	        };

	        // TODO: expose WebSocket error code!
	        // Connection.OnError += (code, message) => OnError?.Invoke(code, message);

	        room.colyseusConnection.OnError += message => room.OnError?.Invoke(0, message);
	        room.colyseusConnection.OnMessage += bytes => room.ParseMessage(bytes);
        }

        /// <summary>
        ///     Response to state changes received as messages
        /// </summary>
        /// ///
        /// <remarks>Invokes everything subscribed to <see cref="OnStateChange" /></remarks>
        /// <param name="encodedState">Byte array of the new state data</param>
        /// <param name="offset">Offset to provide the room's <see cref="serializer" /></param>
        public void SetState(byte[] encodedState, int offset)
        {
            serializer.SetState(encodedState, offset);
            OnStateChange?.Invoke(serializer.GetState(), true);
        }

        /// <summary>
        ///     Send a message by number type, without payload
        /// </summary>
        /// <param name="type">Message type</param>
        public async Task Send(byte type)
        {
            await colyseusConnection.Send(new[] {ColyseusProtocol.ROOM_DATA, type});
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

            byte[] initialBytes = {ColyseusProtocol.ROOM_DATA, type};
            byte[] encodedMessage = serializationOutput.ToArray();

            byte[] bytes = new byte[initialBytes.Length + encodedMessage.Length];
            Buffer.BlockCopy(initialBytes, 0, bytes, 0, initialBytes.Length);
            Buffer.BlockCopy(encodedMessage, 0, bytes, initialBytes.Length, encodedMessage.Length);

            await colyseusConnection.Send(bytes);
        }

        /// <summary>
        ///     Send a message by string type, without payload
        /// </summary>
        /// <param name="type">Message type</param>
        public async Task Send(string type)
        {
            byte[] encodedType = Encoding.UTF8.GetBytes(type);
            byte[] initialBytes = Encode.getInitialBytesFromEncodedType(encodedType, ColyseusProtocol.ROOM_DATA);

            byte[] bytes = new byte[initialBytes.Length + encodedType.Length];
            Buffer.BlockCopy(initialBytes, 0, bytes, 0, initialBytes.Length);
            Buffer.BlockCopy(encodedType, 0, bytes, initialBytes.Length, encodedType.Length);

            await colyseusConnection.Send(bytes);
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
            byte[] initialBytes = Encode.getInitialBytesFromEncodedType(encodedType, ColyseusProtocol.ROOM_DATA);
            byte[] encodedMessage = serializationOutput.ToArray();

            byte[] bytes = new byte[encodedType.Length + encodedMessage.Length + initialBytes.Length];
            Buffer.BlockCopy(initialBytes, 0, bytes, 0, initialBytes.Length);
            Buffer.BlockCopy(encodedType, 0, bytes, initialBytes.Length, encodedType.Length);
            Buffer.BlockCopy(encodedMessage, 0, bytes, initialBytes.Length + encodedType.Length, encodedMessage.Length);

            await colyseusConnection.Send(bytes);
        }

        /// <summary>
        ///     Send a message by number type with raw bytes payload
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="bytes">Message payload</param>
        public async Task SendBytes(byte type, byte[] bytes)
        {
            byte[] initialBytes = { ColyseusProtocol.ROOM_DATA_BYTES, type };

            byte[] bytesToSend = new byte[initialBytes.Length + bytes.Length];
            Buffer.BlockCopy(initialBytes, 0, bytesToSend, 0, initialBytes.Length);
            Buffer.BlockCopy(bytes, 0, bytesToSend, initialBytes.Length, bytes.Length);

            await colyseusConnection.Send(bytesToSend);
        }

        /// <summary>
        ///     Send a message by string type with raw bytes payload
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="bytes">Message payload</param>
        public async Task SendBytes(string type, byte[] bytes)
        {
            byte[] encodedType = Encoding.UTF8.GetBytes(type);
            byte[] initialBytes = Encode.getInitialBytesFromEncodedType(encodedType, ColyseusProtocol.ROOM_DATA_BYTES);

            byte[] bytesToSend = new byte[encodedType.Length + bytes.Length + initialBytes.Length];
            Buffer.BlockCopy(initialBytes, 0, bytesToSend, 0, initialBytes.Length);
            Buffer.BlockCopy(encodedType, 0, bytesToSend, initialBytes.Length, encodedType.Length);
            Buffer.BlockCopy(bytes, 0, bytesToSend, initialBytes.Length + encodedType.Length, bytes.Length);

            await colyseusConnection.Send(bytesToSend);
        }

        /// <summary>
        ///     Method to add new message handlers to the room
        /// </summary>
        /// <param name="type">The type of message received</param>
        /// <param name="handler"></param>
        /// <typeparam name="MessageType">The type of object this message should respond with</typeparam>
        public void OnMessage<MessageType>(string type, Action<MessageType> handler)
        {
            OnMessageHandlers.Add(type, new ColyseusMessageHandler<MessageType>
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
            OnMessageHandlers.Add("i" + type, new ColyseusMessageHandler<MessageType>
            {
                Action = handler
            });
        }

        /// <summary>
        ///     Method to add new message handlers to the room
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="MessageType">The type of object this message should respond with</typeparam>
        public void OnMessage<MessageType>(Action<MessageType> handler) where MessageType : Schema.Schema, new()
        {
            OnMessageHandlers.Add("s" + typeof(MessageType), new ColyseusMessageHandler<MessageType>
            {
                Action = handler
            });
        }

        /// <summary>
        ///     The function that will be called when the <see cref="colyseusConnection" /> receives a message
        /// </summary>
        /// <param name="bytes">The message as provided from the <see cref="colyseusConnection" /></param>
        protected async void ParseMessage(byte[] bytes)
        {
            byte code = bytes[0];

            if (code == ColyseusProtocol.JOIN_ROOM)
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
                        serializer = new ColyseusSchemaSerializer<T>();
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
                    try
                    {
                        serializer = (IColyseusSerializer<T>) new ColyseusNoneSerializer();
                    }
                    catch (Exception e)
                    {
                        DisplaySerializerErrorHelp(e,
                            "Consider setting state in the server-side using \"this.setState(new " + typeof(T).Name +
                            "())\"");
                    }
                }

                if (bytes.Length > offset)
                {
	                try {
		                serializer.Handshake(bytes, offset);
	                }
	                catch (Exception e)
	                {
		                await Leave(false);
		                OnError?.Invoke(ColyseusErrorCode.SCHEMA_MISMATCH, e.Message);
		                return;
	                }
                }

                ReconnectionToken = new ReconnectionToken()
                {
                    RoomId = RoomId,
                    Token = reconnectionToken
                };

                OnJoin?.Invoke();

                // Acknowledge JOIN_ROOM
                await colyseusConnection.Send(new[] {ColyseusProtocol.JOIN_ROOM});
            }
            else if (code == ColyseusProtocol.ERROR)
            {
                Iterator it = new Iterator {Offset = 1};
                float errorCode = Decode.DecodeNumber(bytes, it);
                string errorMessage = Decode.DecodeString(bytes, it);
                OnError?.Invoke((int) errorCode, errorMessage);
            }
            else if (code == ColyseusProtocol.ROOM_DATA_SCHEMA)
            {
                Iterator it = new Iterator {Offset = 1};
                float typeId = Decode.DecodeNumber(bytes, it);

                Type messageType = ColyseusContext.GetInstance().Get(typeId);
                Schema.Schema message = (Schema.Schema) Activator.CreateInstance(messageType);

                message.Decode(bytes, it);

                IColyseusMessageHandler handler = null;
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
            else if (code == ColyseusProtocol.LEAVE_ROOM)
            {
                await Leave();
            }
            else if (code == ColyseusProtocol.ROOM_STATE)
            {
	            SetState(bytes, 1);
            }
            else if (code == ColyseusProtocol.ROOM_STATE_PATCH)
            {
                Patch(bytes, 1);
            }
            else if (code == ColyseusProtocol.ROOM_DATA || code == ColyseusProtocol.ROOM_DATA_BYTES)
            {
                IColyseusMessageHandler handler = null;
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

                    if ( code == ColyseusProtocol.ROOM_DATA )
                    {
                        //
                        // MsgPack deserialization can be optimized:
                        // https://github.com/deniszykov/msgpack-unity3d/issues/23
                        //
                        message = bytes.Length > it.Offset
                            ? MsgPack.Deserialize(handler.Type,
                                new MemoryStream(bytes, it.Offset, bytes.Length - it.Offset, false))
                            : null;
                    }
                    else if ( code == ColyseusProtocol.ROOM_DATA_BYTES )
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
        /// <param name="offset">Offset to provide the room's <see cref="serializer" /></param>
        protected void Patch(byte[] delta, int offset)
        {
            serializer.Patch(delta, offset);
            OnStateChange?.Invoke(serializer.GetState(), false);
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
    }
}
