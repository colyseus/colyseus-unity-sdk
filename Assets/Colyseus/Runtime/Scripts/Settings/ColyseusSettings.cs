using System;
using System.Collections.Generic;
using UnityEngine;

namespace Colyseus
{
    /// <summary>
    ///     <see cref="ScriptableObject" /> containing relevant Colyseus settings
    /// </summary>
    [CreateAssetMenu(fileName = "MyServerSettings", menuName = "Colyseus/Generate ColyseusSettings Scriptable Object", order = 1)]
    [Serializable]
    public class ColyseusSettings : ScriptableObject
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
        /// Internal wrapper class for a <see cref="UnityEngine.Networking.UnityWebRequest"/> Request header since Unity cant serialize arrays
        /// </summary>
        [Serializable]
        public class RequestHeader
        {
            public string name;
            public string value;
        }

        [SerializeField]
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
        /// Convert the user-defined <see cref="_requestHeaders"/> into a dictionary to be used in a <see cref="UnityEngine.Networking.UnityWebRequest"/>
        /// </summary>
        public Dictionary<string, string> HeadersDictionary
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
                if (string.IsNullOrEmpty(colyseusServerPort) || colyseusServerPort.Equals(("80")))
                {
                    return (useSecureProtocol ? "wss" : "ws") + "://" + colyseusServerAddress;
                }

                return (useSecureProtocol ? "wss" : "ws") + "://" + colyseusServerAddress + ":" + colyseusServerPort;
            }
        }

        /// <summary>
        ///     Centralized location to build and return an WebSocketEndpoint ignoring WS/WSS protocols for Unity Web Requests
        /// </summary>
        public string WebRequestEndpoint
        {
            get
            {
                if (string.IsNullOrEmpty(colyseusServerPort) || colyseusServerPort.Equals(("80")))
                {
                    return (useSecureProtocol ? "https" : "http") + "://" + colyseusServerAddress;
                }

                return (useSecureProtocol ? "https" : "http") + "://" + colyseusServerAddress + ":" + colyseusServerPort;
            }
        }

        /// <summary>
        /// Create a copy of the provided <see cref="ColyseusSettings"/> object
        /// </summary>
        /// <param name="orig">The settings to copy</param>
        /// <returns>A new instance of <see cref="ColyseusSettings"/> with values copied from the provided object</returns>
        public static ColyseusSettings Clone(ColyseusSettings orig)
        {
            ColyseusSettings clone = CreateInstance<ColyseusSettings>();
            clone.colyseusServerAddress = orig.colyseusServerAddress;
            clone.colyseusServerPort = orig.colyseusServerPort;
            clone.useSecureProtocol = orig.useSecureProtocol;
            clone.SetRequestHeaders(orig.GetRequestHeaders());

            return clone;
        }
    }
}