using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Colyseus
{
    public class ColyseusManager : MonoBehaviour
    {
		public Client Client;

        private const string SingletonName = "/[Colyseus]";
        private static readonly object Lock = new object();
        private static ColyseusManager _instance;

        /// <summary>
        /// The singleton instance of the Colyseus Manager.
        /// </summary>
        public static ColyseusManager Instance
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

                    if (go.GetComponent<ColyseusManager>() == null)
                    {
                        go.AddComponent<ColyseusManager>();
                    }
                    DontDestroyOnLoad(go);
                    _instance = go.GetComponent<ColyseusManager>();
                    return _instance;
                }
            }
        }

        public Client CreateClient(string endpoint)
        {
			Client = new Client(endpoint);
			return Client;
		}

		public async Task Connect()
		{
			await Client.Connect();
		}

		private void OnApplicationQuit()
		{
			if (Client != null)
			{
				foreach (KeyValuePair<string, IRoom> room in Client.rooms)
				{
					room.Value.Leave(false);
				}

				Client.Close();
			}
		}
	}
}