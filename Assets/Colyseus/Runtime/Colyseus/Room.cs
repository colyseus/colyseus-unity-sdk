using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Colyseus.Schema;
using GameDevWare.Serialization;

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
        ///     Whether automatic reconnection is enabled.
        ///     Set to false to disable automatic reconnection entirely.
        /// </summary>
        public bool Enabled = true;

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
        ///     Server-time + RTT estimator, driven by the TIMED prefix servers
        ///     emit when the room called <c>defineInput()</c>. Always present;
        ///     without TIMED samples it reports <c>ServerNow() == Now()</c> and
        ///     zeros.
        /// </summary>
        public RoomClock Clock = new RoomClock();

        // input layer — populated from the JOIN_ROOM handshake sections
        private InputHandle inputHandle;
        private DynamicTypeDefinition inputDefinitionFromReflection;
        private bool inputStampRender;
        private bool inputStampReckon;
        private int? inputTickRate;
        private int? inputPatchRate;
        private int? inputSubSteps;

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
        ///     Timestamp of the last ping request (in milliseconds).
        /// </summary>
        private long _lastPingTime = 0;

        /// <summary>
        ///     Default <see cref="Request{TResponse}(string, object, int)" /> timeout, in milliseconds.
        /// </summary>
        public static int DefaultRequestTimeout = 10000;

        // request/response (ROOM_REQUEST / ROOM_RESPONSE) correlation state
        private uint _nextRequestId;
        private readonly Dictionary<uint, PendingRequest> _pendingRequests = new Dictionary<uint, PendingRequest>();

        private class PendingRequest
        {
            /// <summary>Called once with the decoded reply outcome: (ok, payload, faulted).</summary>
            public Action<bool, object, bool> OnReply;

            /// <summary>Called when the connection closes before a reply arrives.</summary>
            public Action<string> OnClose;

            /// <summary>OK payloads deserialize into this type.</summary>
            public System.Type ResponseType;

            /// <summary>Unix ms after which the request expires; 0 = never.</summary>
            public long ExpiresAt;

            /// <summary>Called once when <see cref="ExpiresAt" /> passes unanswered.</summary>
            public Action OnTimeout;
        }

        /// <summary>
        ///     Callback to invoke when ping response is received.
        /// </summary>
        private Action<int> _pingCallback = null;

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
                var tcs = new TaskCompletionSource<int>();
                void onLeave(int code) { tcs.TrySetResult(code); }
                OnLeave += onLeave;

                try
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
                catch (OperationCanceledException)
                {
                    // Connection already dropped — leave will complete via OnLeave
                }

                try
                {
                    // Wait for the connection to fully close (with timeout to avoid hanging)
                    await Task.WhenAny(tcs.Task, Task.Delay(5000));
                }
                finally
                {
                    OnLeave -= onLeave;
                }
            }
            else
            {
                OnLeave?.Invoke((int)CloseCode.CONSENTED);
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
				// in-flight requests can't be answered on a closed socket
				RejectAllPendingRequests("connection closed before a response was received.");

				if (JoinedAtTime == 0) {
					OnError?.Invoke(code, "Connection closed before joining room");
					return;
				}

				if (
					code == (int) CloseCode.NO_STATUS_RECEIVED ||
					code == (int) CloseCode.ABNORMAL_CLOSURE ||
					code == (int) CloseCode.GOING_AWAY ||
					code == (int) CloseCode.MAY_TRY_RECONNECT
				) {
					OnDrop?.Invoke(code);
					HandleReconnection(code);

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
            hasFirstState = true;
            OnStateChange?.Invoke(Serializer.GetState(), true);
            // after the event handlers, so an awaiter observes the same order
            // a late OnStateChange subscriber would
            firstStateSource?.TrySetResult(Serializer.GetState());
        }

        private bool hasFirstState;
        private TaskCompletionSource<T> firstStateSource;

        /// <summary>
        ///     Awaitable first full state sync — the C# analogue of the JS
        ///     SDK's <c>room.onStateChange.once(...)</c>.
        ///
        ///     A resolved join does NOT mean the state has decoded: schema
        ///     collections (<c>State.players</c>, refs, …) arrive with the
        ///     first full sync a moment later, and mounting on them earlier
        ///     reads nulls. Await this after joining, before touching state.
        ///
        ///     Resolves immediately when the state has already synced.
        ///     Deliberately timer-free — it completes inline on the message
        ///     pump, so it works on WebGL (no thread-pool timers there; a
        ///     <c>Task.Delay</c> poll never resumes) and adds no per-frame
        ///     polling anywhere else.
        /// </summary>
        /// <returns>The decoded root state instance.</returns>
        public Task<T> WaitForFirstState()
        {
            if (hasFirstState) return Task.FromResult(Serializer.GetState());
            if (firstStateSource == null) firstStateSource = new TaskCompletionSource<T>();
            return firstStateSource.Task;
        }

        /// <summary>
        ///     Send a message by number type, without payload
        /// </summary>
        /// <param name="type">Message type</param>
        public Task Send(byte type)
        {
            return SendOrEnqueue(BuildFrame(Protocol.ROOM_DATA, null, type, null));
        }

        /// <summary>
        ///     Send a message by number type with payload
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="message">Message payload</param>
        public Task Send(byte type, object message)
        {
            return SendOrEnqueue(BuildFrame(Protocol.ROOM_DATA, null, type, Pack(message)));
        }

        /// <summary>
        ///     Send a message by string type, without payload
        /// </summary>
        /// <param name="type">Message type</param>
        public Task Send(string type)
        {
            return SendOrEnqueue(BuildFrame(Protocol.ROOM_DATA, null, type, null));
        }

        /// <summary>
        ///     Send a message by string type with payload
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="message">Message payload</param>
        public Task Send(string type, object message)
        {
            return SendOrEnqueue(BuildFrame(Protocol.ROOM_DATA, null, type, Pack(message)));
        }

        /// <summary>
        ///     Send a message and hand the server's reply to <paramref name="callback" />
        ///     as <c>(response, null)</c>, or <c>(default, exception)</c> when the
        ///     request fails. Callback form of <see cref="Request{TResponse}(string, object, int)" />;
        ///     uses <see cref="DefaultRequestTimeout" />.
        /// </summary>
        public void Send<TResponse>(string type, object payload, Action<TResponse, Exception> callback)
        {
            SendWithCallback(Request<TResponse>(type, payload), callback);
        }

        /// <inheritdoc cref="Send{TResponse}(string, object, Action{TResponse, Exception})" />
        public void Send<TResponse>(byte type, object payload, Action<TResponse, Exception> callback)
        {
            SendWithCallback(Request<TResponse>(type, payload), callback);
        }

        // awaited (not ContinueWith) so the callback lands on the captured
        // SynchronizationContext — Unity's main thread
        private static async void SendWithCallback<TResponse>(Task<TResponse> request, Action<TResponse, Exception> callback)
        {
            TResponse response;
            try
            {
                response = await request;
            }
            catch (Exception e)
            {
                callback(default, e);
                return;
            }
            callback(response, null);
        }

        /// <summary>
        ///     Send a message by number type with raw bytes payload
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="bytes">Message payload</param>
        public Task SendBytes(byte type, byte[] bytes)
        {
            return SendOrEnqueue(BuildFrame(Protocol.ROOM_DATA_BYTES, null, type, bytes));
        }

        /// <summary>
        ///     Send a message by string type with raw bytes payload
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="bytes">Message payload</param>
        public Task SendBytes(string type, byte[] bytes)
        {
            return SendOrEnqueue(BuildFrame(Protocol.ROOM_DATA_BYTES, null, type, bytes));
        }

        private static byte[] Pack(object message)
        {
            MemoryStream output = new MemoryStream();
            MsgPack.Serialize(message, output, SerializationOptions.SuppressTypeInformation);
            return output.ToArray();
        }

        /// <summary>
        ///     <c>[protocol][prefix?][type][payload?]</c> — the type rides as a
        ///     schema "string" or non-negative "number", the encoding the server's
        ///     <c>decode.stringCheck</c> / <c>decode.number</c> expect.
        /// </summary>
        private static byte[] BuildFrame(byte protocol, byte[] prefix, object type, byte[] payload)
        {
            List<byte> frame = new List<byte> { protocol };
            if (prefix != null) { frame.AddRange(prefix); }

            if (type is string stringType)
            {
                Encode.String(frame, stringType);
            }
            else
            {
                frame.AddRange(Encode.EncodeUintNumber((byte)type));
            }

            if (payload != null) { frame.AddRange(payload); }
            return frame.ToArray();
        }

        // reliable + offline: buffer so it flushes on (re)connect
        private Task SendOrEnqueue(byte[] frame)
        {
            if (!Connection.IsOpen)
            {
                EnqueueMessage(frame);
                return Task.CompletedTask;
            }
            return Connection.Send(frame);
        }

        /// <summary>
        ///     Send a message and await the server's reply — the value the server
        ///     returns from its matching <c>onMessage(type, ...)</c> handler.
        /// </summary>
        /// <remarks>
        ///     Throws <see cref="RequestError" /> when the handler rejects (the
        ///     authored reason) or throws (<c>{name, message, code?}</c>), and
        ///     plain exceptions when the connection closes first or no reply
        ///     arrives within <paramref name="timeoutMs" />
        ///     (default: <see cref="DefaultRequestTimeout" />).
        /// </remarks>
        public Task<TResponse> Request<TResponse>(string type, object payload = null, int timeoutMs = 0)
        {
            return RequestInternal<TResponse>(type, payload, timeoutMs);
        }

        /// <inheritdoc cref="Request{TResponse}(string, object, int)" />
        public Task<TResponse> Request<TResponse>(byte type, object payload = null, int timeoutMs = 0)
        {
            return RequestInternal<TResponse>(type, payload, timeoutMs);
        }

        private async Task<TResponse> RequestInternal<TResponse>(object type, object payload, int timeoutMs)
        {
            if (Connection == null || !Connection.IsOpen)
            {
                throw new Exception($"cannot send request \"{type}\": connection is not open.");
            }

            var tcs = new TaskCompletionSource<TResponse>();

            uint requestId = await SendRequest(type, payload, typeof(TResponse),
                (ok, replyPayload, faulted) =>
                {
                    if (ok)
                    {
                        tcs.TrySetResult(replyPayload == null ? default : (TResponse)replyPayload);
                    }
                    else
                    {
                        tcs.TrySetException(new RequestError(replyPayload, faulted));
                    }
                },
                reason => tcs.TrySetException(new Exception(reason)));

            int ms = timeoutMs > 0 ? timeoutMs : DefaultRequestTimeout;
            ArmTimeout(requestId, ms,
                () => tcs.TrySetException(new TimeoutException($"request \"{type}\" timed out after {ms}ms.")));

            return await tcs.Task;
        }

        /// <summary>
        ///     Low-level round-trip primitive: registers <paramref name="onReply" />
        ///     (called once with the decoded outcome when the server replies) and
        ///     transmits a ROOM_REQUEST frame. <see cref="Request{TResponse}(string, object, int)" />
        ///     wraps it with a timeout. Returns the request id.
        /// </summary>
        protected async Task<uint> SendRequest(object type, object payload, System.Type responseType, Action<bool, object, bool> onReply, Action<string> onClose)
        {
            uint requestId = _nextRequestId++;

            // [ROOM_REQUEST][requestId][type][msgpack payload?]
            byte[] frame = BuildFrame(Protocol.ROOM_REQUEST, Encode.EncodeUintNumber(requestId), type,
                payload != null ? Pack(payload) : null);

            lock (_pendingRequests)
            {
                _pendingRequests[requestId] = new PendingRequest
                {
                    OnReply = onReply,
                    OnClose = onClose,
                    ResponseType = responseType,
                };
            }

            await SendOrEnqueue(frame);

            return requestId;
        }

        /// <summary>
        ///     Drop a pending round-trip (request timeout, or a caller's own
        ///     TTL/cancel path). A late reply is then ignored. Idempotent.
        /// </summary>
        protected void CancelRequest(uint requestId)
        {
            lock (_pendingRequests)
            {
                _pendingRequests.Remove(requestId);
            }
        }

        // Two expiry paths converge on ExpireRequest: a Task.Delay timer, and a
        // sweep on every inbound frame. WebGL has no thread-pool timers (a
        // Task.Delay never resumes there), so the sweep is what fails an
        // unanswered request on that platform; elsewhere the timer fires first
        // and the sweep finds nothing.
        private void ArmTimeout(uint requestId, int ms, Action onTimeout)
        {
            lock (_pendingRequests)
            {
                // already answered — nothing to arm
                if (!_pendingRequests.TryGetValue(requestId, out PendingRequest entry)) { return; }
                entry.ExpiresAt = NowMs() + ms;
                entry.OnTimeout = onTimeout;
            }

            _ = Task.Delay(ms).ContinueWith(_ => ExpireRequest(requestId), TaskContinuationOptions.ExecuteSynchronously);
        }

        private void ExpireRequest(uint requestId)
        {
            PendingRequest entry;
            lock (_pendingRequests)
            {
                if (!_pendingRequests.TryGetValue(requestId, out entry)) { return; }
                _pendingRequests.Remove(requestId);
            }
            entry.OnTimeout?.Invoke();
        }

        /// <summary>Wall clock (unix ms) for request expiry; overridable for tests.</summary>
        protected virtual long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private void SweepExpiredRequests()
        {
            List<PendingRequest> expired = null;
            lock (_pendingRequests)
            {
                if (_pendingRequests.Count == 0) { return; }

                long now = NowMs();
                List<uint> ids = null;
                foreach (KeyValuePair<uint, PendingRequest> pair in _pendingRequests)
                {
                    if (pair.Value.ExpiresAt > 0 && pair.Value.ExpiresAt <= now)
                    {
                        (ids ??= new List<uint>()).Add(pair.Key);
                        (expired ??= new List<PendingRequest>()).Add(pair.Value);
                    }
                }
                if (ids == null) { return; }
                foreach (uint id in ids) { _pendingRequests.Remove(id); }
            }

            foreach (PendingRequest entry in expired) { entry.OnTimeout?.Invoke(); }
        }

        private void RejectAllPendingRequests(string reason)
        {
            List<PendingRequest> entries;
            lock (_pendingRequests)
            {
                if (_pendingRequests.Count == 0) { return; }
                entries = new List<PendingRequest>(_pendingRequests.Values);
                _pendingRequests.Clear();
            }

            foreach (PendingRequest entry in entries)
            {
                entry.OnClose?.Invoke(reason);
            }
        }

        /// <summary>
        ///     Send a ping to the server and measure round-trip latency.
        /// </summary>
        /// <param name="callback">Callback invoked with the round-trip time in milliseconds</param>
        public async void Ping(Action<int> callback)
        {
            // Skip if connection is not open
            if (Connection == null || !Connection.IsOpen)
            {
                return;
            }

            _lastPingTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _pingCallback = callback;
            await Connection.Send(new[] { Protocol.PING });
        }

        /// <summary>
        ///     Lazily create (and thereafter return) the per-room input handle.
        ///     Mutate <c>handle.Data</c>, then call <c>handle.Send()</c>.
        ///
        ///     <para>
        ///     Pass an input schema instance via <paramref name="data" /> (a
        ///     class generated by schema-codegen for the server's
        ///     <c>defineInput()</c> schema). When omitted, the input schema is
        ///     synthesized from the handshake's input reflection as a
        ///     <see cref="DynamicSchema" /> (mutate via its indexer). Later
        ///     calls return the same handle; their options are ignored (first
        ///     call wins).
        ///     </para>
        /// </summary>
        public InputHandle Input(Schema.Schema data = null, InputOptions options = null)
        {
            if (inputHandle != null)
            {
                return inputHandle;
            }
            options ??= new InputOptions();

            if (options.Mode == "unreliable")
            {
                throw new NotSupportedException(
                    "Room.Input(): mode \"unreliable\" is not supported yet — it needs a " +
                    "WebTransport datagram channel, and this SDK connects over WebSocket only. " +
                    "Use mode \"reliable\".");
            }

            if (data == null)
            {
                if (inputDefinitionFromReflection == null)
                {
                    throw new Exception(
                        "Room.Input(): no input schema available. The server room must call " +
                        "defineInput(YourInput), or pass an input schema instance explicitly.");
                }
                data = new DynamicSchema { Definition = inputDefinitionFromReflection };
            }

            var encoder = new InputEncoder(data, options.Mode, options.HistorySize);
            inputHandle = new InputHandle(
                data, encoder,
                inputStampRender, inputStampReckon,
                options.RenderDelay, options.AllowRewind,
                inputTickRate, inputPatchRate, inputSubSteps,
                () => Connection, () => Clock);
            return inputHandle;
        }

        /// <summary>
        ///     Method to add new message handlers to the room
        ///     <para>
        ///         Several handlers may be registered for the same message type, and are invoked in
        ///         registration order. They must all share the same <typeparamref name="MessageType" />.
        ///     </para>
        /// </summary>
        /// <param name="type">The type of message received</param>
        /// <param name="handler"></param>
        /// <typeparam name="MessageType">The type of object this message should respond with</typeparam>
        /// <returns>An <see cref="Action" /> that removes this handler. Safe to call more than once.</returns>
        /// <exception cref="Exception">
        ///     Thrown when <paramref name="type" /> already has handlers of a different message type.
        /// </exception>
        public Action OnMessage<MessageType>(string type, Action<MessageType> handler)
        {
            // an emptied-out entry no longer constrains the message type, so it is replaced rather than reused
            if (!OnMessageHandlers.TryGetValue(type, out var entry) || entry.IsEmpty)
            {
                entry = new MessageHandler<MessageType>();
                OnMessageHandlers[type] = entry;
            }

            var messageHandler = entry as MessageHandler<MessageType>;
            if (messageHandler == null)
            {
                throw new Exception(
                    $"room.OnMessage(\"{type}\") is already registered as {entry.Type}. All handlers for the same message must share the same type.");
            }

            messageHandler.Action += handler;

            return () => messageHandler.Action -= handler;
        }

        /// <summary>
        ///     Method to add new message handlers to the room
        /// </summary>
        /// <param name="type">The type of message received</param>
        /// <param name="handler"></param>
        /// <typeparam name="MessageType">The type of object this message should respond with</typeparam>
        /// <returns>An <see cref="Action" /> that removes this handler. Safe to call more than once.</returns>
        public Action OnMessage<MessageType>(byte type, Action<MessageType> handler)
        {
            return OnMessage("i" + type, handler);
        }

        /// <summary>
        ///     The function that will be called when the <see cref="Connection" /> receives a message
        /// </summary>
        /// <param name="bytes">The message as provided from the <see cref="Connection" /></param>
        protected async void ParseMessage(byte[] bytes)
        {
            SweepExpiredRequests();

            Iterator it = new Iterator { Offset = 1 };

            // Strip modifier bits (bits 5..7) so the dispatch below stays
            // modifier-agnostic; consume any modifier-attached prefix bytes here.
            byte rawByte = bytes[0];
            byte code = (byte)(rawByte & Protocol.CODE_MASK);

            if ((rawByte & (byte)ProtocolModifier.TIMED) != 0)
            {
                // [uint32 sNow][uint32 inputSeq] — server time (ms since room
                // start) + last PROCESSED input seq. The input ack goes to the
                // handle (it owns the round-trip); its RTT sample + sNow feed
                // the time-only clock.
                double sNow = Convert.ToDouble(Decode.DecodeUint32(bytes, it));
                int inputSeq = Convert.ToInt32(Decode.DecodeUint32(bytes, it));
                double rttSample = inputHandle?.AckInput(inputSeq) ?? -1;
                Clock.Sample(sNow, rttSample);
            }

            if (code == Protocol.JOIN_ROOM)
            {
                var reconnectionToken = Encoding.UTF8.GetString(bytes, it.Offset + 1, bytes[it.Offset]);
                it.Offset += reconnectionToken.Length + 1;

                SerializerId = Encoding.UTF8.GetString(bytes, it.Offset + 1, bytes[it.Offset]);
                it.Offset += SerializerId.Length + 1;

                if (SerializerId == "schema")
                {
                    try
                    {
                        // keep the existing serializer across in-place reconnects —
                        // recreating it would destroy state identity, and the
                        // rejoin snapshot could no longer reconcile (DecodeResync)
                        if (Serializer == null || !(Serializer is SchemaSerializer<T>))
                        {
                            Serializer = new SchemaSerializer<T>();
                        }
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
                    ColyseusContext.Logger.LogError(
                        "FossilDelta Serialization has been deprecated. It is highly recommended that you update your code to use the Schema Serializer. Otherwise, you must use an earlier version of the Colyseus plugin");
                }
                else
                {
                    Serializer = (ISerializer<T>) new NoneSerializer();
                }

                // State reflection is length-prefixed: the schema handshake must
                // not read past it into the trailing tagged-section bytes. A zero
                // length means reconnect (the serializer already has state).
                int stateReflectionLen = (int)Decode.DecodeNumber(bytes, it);
                if (stateReflectionLen > 0)
                {
                    int reflectionEnd = it.Offset + stateReflectionLen;
	                try {
		                // bounded slice — the reflection decoder reads until end-of-array
		                byte[] reflectionBytes = new byte[reflectionEnd];
		                Buffer.BlockCopy(bytes, 0, reflectionBytes, 0, reflectionEnd);
		                Serializer.Handshake(reflectionBytes, it.Offset);
	                }
	                catch (Exception e)
	                {
		                await Leave(false);
		                OnError?.Invoke(ErrorCode.SCHEMA_MISMATCH, e.Message);
		                return;
	                }
                    it.Offset = reflectionEnd;
                }

                // Trailing tagged sections: [tag byte][length varint][payload].
                // Unknown tags are skipped via length (forward-compatible).
                while (it.Offset < bytes.Length)
                {
                    byte tag = bytes[it.Offset++];
                    int sectionLen = (int)Decode.DecodeNumber(bytes, it);
                    int sectionEnd = it.Offset + sectionLen;

                    if (tag == (byte)HandshakeSection.INPUT_REFLECTION)
                    {
                        // schema reflection of the input struct — build a dynamic
                        // definition so Input() works without an explicit type
                        byte[] sectionBytes = new byte[sectionEnd];
                        Buffer.BlockCopy(bytes, 0, sectionBytes, 0, sectionEnd);
                        inputDefinitionFromReflection =
                            SchemaSerializer<T>.BuildDynamicDefinition(sectionBytes, it.Offset);
                    }
                    else if (tag == (byte)HandshakeSection.INPUT_OPTIONS)
                    {
                        // [flags u8][varints in bit order]
                        byte flags = bytes[it.Offset++];
                        inputStampRender = (flags & (byte)InputFlags.RENDER_TIME) != 0;
                        inputStampReckon = (flags & (byte)InputFlags.RECKON_TIME) != 0;
                        if ((flags & (byte)InputFlags.FIXED_TIMESTEP) != 0) { inputTickRate = (int)Decode.DecodeNumber(bytes, it); }
                        if ((flags & (byte)InputFlags.PATCH_RATE) != 0) { inputPatchRate = (int)Decode.DecodeNumber(bytes, it); }
                        if ((flags & (byte)InputFlags.SUB_STEPS) != 0) { inputSubSteps = (int)Decode.DecodeNumber(bytes, it); }
                    }

                    it.Offset = sectionEnd;
                }

                // hand the snapshot cadence to the clock once the loop has it
                if (inputPatchRate.HasValue)
                {
                    Clock.SetPatchInterval(inputPatchRate.Value);
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
                    ColyseusContext.Logger.Log("[Colyseus reconnection]: Reconnection successful!");
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
	            SetState(bytes, it.Offset);
            }
            else if (code == Protocol.ROOM_STATE_PATCH)
            {
                Patch(bytes, it.Offset);
            }
            else if (code == Protocol.PING)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _pingCallback?.Invoke((int)(now - _lastPingTime));
                _pingCallback = null;
            }
            else if (code == Protocol.ROOM_RESPONSE)
            {
                // reply to a pending Request()
                uint requestId = (uint) Decode.DecodeNumberAsDouble(bytes, it);
                var status = (ResponseStatus) bytes[it.Offset++];

                // already answered (e.g. timed out) or unknown id — ignore
                PendingRequest entry;
                lock (_pendingRequests)
                {
                    if (_pendingRequests.TryGetValue(requestId, out entry))
                    {
                        _pendingRequests.Remove(requestId);
                    }
                }

                if (entry != null)
                {
                    object payload = null;
                    if (bytes.Length > it.Offset)
                    {
                        // OK replies deserialize into the request's response type;
                        // REJECTED/ERROR payloads are shapeless (reason / {name, message})
                        System.Type payloadType = status == ResponseStatus.OK ? entry.ResponseType : typeof(object);
                        payload = MsgPack.Deserialize(payloadType,
                            new MemoryStream(bytes, it.Offset, bytes.Length - it.Offset, false));
                    }

                    // the ONE place the wire's three statuses collapse to (ok, payload, faulted):
                    entry.OnReply(status == ResponseStatus.OK, payload, status == ResponseStatus.ERROR);
                }
            }
            else if (code == Protocol.ROOM_DATA || code == Protocol.ROOM_DATA_BYTES)
            {
                IMessageHandler handler = null;
                object type;

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

                if (handler != null && !handler.IsEmpty)
                {
                    object message = null;

                    if ( code == Protocol.ROOM_DATA )
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
                    else if ( code == Protocol.ROOM_DATA_BYTES )
                    {
                        message = new byte[bytes.Length - it.Offset];
                        Buffer.BlockCopy(bytes, it.Offset, (byte[])message, 0, bytes.Length - it.Offset);
                    }

                    handler.Invoke(message);
                }
                else if (type != null && !type.ToString().StartsWith("__"))
                {
                    ColyseusContext.Logger.LogWarning("room.OnMessage() not registered for: '" + type + "'");
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
            ColyseusContext.Logger.LogWarning("The serializer from the server is: '" + SerializerId + "'. " + helpMessage);
            throw e;
        }

		private void HandleReconnection(int code)
		{
			if (!Reconnection.Enabled)
			{
				OnLeave?.Invoke(code);
				return;
			}

			// Check minimum uptime before allowing reconnection
			long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (currentTime - JoinedAtTime < Reconnection.MinUptime)
			{
				ColyseusContext.Logger.Log($"[Colyseus reconnection]: Room not up long enough for auto-reconnect (min uptime: {Reconnection.MinUptime}ms)");
				OnLeave?.Invoke((int)CloseCode.ABNORMAL_CLOSURE);
				return;
			}

			if (!Reconnection.IsReconnecting)
			{
				Reconnection.RetryCount = 0;
				Reconnection.IsReconnecting = true;
			}

			// The server allocates a FRESH input buffer for the reconnected
			// client (its consumed counter restarts at 0) — zero ours so
			// post-reconnect seqs line up. Observing controllers follow via
			// the handle's Epoch.
			inputHandle?.Reset();

			_ = RetryReconnection();
		}

		private async Task RetryReconnection()
		{
			if (Reconnection.RetryCount >= Reconnection.MaxRetries)
			{
				// No more retries
				ColyseusContext.Logger.Log($"[Colyseus reconnection]: Reconnection failed after {Reconnection.MaxRetries} attempts.");
				Reconnection.IsReconnecting = false;
				OnLeave?.Invoke((int)CloseCode.FAILED_TO_RECONNECT);
				return;
			}

			Reconnection.RetryCount++;

			int delay = Math.Min(Reconnection.MaxDelay,
				Math.Max(Reconnection.MinDelay,
					Reconnection.Backoff(Reconnection.RetryCount, Reconnection.Delay)));

			ColyseusContext.Logger.Log($"[Colyseus reconnection]: Will retry in {delay / 1000f:F1} seconds...");
			await Task.Delay(delay);

			ColyseusContext.Logger.Log($"[Colyseus reconnection]: Re-establishing sessionId '{SessionId}' with roomId '{RoomId}'... (attempt {Reconnection.RetryCount} of {Reconnection.MaxRetries})");

			try
			{
				await Connection.Reconnect(ReconnectionToken.Token);
			}
			catch (Exception e)
			{
				ColyseusContext.Logger.Log($"[Colyseus reconnection]: Reconnect failed - {e.Message}");
				_ = RetryReconnection();
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
