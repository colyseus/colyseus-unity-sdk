using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

#if UNITY_WEBGL && !UNITY_EDITOR
using AOT;
using System.Runtime.InteropServices;
using Colyseus;
#endif

namespace NativeWebSocket
{
	public delegate void WebSocketOpenEventHandler();
	public delegate void WebSocketMessageEventHandler(byte[] data);
	public delegate void WebSocketErrorEventHandler(string errorMsg);
	public delegate void WebSocketCloseEventHandler(int closeCode);

#if UNITY_WEBGL && !UNITY_EDITOR
  /// <summary>
  /// WebSocket class bound to JSLIB.
  /// </summary>
  public class WebSocket : IWebSocket {

    /* WebSocket JSLIB functions */
    [DllImport ("__Internal")]
    public static extern int WebSocketConnect (int instanceId);

    [DllImport ("__Internal")]
    public static extern int WebSocketClose (int instanceId, int code, string reason);

    [DllImport ("__Internal")]
    public static extern int WebSocketSend (int instanceId, byte[] dataPtr, int dataLength);

    [DllImport ("__Internal")]
    public static extern int WebSocketSendText (int instanceId, string message);

    [DllImport ("__Internal")]
    public static extern int WebSocketGetState (int instanceId);

    protected int instanceId;

    public event WebSocketOpenEventHandler OnOpen;
    public event WebSocketMessageEventHandler OnMessage;
    public event WebSocketErrorEventHandler OnError;
    public event WebSocketCloseEventHandler OnClose;

    public WebSocket (string url, Dictionary<string, string> headers = null) {
      if (!WebSocketFactory.isInitialized) {
        WebSocketFactory.Initialize ();
      }

      int instanceId = WebSocketFactory.WebSocketAllocate (url);
      WebSocketFactory.instances.Add (instanceId, this);

      this.instanceId = instanceId;
    }

    ~WebSocket () {
      WebSocketFactory.HandleInstanceDestroy (this.instanceId);
    }

    public int GetInstanceId () {
      return this.instanceId;
    }

    public UniTask Connect () {
      int ret = WebSocketConnect (this.instanceId);

      if (ret < 0)
        throw WebSocketExtensions.GetErrorMessageFromCode (ret, null);

      return UniTask.CompletedTask;
    }

    public void CancelConnection () {
        if (State == WebSocketState.Open)
            Close (WebSocketCloseCode.Abnormal);
    }

    public UniTask Close (WebSocketCloseCode code = WebSocketCloseCode.Normal, string reason = null) {
      int ret = WebSocketClose (this.instanceId, (int) code, reason);

      if (ret < 0)
        throw WebSocketExtensions.GetErrorMessageFromCode (ret, null);

      return UniTask.CompletedTask;
    }

    public UniTask Send (byte[] data) {
      int ret = WebSocketSend (this.instanceId, data, data.Length);

      if (ret < 0)
        throw WebSocketExtensions.GetErrorMessageFromCode (ret, null);

      return UniTask.CompletedTask;
    }

    public UniTask SendText (string message) {
      int ret = WebSocketSendText (this.instanceId, message);

      if (ret < 0)
        throw WebSocketExtensions.GetErrorMessageFromCode (ret, null);

      return UniTask.CompletedTask;
    }

    public WebSocketState State {
      get {
        int state = WebSocketGetState (this.instanceId);

        if (state < 0)
          throw WebSocketExtensions.GetErrorMessageFromCode (state, null);

        switch (state) {
          case 0:
            return WebSocketState.Connecting;

          case 1:
            return WebSocketState.Open;

          case 2:
            return WebSocketState.Closing;

          case 3:
            return WebSocketState.Closed;

          default:
            return WebSocketState.Closed;
        }
      }
    }

    public void DelegateOnOpenEvent () {
        this.OnOpen?.Invoke ();
    }

    public void DelegateOnMessageEvent (byte[] data) {
        this.OnMessage?.Invoke (data);
    }

    public void DelegateOnErrorEvent (string errorMsg) {
        this.OnError?.Invoke (errorMsg);
    }

    public void DelegateOnCloseEvent (int closeCode) {
        this.OnClose?.Invoke (closeCode);
    }

  }
#else
	public class WebSocket : IWebSocket
	{
		public event WebSocketOpenEventHandler OnOpen;
		public event WebSocketMessageEventHandler OnMessage;
		public event WebSocketErrorEventHandler OnError;
		public event WebSocketCloseEventHandler OnClose;

		private Uri uri;
		private Dictionary<string, string> headers;
		private ClientWebSocket m_Socket = new ClientWebSocket();

		private CancellationTokenSource m_TokenSource;
		private CancellationToken m_CancellationToken;

		private readonly object OutgoingMessageLock = new object();
		private readonly object IncomingMessageLock = new object();

		private readonly SemaphoreSlim _socketLock = new SemaphoreSlim(1, 1); // Semaphore for synchronizing access to the socket
		private bool isSending = false;
		private List<ArraySegment<byte>> sendBytesQueue = new List<ArraySegment<byte>>();
		private List<ArraySegment<byte>> sendTextQueue = new List<ArraySegment<byte>>();

		public WebSocket(string url, Dictionary<string, string> headers = null)
		{
			uri = new Uri(url);

			if (headers == null)
			{
				this.headers = new Dictionary<string, string>();
			}
			else
			{
				this.headers = headers;
			}

			string protocol = uri.Scheme;
			if (!protocol.Equals("ws") && !protocol.Equals("wss"))
				throw new ArgumentException("Unsupported protocol: " + protocol);
		}

		public void CancelConnection()
		{
			m_TokenSource?.Cancel();
		}

		public async UniTask Connect()
		{
			try
			{
				m_TokenSource = new CancellationTokenSource();
				m_CancellationToken = m_TokenSource.Token;

				m_Socket = new ClientWebSocket();

				foreach (var header in headers)
				{
					m_Socket.Options.SetRequestHeader(header.Key, header.Value);
				}

				await m_Socket.ConnectAsync(uri, m_CancellationToken);
				OnOpen?.Invoke();

				await Receive();
			}
			catch (Exception ex)
			{
				OnError?.Invoke(ex.Message);
				OnClose?.Invoke((int)WebSocketCloseCode.Abnormal);
			}
			finally
			{
				if (m_Socket != null)
				{
					m_TokenSource?.Cancel();
					m_Socket?.Dispose();
					m_TokenSource?.Dispose();
				}
			}
		}

		public WebSocketState State
		{
			get
			{
				switch (m_Socket.State)
				{
					case System.Net.WebSockets.WebSocketState.Connecting:
						return WebSocketState.Connecting;

					case System.Net.WebSockets.WebSocketState.Open:
						return WebSocketState.Open;

					case System.Net.WebSockets.WebSocketState.CloseSent:
					case System.Net.WebSockets.WebSocketState.CloseReceived:
						return WebSocketState.Closing;

					case System.Net.WebSockets.WebSocketState.Closed:
						return WebSocketState.Closed;

					default:
						return WebSocketState.Closed;
				}
			}
		}

		public UniTask Send(byte[] bytes)
		{
			// return m_Socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
			return SendMessage(sendBytesQueue, WebSocketMessageType.Binary, new ArraySegment<byte>(bytes));
		}

		public UniTask SendText(string message)
		{
			var encoded = Encoding.UTF8.GetBytes(message);

			// m_Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
			return SendMessage(sendTextQueue, WebSocketMessageType.Text, new ArraySegment<byte>(encoded, 0, encoded.Length));
		}

		private async UniTask SendMessage(List<ArraySegment<byte>> queue, WebSocketMessageType messageType, ArraySegment<byte> buffer)
		{
			// Return control to the calling method immediately.
			//await UniTask.Yield();

			// Make sure we have data.
			if (buffer.Count == 0)
			{
				return;
			}

			// The state of the connection is contained in the context Items dictionary.
			bool sending;

			lock (OutgoingMessageLock)
			{
				sending = isSending;

				// If not, we are now.
				if (!isSending)
				{
					isSending = true;
				}
			}

			if (!sending)
			{
				// Lock with a timeout, just in case.
				if (!await _socketLock.WaitAsync(1000))
				{
					// If we couldn't obtain exclusive access to the socket in one second, something is wrong.
					await m_Socket.CloseAsync(WebSocketCloseStatus.InternalServerError, string.Empty, m_CancellationToken);
					return;
				}

				try
				{
					// Send the message synchronously.
					var t = m_Socket.SendAsync(buffer, messageType, true, m_CancellationToken);
					t.Wait(m_CancellationToken);
				}
				finally
				{
					_socketLock.Release();

					// Note that we've finished sending.
					lock (OutgoingMessageLock)
					{
						isSending = false;
					}
				}

				// Handle any queued messages.
				await HandleQueue(queue, messageType);
			}
			else
			{
				// Add the message to the queue.
				lock (OutgoingMessageLock)
				{
					queue.Add(buffer);
				}
			}
		}

		private async UniTask HandleQueue(List<ArraySegment<byte>> queue, WebSocketMessageType messageType)
		{
			var buffer = new ArraySegment<byte>();
			lock (OutgoingMessageLock)
			{
				// Check for an item in the queue.
				if (queue.Count > 0)
				{
					// Pull it off the top.
					buffer = queue[0];
					queue.RemoveAt(0);
				}
			}

			// Send that message.
			if (buffer.Count > 0)
			{
				await SendMessage(queue, messageType, buffer);
			}
		}

		private List<byte[]> m_MessageList = new List<byte[]>();

		// simple dispatcher for queued messages.
		public void DispatchMessageQueue()
		{
			if (m_MessageList.Count == 0)
			{
				return;
			}

			List<byte[]> messageListCopy;

			lock (IncomingMessageLock)
			{
				messageListCopy = new List<byte[]>(m_MessageList);
				m_MessageList?.Clear();
			}

			var len = messageListCopy.Count;
			for (int i = 0; i < len; i++)
			{
				OnMessage?.Invoke(messageListCopy[i]);
			}
		}

		public async UniTask Receive()
		{
			int closeCode = (int)WebSocketCloseCode.Abnormal;
			await new WaitForBackgroundThread();

			ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192]);
			try
			{
				while (m_Socket.State == System.Net.WebSockets.WebSocketState.Open)
				{
					WebSocketReceiveResult result = null;

					using (var ms = new MemoryStream())
					{
						do
						{
							result = await m_Socket.ReceiveAsync(buffer, m_CancellationToken);
							ms.Write(buffer.Array, buffer.Offset, result.Count);
						}
						while (!result.EndOfMessage);

						ms.Seek(0, SeekOrigin.Begin);

						if (result.MessageType == WebSocketMessageType.Text)
						{
							lock (IncomingMessageLock)
							{
								m_MessageList.Add(ms.ToArray());
							}

							//using (var reader = new StreamReader(ms, Encoding.UTF8))
							//{
							//    string message = reader.ReadToEnd();
							//    OnMessage?.Invoke(this, new MessageEventArgs(message));
							//}
						}
						else if (result.MessageType == WebSocketMessageType.Binary)
						{
							lock (IncomingMessageLock)
							{
								m_MessageList.Add(ms.ToArray());
							}
						}
						else if (result.MessageType == WebSocketMessageType.Close)
						{
							await Close();
							closeCode = (int)result.CloseStatus;
							break;
						}
					}
				}
			}
			catch (Exception)
			{
				m_TokenSource?.Cancel();
			}
			finally
			{
				await new WaitForUpdate();
				OnClose?.Invoke(closeCode);
			}
		}

		public async UniTask Close()
		{
			if (State == WebSocketState.Open)
			{
				await m_Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, m_CancellationToken);

				lock (OutgoingMessageLock)
				{
					sendBytesQueue?.Clear();
					sendTextQueue?.Clear();
				}

				lock (IncomingMessageLock)
				{
					m_MessageList?.Clear();
				}
			}
		}
	}
#endif
}
