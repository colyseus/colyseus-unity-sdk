using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GameDevWare.Serialization;
#if !WINDOWS_UWP
using WebSocketSharp;
#endif
using UnityEngine;

namespace Colyseus
{
    /// <summary>
    /// Colyseus.Client
    /// </summary>
    /// <remarks>
    /// Provides integration between Colyseus Game Server through WebSocket protocol (<see href="http://tools.ietf.org/html/rfc6455">RFC 6455</see>).
    /// </remarks>
    public class Client
    {
        /// <summary>
        /// Unique <see cref="Client"/> identifier.
        /// </summary>
        public string id = null;

        public WebSocket ws;
        private Dictionary<string, Room> rooms = new Dictionary<string, Room>();

        // Events

        /// <summary>
        /// Occurs when the <see cref="Client"/> connection has been established, and Client <see cref="id"/> is available.
        /// </summary>
        public event EventHandler OnOpen;

        /// <summary>
        /// Occurs when the <see cref="Client"/> connection has been closed.
        /// </summary>
        public event EventHandler OnClose;

        /// <summary>
        /// Occurs when the <see cref="Client"/> gets an error.
        /// </summary>
        public event EventHandler OnError;

        /// <summary>
        /// Occurs when the <see cref="Client"/> receives a message from server.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMessage;

        // TODO: implement auto-reconnect feature
        // public event EventHandler OnReconnect;

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class with
        /// the specified Colyseus Game Server Server endpoint.
        /// </summary>
        /// <param name="endpoint">
        /// A <see cref="string"/> that represents the WebSocket URL to connect.
        /// </param>
        public Client(string endpoint)
        {
            this.ws = new WebSocket(new Uri(endpoint));

            //this.ws.OnMessage += OnMessageHandler;
            //this.ws.OnClose += OnCloseHandler;
            //this.ws.OnError += OnErrorHandler;
        }

        public IEnumerator Connect()
        {
            return this.ws.Connect();
        }

        public void Recv()
        {
            byte[] data = this.ws.Recv();
            if (data != null)
            {
                this.ParseMessage(data);
            }
        }

#if !WINDOWS_UWP
        void OnCloseHandler(object sender, CloseEventArgs e)
        {
            this.OnClose.Emit(this, e);
        }
#else
        void OnCloseHandler(object sender, EventArgs e)
        {
            this.OnClose.Invoke(this, e);
        }
#endif

        void ParseMessage(byte[] recv)
        {
            var stream = new MemoryStream(recv);
            var raw = MsgPack.Deserialize<List<object>>(stream);

            //object[] message = new object[raw.Values.Count];
            //raw.Values.CopyTo(message, 0);

            var message = raw;
            var code = (byte)message[0];

            // Parse roomId or roomName
            Room room = null;
            int roomIdInt32 = 0;
            string roomId = "0";
            string roomName = null;

            try
            {
                roomIdInt32 = (byte)message[1];
                roomId = roomIdInt32.ToString();
            }
            catch (Exception)
            {
                try
                {
                    roomName = (string)message[1];
                }
                catch (Exception)
                {
                }
            }

            if (code == Protocol.USER_ID)
            {
                this.id = (string)message[1];
                this.OnOpen.Invoke(this, EventArgs.Empty);
            }
            else if (code == Protocol.JOIN_ROOM)
            {
                roomName = (string)message[2];

                if (this.rooms.ContainsKey(roomName))
                {
                    this.rooms[roomId] = this.rooms[roomName];
                    this.rooms.Remove(roomName);
                }

                room = this.rooms[roomId];
                room.id = roomIdInt32;
            }
            else if (code == Protocol.JOIN_ERROR)
            {
                room = this.rooms[roomName];

                MessageEventArgs error = new MessageEventArgs(room, message);
                room.EmitError(error);
                this.OnError.Invoke(this, error);
                this.rooms.Remove(roomName);
            }
            else if (code == Protocol.LEAVE_ROOM)
            {
                room = this.rooms[roomId];
                room.Leave(false);
            }
            else if (code == Protocol.ROOM_STATE)
            {
                var state = (IndexedDictionary<string, object>)message[2];
                var remoteCurrentTime = (double)message[3];
                var remoteElapsedTime = (byte)message[4];

                room = this.rooms[roomId];
                // JToken.Parse (message [2].ToString ())
                room.SetState(state, remoteCurrentTime, remoteElapsedTime);
            }
            else if (code == Protocol.ROOM_STATE_PATCH)
            {
                room = this.rooms[roomId];

                var patchBytes = (List<object>)message[2];
                byte[] patches = new byte[patchBytes.Count];

                int idx = 0;
                foreach (byte obj in patchBytes)
                {
                    patches[idx] = obj;
                    idx++;
                }

                room.ApplyPatch(patches);
            }
            else if (code == Protocol.ROOM_DATA)
            {
                room = this.rooms[roomId];
                room.ReceiveData(message[2]);
                this.OnMessage.Invoke(this, new MessageEventArgs(room, message[2]));
            }
        }

        /// <summary>
        /// Request <see cref="Client"/> to join in a <see cref="Room"/>.
        /// </summary>
        /// <param name="roomName">The name of the Room to join.</param>
        /// <param name="options">Custom join request options</param>
        public Room Join(string roomName, object options = null)
        {
            if (!this.rooms.ContainsKey(roomName))
            {
                this.rooms.Add(roomName, new Room(this, roomName));
            }

            this.Send(new object[] { Protocol.JOIN_ROOM, roomName, options });

            return this.rooms[roomName];
        }

        private void OnErrorHandler(object sender, EventArgs args)
        {
            this.OnError.Invoke(sender, args);
        }

        /// <summary>
        /// Send data to all connected rooms.
        /// </summary>
        /// <param name="data">Data to be sent to all connected rooms.</param>
        public void Send(object[] data)
        {
            var stream = new MemoryStream();
            MsgPack.Serialize(data, stream);
            var ser = stream.ToArray();
            this.ws.Send(ser);
        }


        /// <summary>
        /// Close <see cref="Client"/> connection and leave all joined rooms.
        /// </summary>
        public void Close()
        {
            this.ws.Close();
        }

        public string error
        {
            get { return this.ws.error; }
        }
    }
}