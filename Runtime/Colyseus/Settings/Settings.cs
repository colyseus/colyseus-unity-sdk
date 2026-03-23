using System;
using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace Colyseus
{
    /// <summary>
    ///     Contains relevant Colyseus settings
    /// </summary>
#if UNITY_5_3_OR_NEWER
    [CreateAssetMenu(fileName = "MyServerSettings", menuName = "Colyseus/Server Settings Scriptable Object", order = 1)]
    [Serializable]
    public class Settings : ScriptableObject
#else
    [Serializable]
    public class Settings
#endif
    {
        /// <summary>
        ///     The server address
        /// </summary>
        public string colyseusServerAddress = "localhost";

        /// <summary>
        ///     The port to connect to
        /// </summary>
        public string colyseusServerPort = "2567";

        /// <summary>
        ///     If true, we use secure protocols (wss, https) otherwise we use ws, http (based on <see cref="useWss"/>)
        /// </summary>
        public bool useSecureProtocol = false;

        /// <summary>
        /// Internal wrapper class for a Request header
        /// </summary>
        [Serializable]
        public class RequestHeader
        {
            public string name;
            public string value;
        }

#if UNITY_5_3_OR_NEWER
        [SerializeField]
#endif
        private RequestHeader[] _requestHeaders;

        private Dictionary<string, string> _headersDictionary;

        public void SetRequestHeaders(RequestHeader[] headers)
        {
            _requestHeaders = new RequestHeader[headers.Length];
            for (int i = 0; i < headers.Length; i++)
            {
                _requestHeaders[i] = headers[i];
            }
        }

        public RequestHeader[] GetRequestHeaders()
        {
            return _requestHeaders;
        }

        /// <summary>
        /// Convert the user-defined <see cref="_requestHeaders"/> into a dictionary
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get
            {
                if (_headersDictionary == null)
                {
                    _headersDictionary = new Dictionary<string, string>();

					for (int i = 0; _requestHeaders != null && i < _requestHeaders.Length; ++i)
					{
						_headersDictionary.Add(_requestHeaders[i].name, _requestHeaders[i].value);
					}
				}

                return _headersDictionary;
            }
        }

        /// <summary>
        ///     Centralized location to build and return the WebSocketEndpoint
        /// </summary>
        public string WebSocketEndpoint
        {
            get
            {
				return BuildWebSocketEndpoint();
			}
        }

        /// <summary>
        ///     Centralized location to build and return an WebRequestEndpoint
        /// </summary>
        public string WebRequestEndpoint
        {
            get
            {
				return BuildWebRequestEndpoint();
			}
        }

        public static Settings Create()
        {
#if UNITY_5_3_OR_NEWER
            return CreateInstance<Settings>();
#else
            return new Settings();
#endif
        }

        /// <summary>
        /// Create a copy of the provided <see cref="Settings"/> object
        /// </summary>
        /// <param name="orig">The settings to copy</param>
        /// <returns>A new instance of <see cref="Settings"/> with values copied from the provided object</returns>
        public static Settings Clone(Settings orig)
        {
            Settings clone = Create();
            clone.colyseusServerAddress = orig.colyseusServerAddress;
            clone.colyseusServerPort = orig.colyseusServerPort;
            clone.useSecureProtocol = orig.useSecureProtocol;
            clone.SetRequestHeaders(orig.GetRequestHeaders());

            return clone;
        }

        private string BuildWebRequestEndpoint()
        {
	        UriBuilder webRequestEndpointBuilder = new UriBuilder(GetBaseEndpoint(GetWebRequestEndpointScheme()));

	        webRequestEndpointBuilder.Port = GetPort();

	        return webRequestEndpointBuilder.ToString();
        }

        private string BuildWebSocketEndpoint()
        {
	        UriBuilder webSocketEndpointBuilder = new UriBuilder(GetBaseEndpoint(GetWebSocketEndpointScheme()));

	        webSocketEndpointBuilder.Port = GetPort();

	        return webSocketEndpointBuilder.ToString();
        }

        private string GetBaseEndpoint(string scheme)
        {
	        return $"{scheme}://{colyseusServerAddress}";
        }

        public string GetWebSocketEndpointScheme()
        {
	        return (useSecureProtocol ? "wss" : "ws");
        }

        public string GetWebRequestEndpointScheme()
        {
	        return (useSecureProtocol ? "https" : "http");
        }

        public int GetPort()
        {
	        if (ShouldIncludeServerPort() && int.TryParse(colyseusServerPort, out int serverPort))
	        {
		        return serverPort;
	        }
	        else
	        {
		        return -1;
	        }
        }

        private bool ShouldIncludeServerPort()
        {
	        return !string.IsNullOrEmpty(colyseusServerPort) && !string.Equals(colyseusServerPort, "80") && !string.Equals(colyseusServerPort, "443");
        }
	}
}
