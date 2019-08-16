using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

using GameDevWare.Serialization;

using UnityEngine;
using UnityEngine.Networking;

namespace Colyseus
{
	[Serializable]
	public class RoomListingData
	{
		public uint clients;
		public uint maxClients;
		public string name;
		public string roomId;
		public object metadata;
		public string processId;
	}

	[Serializable]
	public class RoomListingCollection
	{
		public RoomListingData[] rooms;
	}

	[Serializable]
	public class MatchMakeResponse
	{
		// success
		public RoomListingData room;
		public string sessionId;
		// error
		public int code;
		public string error;
	}

	public class MatchMakeException : Exception
	{
		public int Code;
		public MatchMakeException(string message, int code) : base(message)
		{
			Code = code;
		}
	}


	/// <summary>
	/// Colyseus.Client
	/// </summary>
	/// <remarks>
	/// Provides integration between Colyseus Game Server through WebSocket protocol (<see href="http://tools.ietf.org/html/rfc6455">RFC 6455</see>).
	/// </remarks>
	public class Client
	{
		public Auth Auth;
		public UriBuilder Endpoint;

		/// <summary>
		/// Initializes a new instance of the <see cref="Client"/> class with
		/// the specified Colyseus Game Server Server endpoint.
		/// </summary>
		/// <param name="endpoint">
		/// A <see cref="string"/> that represents the WebSocket URL to connect.
		/// </param>
		public Client (string endpoint)
		{
			Endpoint = new UriBuilder(new Uri (endpoint));
			Auth = new Auth(Endpoint.Uri);
		}

		public async Task<Room<T>> JoinOrCreate<T>(string roomName, Dictionary<string, object> options = null)
		{
			return await CreateMatchMakeRequest<T>("joinOrCreate", roomName, options);
		}

		public async Task<Room<T>> Create<T>(string roomName, Dictionary<string, object> options = null)
		{
			return await CreateMatchMakeRequest<T>("create", roomName, options);
		}

		public async Task<Room<T>> Join<T>(string roomName, Dictionary<string, object> options = null)
		{
			return await CreateMatchMakeRequest<T>("join", roomName, options);
		}

		public async Task<Room<T>> JoinById<T>(string roomId, Dictionary<string, object> options = null)
		{
			return await CreateMatchMakeRequest<T>("joinById", roomId, options);
		}

		public async Task<Room<T>> Reconnect<T>(string roomId, string sessionId)
		{
			Dictionary<string, object> options = new Dictionary<string, object>();
			options.Add("sessionId", sessionId);
			return await CreateMatchMakeRequest<T>("joinById", roomId, options);
		}

		//public async Task<Room<IndexedDictionary<string, object>>> Join(string roomName, Dictionary<string, object> options = null)
		//{
		//	return await Join<IndexedDictionary<string, object>>(roomName, options);
		//}

		//public async Task<Room<IndexedDictionary<string, object>>> ReJoin (string roomName, string sessionId)
		//{
		//	return await ReJoin<IndexedDictionary<string, object>>(roomName, sessionId);
		//}

		public async Task<RoomListingData[]> GetAvailableRooms (string roomName = "")
		{
			var uriBuilder = new UriBuilder(Endpoint.Uri);
			uriBuilder.Path += "matchmake/" + roomName;
			uriBuilder.Scheme = uriBuilder.Scheme.Replace("ws", "http"); // FIXME: replacing "ws" with "http" is too hacky!

			var req = new UnityWebRequest();
			req.method = "GET";
			req.url = uriBuilder.Uri.ToString();

			req.SetRequestHeader("Accept", "application/json");

			req.downloadHandler = new DownloadHandlerBuffer();
			await req.SendWebRequest();

			var json = req.downloadHandler.text;
			if (json.StartsWith("[", StringComparison.CurrentCulture))
			{
				json = "{\"rooms\":" + json + "}";
			}

			var response = JsonUtility.FromJson<RoomListingCollection>(json);
			return response.rooms;
		}

		protected async Task<Room<T>> CreateMatchMakeRequest<T>(string method, string roomName, Dictionary<string, object> options)
		{
			if (options == null)
			{
				options = new Dictionary<string, object>();
			}

			if (Auth.HasToken)
			{
				options.Add("token", Auth.Token);
			}

			var uriBuilder = new UriBuilder(Endpoint.Uri);
			uriBuilder.Path += "matchmake/" + method + "/" + roomName;
			uriBuilder.Scheme = uriBuilder.Scheme.Replace("ws", "http"); // FIXME: replacing "ws" with "http" is too hacky!

			var req = new UnityWebRequest();
			req.method = "POST";

			req.url = uriBuilder.Uri.ToString();

			// Send JSON options on request body
			var jsonBodyStream = new MemoryStream();
			Json.Serialize(options, jsonBodyStream);

			req.uploadHandler = new UploadHandlerRaw(jsonBodyStream.ToArray())
			{
				contentType = "application/json"
			};
			req.SetRequestHeader("Content-Type", "application/json");
			req.SetRequestHeader("Accept", "application/json");

			req.downloadHandler = new DownloadHandlerBuffer();
			await req.SendWebRequest();

			if (req.isNetworkError || req.isHttpError)
			{
				throw new Exception(req.error);
			}

			var response = JsonUtility.FromJson<MatchMakeResponse>(req.downloadHandler.text);
			if (!string.IsNullOrEmpty(response.error))
			{
				throw new MatchMakeException(response.error, response.code);
			}

			var room = new Room<T>(roomName)
			{
				Id = response.room.roomId,
				SessionId = response.sessionId
			};

			var queryString = new Dictionary<string, object>();
			queryString.Add("sessionId", room.SessionId);

			room.SetConnection(CreateConnection(response.room.processId + "/" + room.Id, queryString));

			var tcs = new TaskCompletionSource<Room<T>>();

			void OnError(string message)
			{
				room.OnError -= OnError;
				tcs.SetException(new Exception(message));
			};

			void OnJoin()
			{
				room.OnError -= OnError;
				tcs.TrySetResult(room);
			}

			room.OnError += OnError;
			room.OnJoin += OnJoin;

			ColyseusManager.Instance.AddRoom(room);

			return await tcs.Task;
		}

		protected Connection CreateConnection (string path = "", Dictionary<string, object> options = null)
		{
			if (options == null) {
				options = new Dictionary<string, object> ();
			}

			var list = new List<string>();
			foreach(var item in options)
			{
				list.Add(item.Key + "=" + ((item.Value != null) ? Convert.ToString(item.Value) : "null") );
			}

			UriBuilder uriBuilder = new UriBuilder(Endpoint.Uri)
			{
				Path = path,
				Query = string.Join("&", list.ToArray())
			};

			return new Connection (uriBuilder.ToString());
		}

	}

}
