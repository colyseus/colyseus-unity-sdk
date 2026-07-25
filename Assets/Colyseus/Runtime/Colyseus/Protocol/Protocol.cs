// ReSharper disable InconsistentNaming

namespace Colyseus
{
    /// <summary>
    ///     Colyseus protocol codes.
    ///
    ///     Codes occupy bits 0..4 of the leading message byte (values 0..31).
    ///     Bits 5..7 carry <see cref="ProtocolModifier" /> decorations, OR'd
    ///     onto the base code at send time. Decoders strip the modifier bits
    ///     before dispatching:
    ///     <code>
    ///         var code = (byte)(bytes[0] &amp; Protocol.CODE_MASK);
    ///         var modifiers = (byte)(bytes[0] &amp; Protocol.MODIFIER_MASK);
    ///     </code>
    /// </summary>
    public class Protocol
    {
        //
        // Room-related (10~18)
        //

        /// <summary>
        ///     When JOIN request is accepted.
        /// </summary>
        public static byte JOIN_ROOM = 10;

        /// <summary>
        ///     When an error has happened in the server-side.
        /// </summary>
        public static byte ERROR = 11;

        /// <summary>
        ///     When server explicitly removes <see cref="Client" /> from the <see cref="Room{T}" />
        /// </summary>
        public static byte LEAVE_ROOM = 12;

        /// <summary>
        ///     When server sends data to a particular <see cref="Room{T}" />
        /// </summary>
        public static byte ROOM_DATA = 13;

        /// <summary>
        ///     When server sends <see cref="Room{T}" /> state to its clients.
        /// </summary>
        public static byte ROOM_STATE = 14;

        /// <summary>
        ///     When server sends <see cref="Room{T}" /> state patches to its clients.
        /// </summary>
        public static byte ROOM_STATE_PATCH = 15;

        /// <summary>
        ///     Deprecated in 0.18 (schema instances via room.send) — never dispatched.
        /// </summary>
        public static byte ROOM_DATA_SCHEMA = 16;

        public static byte ROOM_DATA_BYTES = 17;

        /// <summary>
        ///     Ping message for measuring round-trip latency. Ping and pong
        ///     share this code — the server echoes it.
        /// </summary>
        public static byte PING = 18;

        //
        // Input-related (19~20) — consumed by the input layer (not ported yet)
        //

        /// <summary>[byte, stamp?, input bytes] — client→server single input.</summary>
        public static byte ROOM_INPUT_RELIABLE = 19;

        /// <summary>[byte, len|input, ...] — client→server length-framed ring.</summary>
        public static byte ROOM_INPUT_UNRELIABLE = 20;

        //
        // Request/response (21~22)
        //

        /// <summary>[byte, requestId varint, type(str|num), msgpack payload?] — expects a reply.</summary>
        public static byte ROOM_REQUEST = 21;

        /// <summary>[byte, requestId varint, status uint8, msgpack payload?] — reply to a request.</summary>
        public static byte ROOM_RESPONSE = 22;

        /// <summary>Isolates the base protocol code (low 5 bits, values 0..31).</summary>
        public const byte CODE_MASK = 0x1F;

        /// <summary>Isolates modifier bits (high 3 bits; only TIMED is assigned today).</summary>
        public const byte MODIFIER_MASK = 0xE0;
    }

    /// <summary>
    ///     Modifier bits OR'd into the leading protocol byte. Composable — the
    ///     decoder strips them in a preamble step that precedes the
    ///     protocol-code dispatch.
    /// </summary>
    public enum ProtocolModifier : byte
    {
        /// <summary>
        ///     A <c>[uint32 sNow][uint32 inputSeq]</c> prefix precedes the body —
        ///     server time (ms since room start) + this client's last PROCESSED
        ///     input seq. Set by the server on ROOM_STATE / ROOM_STATE_PATCH
        ///     whenever the room called <c>defineInput()</c>.
        /// </summary>
        TIMED = 0x80,
    }

    /// <summary>Status byte of a ROOM_RESPONSE reply.</summary>
    public enum ResponseStatus : byte
    {
        OK = 0,

        /// <summary>Deliberate, typed rejection — the authored reason rides as the payload.</summary>
        REJECTED = 1,

        /// <summary>Handler fault (threw / no handler) — payload is <c>{name, message, code?}</c>.</summary>
        ERROR = 2,
    }

    /// <summary>
    ///     Section tags for trailing tagged blobs in the JOIN_ROOM handshake:
    ///     <c>[tag uint8][length varint][payload]</c>, repeated until
    ///     end-of-buffer. Unknown tags are skipped via <c>length</c>
    ///     (forward-compatible).
    /// </summary>
    public enum HandshakeSection : byte
    {
        /// <summary>Reflection bytes for the room's input schema (<c>defineInput()</c>).</summary>
        INPUT_REFLECTION = 1,

        /// <summary>Input feature flags + rates the client mirrors (<c>defineInput()</c>).</summary>
        INPUT_OPTIONS = 2,
    }

    /// <summary>
    ///     Error thrown when a <c>Room.Request()</c> is answered with
    ///     <see cref="ResponseStatus.REJECTED" /> or <see cref="ResponseStatus.ERROR" />.
    /// </summary>
    public class RequestError : System.Exception
    {
        /// <summary>The reply payload: the authored rejection reason, or <c>{name, message, code?}</c> for faults.</summary>
        public readonly object Payload;

        /// <summary>True when the server handler threw (or none was registered) — a fault, not a typed rejection.</summary>
        public readonly bool Faulted;

        public RequestError(object payload, bool faulted)
            : base(payload is System.Collections.IDictionary dict && dict.Contains("message")
                ? dict["message"]?.ToString()
                : payload?.ToString() ?? "request failed")
        {
            Payload = payload;
            Faulted = faulted;
        }
    }

	public enum CloseCode
	{
		NORMAL_CLOSURE = 1000,
		GOING_AWAY = 1001,
		NO_STATUS_RECEIVED = 1005,
		ABNORMAL_CLOSURE = 1006,
		CONSENTED = 4000,
		SERVER_SHUTDOWN = 4001,
		WITH_ERROR = 4002,
		FAILED_TO_RECONNECT = 4003,
		MAY_TRY_RECONNECT = 4010,
	}
}
