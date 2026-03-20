#if UNITY_5_3_OR_NEWER
using System;
using UnityEngine;

namespace Colyseus
{
    /// <summary>
    /// ScriptableObject wrapper around the core Settings POCO for Unity inspector compatibility.
    /// Maintains backward compatibility with existing Unity projects that use Settings as a ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "MyServerSettings", menuName = "Colyseus/Server Settings Scriptable Object", order = 1)]
    [Serializable]
    public class UnitySettings : ScriptableObject
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
        ///     If true, we use secure protocols (wss, https) otherwise we use ws, http
        /// </summary>
        public bool useSecureProtocol = false;

        [SerializeField]
        private Settings.RequestHeader[] _requestHeaders;

        /// <summary>
        /// Convert this UnitySettings to a core Settings object
        /// </summary>
        public Settings ToSettings()
        {
            var settings = new Settings
            {
                colyseusServerAddress = colyseusServerAddress,
                colyseusServerPort = colyseusServerPort,
                useSecureProtocol = useSecureProtocol
            };

            if (_requestHeaders != null)
            {
                settings.SetRequestHeaders(_requestHeaders);
            }

            return settings;
        }

        /// <summary>
        /// Implicit conversion to Settings for backward compatibility
        /// </summary>
        public static implicit operator Settings(UnitySettings unitySettings)
        {
            return unitySettings.ToSettings();
        }
    }
}
#endif
