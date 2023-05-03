using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NativeWebSocket;
// ReSharper disable InconsistentNaming

namespace Colyseus
{
    /// <summary>
    ///     WebSocket connection representation with some custom functionality
    /// </summary>
    public class ColyseusConnection : WebSocket
    {
        /// <summary>
        ///     Is the connection currently open
        /// </summary>
        public bool IsOpen;

        /// <summary>
        ///     Flag to keep processing function alive
        /// </summary>
        /// <remarks>Set to true via <see cref="_OnOpen" />, false via <see cref="_OnClose" /></remarks>
        protected bool ProcessingMessageQueue;

        public ColyseusConnection(string url, Dictionary<string, string> headers) : base(url, headers)
        {
            Initialize();
        }

        private void Initialize()
        {
            OnOpen += _OnOpen;
            OnClose += _OnClose;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
#else
        /// <summary>
        ///     A while loop that runs as long as the connection is open, triggering <see cref="WebSocket.DispatchMessageQueue" />
        /// </summary>
        public async void ProcessMessageQueue()
        {
            ProcessingMessageQueue = true;
            while (ProcessingMessageQueue)
            {
                DispatchMessageQueue();

                // Switch context
                await Task.Yield();
            }
        }
#endif

        /// <summary>
        ///     Functionality to run when connection is opened
        /// </summary>
        /// <remarks>Kick starts the <see cref="ProcessMessageQueue" /> while loop</remarks>
        protected void _OnOpen()
        {
            IsOpen = true;

#if UNITY_WEBGL && !UNITY_EDITOR
#else
            ProcessMessageQueue();
#endif
        }

        /// <summary>
        ///     Functionality to run when a connection closes
        /// </summary>
        /// <remarks>
        ///     Sets the <see cref="ProcessingMessageQueue" /> flag to false, stopping the
        ///     <see cref="ProcessingMessageQueue" /> while loop
        /// </remarks>
        /// <param name="code">The cause of the socket closure</param>
        protected void _OnClose(int code)
        {
            ProcessingMessageQueue = false;
            IsOpen = false;
            Debug.Log(string.Format("Websocket closed! Code:{0}", code.ToString()));
        }
    }
}
