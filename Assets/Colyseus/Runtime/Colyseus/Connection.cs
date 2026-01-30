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
    public class Connection : WebSocket
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

        public Connection(string url, Dictionary<string, string> headers) : base(url, headers)
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
		public void Drop()
		{
			m_Socket.Abort();
		}

        /// <summary>
        ///     Reconnect to the same endpoint with a new reconnection token
        /// </summary>
        /// <param name="reconnectionToken">The token to use for reconnection</param>
        public async Task Reconnect(string reconnectionToken)
        {
			//
			// TODO: refactor here. we should have a single code path for both WebGL and non-WebGL scenarios.
			//
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: Need to create new instance with modified URL
            var originalUri = new Uri(url);
            var query = HttpUtility.ParseQueryString(originalUri.Query);
            query["reconnectionToken"] = reconnectionToken;
            var uriBuilder = new UriBuilder(originalUri) { Query = query.ToString() };

            // Destroy old instance and create new one
            WebSocketFactory.HandleInstanceDestroy(instanceId);
            WebSocketFactory.instances.Remove(instanceId);

            url = uriBuilder.ToString();
            instanceId = WebSocketFactory.WebSocketAllocate(url);
            WebSocketFactory.instances.Add(instanceId, this);

            await Connect();
#else
            // Non-WebGL: Modify URI and reconnect
            var query = HttpUtility.ParseQueryString(uri.Query);
            query["reconnectionToken"] = reconnectionToken;
            var uriBuilder = new UriBuilder(uri) { Query = query.ToString() };
            uri = uriBuilder.Uri;

            await Connect();
#endif
        }

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
        }

    }
}
