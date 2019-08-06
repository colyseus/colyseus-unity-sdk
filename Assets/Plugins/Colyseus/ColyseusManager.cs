using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Colyseus
{
    /// <summary>
    /// Manages a Nakama client and optional socket connection.
    /// </summary>
    /// <seealso cref="NakamaManagerUsage"/>
    public class ColyseusManager : MonoBehaviour
    {
        private const string SessionPrefName = "nakama.session";
        private const string SingletonName = "/[NakamaManager]";

        private static readonly object Lock = new object();
        private static NakamaManager _instance;

        /// <summary>
        /// The singleton instance of the Nakama sdk manager.
        /// </summary>
        public static NakamaManager Instance
        {
            get
            {
                lock (Lock)
                {
                    if (_instance != null) return _instance;
                    var go = GameObject.Find(SingletonName);
                    if (go == null)
                    {
                        go = new GameObject(SingletonName);
                    }

                    if (go.GetComponent<NakamaManager>() == null)
                    {
                        go.AddComponent<NakamaManager>();
                    }
                    DontDestroyOnLoad(go);
                    _instance = go.GetComponent<NakamaManager>();
                    return _instance;
                }
            }
        }

        public IClient Client { get; }
        public ISocket Socket { get; }

        public Task<ISession> Session { get; private set; }

        private NakamaManager()
        {
            Client = new Client("http", "127.0.0.1", 7350, "defaultkey")
            {
#if UNITY_EDITOR
                Logger = new UnityLogger()
#endif
            };
            Socket = Client.NewSocket();
        }

        private Task<ISession> AuthenticateAsync()
        {
            // Modify to fit the authentication strategy you want within your game.
            // EXAMPLE:
            const string deviceIdPrefName = "deviceid";
            var deviceId = PlayerPrefs.GetString(deviceIdPrefName, SystemInfo.deviceUniqueIdentifier);
#if UNITY_EDITOR
            Debug.LogFormat("Device id: {0}", deviceId);
#endif
            // With device IDs save it locally in case of OS updates which can change the value on device.
            PlayerPrefs.SetString(deviceIdPrefName, deviceId);
            return Client.AuthenticateDeviceAsync(deviceId);
        }

        private void Awake()
        {
            // Restore session or create a new one.
            var authToken = PlayerPrefs.GetString(SessionPrefName);
            var session = Nakama.Session.Restore(authToken);
            var expiredDate = DateTime.UtcNow.AddDays(-1);
            if (session == null || session.HasExpired(expiredDate))
            {
                var sessionTask = AuthenticateAsync();
                Session = sessionTask;
                sessionTask.ContinueWith(t =>
                {
                    if (t.IsCompleted)
                    {
                        PlayerPrefs.SetString(SessionPrefName, t.Result.AuthToken);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                Session = Task.FromResult(session);
            }
        }

        private void OnApplicationQuit() => Socket?.CloseAsync();
    }
}