using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Colyseus
{
	/// <summary>
	///     Colyseus.Client
	/// </summary>
	/// <remarks>
	///     Provides integration between Colyseus Game Server through WebSocket protocol (
	///     <see href="http://tools.ietf.org/html/rfc6455">RFC 6455</see>).
	/// </remarks>
	public class ColyseusClient
	{
		/// <summary>
		///     Authentication tools, see: https://docs.colyseus.io/authentication/
		/// </summary>
		public Auth Auth;

		/// <summary>
		///     Reference to the client's <see cref="UriBuilder" />
		/// </summary>
		private UriBuilder Endpoint;

		/// <summary>
		/// Object to perform <see cref="UnityEngine.Networking.UnityWebRequest"/>s to the server.
		/// </summary>
		public HTTP Http;

		/// <summary>
		///     Initializes a new instance of the <see cref="ColyseusClient" /> class with
		///     the specified Colyseus Game Server endpoint.
		/// </summary>
		/// <param name="endpoint">
		///     A <see cref="string" /> that represents the WebSocket URL to connect.
		/// </param>
		public ColyseusClient(string endpoint)
		{
			Endpoint = new UriBuilder(endpoint);

			// Create ColyseusSettings object to pass to the ColyseusRequest object
			ColyseusSettings settings = ScriptableObject.CreateInstance<ColyseusSettings>();
			settings.colyseusServerAddress = $"{Endpoint.Host}{Endpoint.Path}";
			settings.colyseusServerPort = Endpoint.Port.ToString();
			settings.useSecureProtocol = string.Equals(Endpoint.Scheme, "wss") || string.Equals(Endpoint.Scheme, "https");

			Settings = settings;
			Auth = new Auth(this);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ColyseusClient"/> class with
		/// the specified Colyseus Settings object.
		/// </summary>
		/// <param name="settings">The settings you wish to use</param>
		/// <param name="useWebSocketEndpoint">Determines whether the connection endpoint should use either web socket or http protocols.</param>
		public ColyseusClient(ColyseusSettings settings)
		{
			Settings = settings;
			Auth = new Auth(this);
		}

		/// <summary>
		/// The getter for the <see cref="ColyseusSettings"/> currently assigned to this client object
		/// </summary>
		private ColyseusSettings _colyseusSettings;
		public ColyseusSettings Settings
		{
			get
			{
				return _colyseusSettings;
			}

			set
			{
				_colyseusSettings = value;

				Endpoint = new UriBuilder(_colyseusSettings.WebSocketEndpoint);

				// Instantiate our ColyseusRequest object with the settings object
				Http = new HTTP(_colyseusSettings);
			}
		}

		/// <summary>
		///     Join or Create a <see cref="ColyseusRoom{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon creation/joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we create/join the room</param>
		/// <typeparam name="T">Type of <see cref="ColyseusRoom{T}" /> we want to join or create</typeparam>
		/// <returns><see cref="ColyseusRoom{T}" /> via async task</returns>
		public async Task<ColyseusRoom<T>> JoinOrCreate<T>(string roomName, Dictionary<string, object> options = null, Dictionary<string, string> headers = null)
			where T : Schema.Schema
		{
			return await CreateMatchMakeRequest<T>("joinOrCreate", roomName, options, headers);
		}

		/// <summary>
		///     Create a <see cref="ColyseusRoom{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon creation</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we create the room</param>
		/// <typeparam name="T">Type of <see cref="ColyseusRoom{T}" /> we want to create</typeparam>
		/// <returns><see cref="ColyseusRoom{T}" /> via async task</returns>
		public async Task<ColyseusRoom<T>> Create<T>(string roomName, Dictionary<string, object> options = null, Dictionary<string, string> headers = null)
			where T : Schema.Schema
		{
			return await CreateMatchMakeRequest<T>("create", roomName, options, headers);
		}

		/// <summary>
		///     Join a <see cref="ColyseusRoom{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we join the room</param>
		/// <typeparam name="T">Type of <see cref="ColyseusRoom{T}" /> we want to join</typeparam>
		/// <returns><see cref="ColyseusRoom{T}" /> via async task</returns>
		public async Task<ColyseusRoom<T>> Join<T>(string roomName, Dictionary<string, object> options = null, Dictionary<string, string> headers = null)
			where T : Schema.Schema
		{
			return await CreateMatchMakeRequest<T>("join", roomName, options, headers);
		}

		/// <summary>
		///     Join a <see cref="ColyseusRoom{T}" /> by ID
		/// </summary>
		/// <param name="roomId">ID of the room</param>
		/// <param name="options">Dictionary of options to pass to the room upon joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we join the room</param>
		/// <typeparam name="T">Type of <see cref="ColyseusRoom{T}" /> we want to join</typeparam>
		/// <returns><see cref="ColyseusRoom{T}" /> via async task</returns>
		public async Task<ColyseusRoom<T>> JoinById<T>(string roomId, Dictionary<string, object> options = null, Dictionary<string, string> headers = null)
			where T : Schema.Schema
		{
			return await CreateMatchMakeRequest<T>("joinById", roomId, options, headers);
		}

		/// <summary>
		///     Reconnect to a <see cref="ColyseusRoom{T}" />
		/// </summary>
		/// <param name="reconnectionToken">Previously connected ReconnectionToken</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we reconnect to the room</param>
		/// <typeparam name="T">Type of <see cref="ColyseusRoom{T}" /> we want to reconnect with</typeparam>
		/// <returns><see cref="ColyseusRoom{T}" /> via async task</returns>
		public async Task<ColyseusRoom<T>> Reconnect<T>(ReconnectionToken reconnectionToken, Dictionary<string, string> headers = null)
			where T : Schema.Schema
		{
			Dictionary<string, object> options = new Dictionary<string, object>();
			options.Add("reconnectionToken", reconnectionToken.Token);
			return await CreateMatchMakeRequest<T>("reconnect", reconnectionToken.RoomId, options, headers);
		}

		//
		// FossilDelta/None serializer versions for joining the state
		//
		/// <summary>
		///     Join or Create a <see cref="ColyseusRoom{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon creation/joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we create/join the room</param>
		/// <returns><see cref="ColyseusRoom{T}" /> via async task</returns>
		public async Task<ColyseusRoom<NoState>> JoinOrCreate(string roomName, Dictionary<string, object> options = null, Dictionary<string, string> headers = null)
		{
			return await CreateMatchMakeRequest<NoState>("joinOrCreate", roomName, options, headers);
		}

		/// <summary>
		///     Create a <see cref="ColyseusRoom{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon creation</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we create the room</param>
		/// <returns><see cref="ColyseusRoom{T}" /> via async task</returns>
		public async Task<ColyseusRoom<NoState>> Create(string roomName, Dictionary<string, object> options = null,
			Dictionary<string, string> headers = null)
		{
			return await CreateMatchMakeRequest<NoState>("create", roomName, options, headers);
		}

		/// <summary>
		///     Join a <see cref="ColyseusRoom{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we join the room</param>
		/// <returns><see cref="ColyseusRoom{T}" /> via async task</returns>
		public async Task<ColyseusRoom<NoState>> Join(string roomName, Dictionary<string, object> options = null,
			Dictionary<string, string> headers = null)
		{
			return await CreateMatchMakeRequest<NoState>("join", roomName, options, headers);
		}

		/// <summary>
		///     Join a <see cref="ColyseusRoom{T}" /> by ID
		/// </summary>
		/// <param name="roomId">ID of the room</param>
		/// <param name="options">Dictionary of options to pass to the room upon joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we join the room</param>
		/// <returns><see cref="ColyseusRoom{T}" /> via async task</returns>
		public async Task<ColyseusRoom<NoState>> JoinById(string roomId, Dictionary<string, object> options = null,
			Dictionary<string, string> headers = null)
		{
			return await CreateMatchMakeRequest<NoState>("joinById", roomId, options, headers);
		}

		/// <summary>
		///     Reconnect to a <see cref="ColyseusRoom{T}" />
		/// </summary>
		/// <param name="roomId">ID of the room</param>
		/// <param name="sessionId">Previously connected sessionId</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we reconnect to the room</param>
		/// <returns><see cref="ColyseusRoom{T}" /> via async task</returns>
		public async Task<ColyseusRoom<NoState>> Reconnect(string roomId, string sessionId,
			Dictionary<string, string> headers = null)
		{
			Dictionary<string, object> options = new Dictionary<string, object>();
			options.Add("sessionId", sessionId);
			return await CreateMatchMakeRequest<NoState>("joinById", roomId, options, headers);
		}

		/// <summary>
		///     Consume the seat reservation
		/// </summary>
		/// <param name="response">The response from the matchmaking attempt</param>
		/// <param name="headers">Dictionary of headers to pass to the server</param>
		/// <param name="previousRoom">Previous ColyseusRoom{T} instance to re-establish the server connection: Please do not use this devMode param for general purposes</param>
		/// <typeparam name="T">Type of <see cref="ColyseusRoom{T}" /> we're consuming the seat from</typeparam>
		/// <returns><see cref="ColyseusRoom{T}" /> in which we now have a seat via async task</returns>
		public async Task<ColyseusRoom<T>> ConsumeSeatReservation<T>(ColyseusMatchMakeResponse response, Dictionary<string, string> headers = null, ColyseusRoom<T> previousRoom = null)
			where T : Schema.Schema
		{
			ColyseusRoom<T> room = new ColyseusRoom<T>(response.room.name)
			{
				RoomId = response.room.roomId,
				SessionId = response.sessionId
			};

			Dictionary<string, object> queryString = new Dictionary<string, object>
			{
				{ "sessionId", room.SessionId }
			};

			// forward reconnection token
			if (response.reconnectionToken != null)
			{
				queryString.Add("reconnectionToken", response.reconnectionToken);
			}

			ColyseusRoom<T> targetRoom = previousRoom ?? room;

			Action devModeCloseCallback = async () =>
			{
				Debug.Log($"<color=yellow>[Colyseus devMode]:</color> Re-establishing connection with room id {targetRoom.RoomId}");
				int devModeRetryAttempt = 0;
				const int devModeMaxRetryCount = 8;

				async Task retryConnection()
				{
					devModeRetryAttempt++;
					try
					{
						await ConsumeSeatReservation<T>(response, headers, targetRoom);
						Debug.Log($"<color=yellow>[Colyseus devMode]:</color> Successfully re-established connection with room {targetRoom.RoomId}");
					}
					catch (Exception)
					{
						if (devModeRetryAttempt < devModeMaxRetryCount)
						{
							Debug.Log($"<color=yellow>[Colyseus devMode]:</color> retrying... ({devModeRetryAttempt} out of {devModeMaxRetryCount})");
							await Task.Delay(2000);
							await retryConnection();
						}
						else
						{
							Debug.Log($"<color=yellow>[Colyseus devMode]:</color> Failed to reconnect! Is your server running? Please check server logs!");
						}
					}
				}

				await Task.Delay(2000);
				await retryConnection();
			};

			targetRoom.SetConnection(
				CreateConnection(response.room, queryString, headers),
				targetRoom,
				(response.devMode)
					? devModeCloseCallback
				: null
			);

			TaskCompletionSource<ColyseusRoom<T>> tcs = new TaskCompletionSource<ColyseusRoom<T>>();

			void OnError(int code, string message)
			{
				targetRoom.OnError -= OnError;
				tcs.SetException(new MatchMakeException(code, message));
			}

			void OnJoin()
			{
				targetRoom.OnError -= OnError;
				tcs.TrySetResult(targetRoom);
			}

			targetRoom.OnError += OnError;
			targetRoom.OnJoin += OnJoin;

#pragma warning disable 4014
			targetRoom.Connect();
#pragma warning restore 4014

			return await tcs.Task;
		}

		/// <summary>
		///     Create a match making request
		/// </summary>
		/// <param name="method">The type of request we're making (join, create, etc)</param>
		/// <param name="roomName">Room identifierroom we're trying to match</param>
		/// <param name="options">Dictionary of options to use in the match making process</param>
		/// <param name="headers">Dictionary of headers to pass to the server</param>
		/// <typeparam name="T">Type of <see cref="ColyseusRoom{T}" /> we want to match with</typeparam>
		/// <returns><see cref="ColyseusRoom{T}" /> we have matched with via async task</returns>
		/// <exception cref="Exception">Thrown if there is a network related error</exception>
		/// <exception cref="MatchMakeException">Thrown if there is an error in the match making process on the server side</exception>
		protected async Task<ColyseusRoom<T>> CreateMatchMakeRequest<T>(string method, string roomName, Dictionary<string, object> options, Dictionary<string, string> headers)
			where T : Schema.Schema
		{
			if (options == null)
			{
				options = new Dictionary<string, object>();
			}

			if (headers == null)
			{
				headers = new Dictionary<string, string>();
			}

			string json = await Http.Request("POST", $"matchmake/{method}/{roomName}", options, headers);
			//Debug.Log($"Server Response: {json}");

			ColyseusMatchMakeResponse response = JsonUtility.FromJson<ColyseusMatchMakeResponse>(json);
			if (response == null)
			{
				throw new Exception($"Error with request: {json}");
			}

			if (!string.IsNullOrEmpty(response.error))
			{
				throw new MatchMakeException(response.code, response.error);
			}

			// forward reconnection token on reconnect
			if (method == "reconnect")
			{
				response.reconnectionToken = (string)options["reconnectionToken"];
			}

			return await ConsumeSeatReservation<T>(response, headers);
		}

		/// <summary>
		///     Create a connection with a room
		/// </summary>
		/// <param name="path">Additional info used as the <see cref="UriBuilder.Path" /></param>
		/// <param name="options">Dictionary of options to use when connecting</param>
		/// <param name="headers">Dictionary of headers to pass when connecting</param>
		/// <returns></returns>
		protected ColyseusConnection CreateConnection(ColyseusRoomAvailable room, Dictionary<string, object> options = null,
			Dictionary<string, string> headers = null)
		{
			if (options == null)
			{
				options = new Dictionary<string, object>();
			}

			// Add authentication token to query string
			if (!string.IsNullOrEmpty(Http.AuthToken))
			{
				options.Add("_authToken", Http.AuthToken);
			}

			List<string> list = new List<string>();
			foreach (KeyValuePair<string, object> item in options)
			{
				list.Add(item.Key + "=" + (item.Value != null ? Convert.ToString(item.Value) : "null"));
			}

			// Try to connect directly to custom publicAddress, if present.
			var endpoint = (room.publicAddress != null && room.publicAddress.Length > 0)
				? new Uri($"{Endpoint.Scheme}://{room.publicAddress}")
				: Endpoint.Uri;

			var basePath = endpoint.AbsolutePath;

			// make sure to end path with backslash
			if (basePath.Length > 0 && !basePath.EndsWith("/"))
			{
				basePath += "/";
			}

			UriBuilder uriBuilder = new UriBuilder(endpoint)
			{
				Path = $"{basePath}{room.processId}/{room.roomId}",
				Query = string.Join("&", list.ToArray())
			};

			return new ColyseusConnection(uriBuilder.ToString(), headers);
		}
	}
}

