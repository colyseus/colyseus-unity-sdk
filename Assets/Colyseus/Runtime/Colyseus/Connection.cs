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
			CancelConnection();
		}

        /// <summary>
        ///     Reconnect to the same endpoint with a new reconnection token
        /// </summary>
        /// <param name="reconnectionToken">The token to use for reconnection</param>
        public async Task Reconnect(string reconnectionToken)
        {
            // Build query string manually (System.Web.HttpUtility is not available in Unity)
            var queryParams = new List<string>();

            // Preserve existing query parameters
            if (!string.IsNullOrEmpty(uri.Query))
            {
                var existingQuery = uri.Query.TrimStart('?');
                if (!string.IsNullOrEmpty(existingQuery))
                {
                    foreach (var param in existingQuery.Split('&'))
                    {
                        var key = param.Split('=')[0];
                        // Skip params we're going to override
                        if (key != "reconnectionToken" && key != "skipHandshake")
                        {
                            queryParams.Add(param);
                        }
                    }
                }
            }

            queryParams.Add("reconnectionToken=" + Uri.EscapeDataString(reconnectionToken));
            queryParams.Add("skipHandshake=1");

            var uriBuilder = new UriBuilder(uri) { Query = string.Join("&", queryParams) };
            uri = uriBuilder.Uri;

			//
			// TODO: refactor here. we should have a single code path for both WebGL and non-WebGL scenarios.
			//
#if UNITY_WEBGL && !UNITY_EDITOR
            // Destroy old instance and create new one
            WebSocketFactory.HandleInstanceDestroy(instanceId);
            WebSocketFactory.instances.Remove(instanceId);

            url = uriBuilder.ToString();
            instanceId = WebSocketFactory.WebSocketAllocate(url);
            WebSocketFactory.instances.Add(instanceId, this);
#endif

			Debug.Log($"Reconnecting to {uri}");
            await Connect();
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
