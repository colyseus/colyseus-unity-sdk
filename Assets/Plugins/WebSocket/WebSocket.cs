using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using UnityEngine;
using System.Runtime.InteropServices;

#if WINDOWS_UWP
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Networking.Sockets;
#endif

public class WebSocket
{
	private Uri mUrl;

	public event EventHandler OnOpen;
	public event EventHandler OnClose;
	public event EventHandler<Colyseus.ErrorEventArgs> OnError;

	public WebSocket(Uri url)
	{
		mUrl = url;

		string protocol = mUrl.Scheme;
		if (!protocol.Equals("ws") && !protocol.Equals("wss"))
			throw new ArgumentException("Unsupported protocol: " + protocol);
	}

	public void SendString(string str)
	{
		Send(Encoding.UTF8.GetBytes (str));
	}

	public string RecvString()
	{
		byte[] retval = Recv();
		if (retval == null)
			return null;
		return Encoding.UTF8.GetString (retval);
	}

#if UNITY_WEBGL && !UNITY_EDITOR
	[DllImport("__Internal")]
	private static extern int SocketCreate (string url);

	[DllImport("__Internal")]
	private static extern int SocketState (int socketInstance);

	[DllImport("__Internal")]
	private static extern void SocketSend (int socketInstance, byte[] ptr, int length);

	[DllImport("__Internal")]
	private static extern void SocketRecv (int socketInstance, byte[] ptr, int length);

	[DllImport("__Internal")]
	private static extern int SocketRecvLength (int socketInstance);

	[DllImport("__Internal")]
	private static extern void SocketClose (int socketInstance);

	[DllImport("__Internal")]
	private static extern int SocketError (int socketInstance, byte[] ptr, int length);

	int m_NativeRef = 0;

	public void Send(byte[] buffer)
	{
		SocketSend (m_NativeRef, buffer, buffer.Length);
	}

	public byte[] Recv()
	{
		int length = SocketRecvLength (m_NativeRef);
		if (length == 0)
			return null;
		byte[] buffer = new byte[length];
		SocketRecv (m_NativeRef, buffer, length);
		return buffer;
	}

	public IEnumerator Connect()
	{
		m_NativeRef = SocketCreate (mUrl.ToString());

		while (SocketState(m_NativeRef) == 0)
			yield return 0;

		if (OnOpen != null) {
			OnOpen.Invoke(this, new EventArgs());
		}
	}

	public void Close()
	{
		SocketClose(m_NativeRef);

		if (OnClose != null) {
			OnClose.Invoke(this, new EventArgs());
		}
	}

	public string error
	{
		get {
			const int bufsize = 1024;
			byte[] buffer = new byte[bufsize];
			int result = SocketError (m_NativeRef, buffer, bufsize);

			if (result == 0)
				return null;

			return Encoding.UTF8.GetString (buffer);
		}
	}
#elif WINDOWS_UWP
	MessageWebSocket m_Socket;
	Queue<byte[]> m_Messages = new Queue<byte[]>();
	bool m_IsConnected = false;
	string m_Error = null;

	public IEnumerator Connect()
	{
		m_Socket = new MessageWebSocket();
		m_Socket.Control.MessageType = SocketMessageType.Binary;
		m_Socket.MessageReceived += M_Socket_MessageReceived;
		m_Socket.Closed += M_Socket_Closed;

		TryConnect();

		while (!m_IsConnected && m_Error == null)
		  yield return 0;
	}

	private async void TryConnect()
	{
		Debug.Log("Trying to connect to: " + mUrl.ToString());
		try
		{
			await m_Socket.ConnectAsync(mUrl);
			m_IsConnected = true;
			if (OnOpen != null) {
				OnOpen.Invoke(sender, e);
			}
			Debug.Log("Connected");
		}
		catch (Exception ex)
		{
			Debug.Log("Error while connecting!");
			Debug.Log(ex.Source);
			Debug.Log(ex.Message);

			OnError.Invoke(this, new Colyseus.ErrorEventArgs (ex.Message));
		}
	}

    private void M_Socket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
    {
		if (OnClose != null) {
			OnClose.Invoke(sender, e);
		}
        m_Error = args.Reason;
    }

    private void M_Socket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
    {
        DataReader messageReader = args.GetDataReader();
        messageReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

        byte[] message = new byte[messageReader.UnconsumedBufferLength];
        messageReader.ReadBytes(message);

        m_Messages.Enqueue (message);
    }

    public async void Send(byte[] buffer)
	{
		try {
			await SendMessage(m_Socket, buffer);
		}
		catch(Exception e){
			Debug.Log("Exception while sending, error: " + e.Message);
		}
	}

    private async Task SendMessage(MessageWebSocket webSock, byte[] buffer)
    {
        DataWriter messageWriter = new DataWriter(webSock.OutputStream);
        messageWriter.ByteOrder = ByteOrder.BigEndian;
        messageWriter.WriteBytes(buffer);

        try {
			await messageWriter.StoreAsync();
			await messageWriter.FlushAsync();
			messageWriter.DetachStream();
        }
        catch (Exception e)
        {
			Debug.Log("Exception while sending message, error: " + e.Message);
        }
    }

    public byte[] Recv()
	{
		if (m_Messages.Count == 0)
			return null;
		return m_Messages.Dequeue();
	}

	public void Close()
	{
        m_Socket.Close(1000, "");
	}

	public string error
	{
		get {
			return m_Error;
		}
	}
#else
    WebSocketSharp.WebSocket m_Socket;
	Queue<byte[]> m_Messages = new Queue<byte[]>();
	bool m_IsConnected = false;

	public IEnumerator Connect()
	{
		m_Socket = new WebSocketSharp.WebSocket(mUrl.ToString());

		m_Socket.OnMessage += (sender, e) => m_Messages.Enqueue (e.RawData);

		m_Socket.OnOpen += (sender, e) => {
			Debug.Log("WebSocketSharp Open!");
			if (OnOpen != null) {
				OnOpen.Invoke(sender, e);
			}
			m_IsConnected = true;
		};

		m_Socket.OnClose += (sender, e) => {
			Debug.Log("WebSocketSharp Close!");
			if (OnClose != null) {
				OnClose.Invoke(sender, e);
			}
		};

		m_Socket.OnError += (sender, e) => {
			Debug.Log("WebSocketSharp Error!");
			if (this.OnError != null) {
				this.OnError.Invoke (this, new Colyseus.ErrorEventArgs (e.Message));
			}
		};

		m_Socket.ConnectAsync();

		while (!m_IsConnected) //  && m_Error == null
			yield return 0;
	}

	public void Send(byte[] buffer)
	{
		m_Socket.Send(buffer);
	}

	public byte[] Recv()
	{
		if (m_Messages.Count == 0)
			return null;
		return m_Messages.Dequeue();
	}

	public void Close()
	{
		m_Socket.Close();
	}
#endif
}
