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
	public class Client
	{
		/// <summary>
		///     Authentication tools, see: https://docs.colyseus.io/auth/
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
		///     Initializes a new instance of the <see cref="Client" /> class with
		///     the specified Colyseus Game Server endpoint.
		/// </summary>
		/// <param name="endpoint">
		///     A <see cref="string" /> that represents the WebSocket URL to connect.
		/// </param>
		public Client(string endpoint)
		{
			Endpoint = new UriBuilder(endpoint);

			// Create Settings object to pass to the ColyseusRequest object
			Settings settings = ScriptableObject.CreateInstance<Settings>();
			settings.colyseusServerAddress = $"{Endpoint.Host}{Endpoint.Path}";
			settings.colyseusServerPort = Endpoint.Port.ToString();
			settings.useSecureProtocol = string.Equals(Endpoint.Scheme, "wss") || string.Equals(Endpoint.Scheme, "https");

			Settings = settings;
			Auth = new Auth(this);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Client"/> class with
		/// the specified Colyseus Settings object.
		/// </summary>
		/// <param name="settings">The settings you wish to use</param>
		/// <param name="useWebSocketEndpoint">Determines whether the connection endpoint should use either web socket or http protocols.</param>
		public Client(Settings settings)
		{
			Settings = settings;
			Auth = new Auth(this);
		}

		/// <summary>
		/// The getter for the <see cref="Settings"/> currently assigned to this client object
		/// </summary>
		private Settings _colyseusSettings;
		public Settings Settings
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
		///     Join or Create a <see cref="Room{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon creation/joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we create/join the room</param>
		/// <typeparam name="T">Type of <see cref="Room{T}" /> we want to join or create</typeparam>
		/// <returns><see cref="Room{T}" /> via async task</returns>
		public async Task<Room<T>> JoinOrCreate<T>(string roomName, Dictionary<string, object> options = null, Dictionary<string, string> headers = null)
			where T : Schema.Schema
		{
			return await CreateMatchMakeRequest<T>("joinOrCreate", roomName, options, headers);
		}

		/// <summary>
		///     Create a <see cref="Room{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon creation</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we create the room</param>
		/// <typeparam name="T">Type of <see cref="Room{T}" /> we want to create</typeparam>
		/// <returns><see cref="Room{T}" /> via async task</returns>
		public async Task<Room<T>> Create<T>(string roomName, Dictionary<string, object> options = null, Dictionary<string, string> headers = null)
			where T : Schema.Schema
		{
			return await CreateMatchMakeRequest<T>("create", roomName, options, headers);
		}

		/// <summary>
		///     Join a <see cref="Room{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we join the room</param>
		/// <typeparam name="T">Type of <see cref="Room{T}" /> we want to join</typeparam>
		/// <returns><see cref="Room{T}" /> via async task</returns>
		public async Task<Room<T>> Join<T>(string roomName, Dictionary<string, object> options = null, Dictionary<string, string> headers = null)
			where T : Schema.Schema
		{
			return await CreateMatchMakeRequest<T>("join", roomName, options, headers);
		}

		/// <summary>
		///     Join a <see cref="Room{T}" /> by ID
		/// </summary>
		/// <param name="roomId">ID of the room</param>
		/// <param name="options">Dictionary of options to pass to the room upon joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we join the room</param>
		/// <typeparam name="T">Type of <see cref="Room{T}" /> we want to join</typeparam>
		/// <returns><see cref="Room{T}" /> via async task</returns>
		public async Task<Room<T>> JoinById<T>(string roomId, Dictionary<string, object> options = null, Dictionary<string, string> headers = null)
			where T : Schema.Schema
		{
			return await CreateMatchMakeRequest<T>("joinById", roomId, options, headers);
		}

		/// <summary>
		///     Reconnect to a <see cref="Room{T}" />
		/// </summary>
		/// <param name="reconnectionToken">Previously connected ReconnectionToken</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we reconnect to the room</param>
		/// <typeparam name="T">Type of <see cref="Room{T}" /> we want to reconnect with</typeparam>
		/// <returns><see cref="Room{T}" /> via async task</returns>
		public async Task<Room<T>> Reconnect<T>(ReconnectionToken reconnectionToken, Dictionary<string, string> headers = null)
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
		///     Join or Create a <see cref="Room{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon creation/joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we create/join the room</param>
		/// <returns><see cref="Room{T}" /> via async task</returns>
		public async Task<Room<NoState>> JoinOrCreate(string roomName, Dictionary<string, object> options = null, Dictionary<string, string> headers = null)
		{
			return await CreateMatchMakeRequest<NoState>("joinOrCreate", roomName, options, headers);
		}

		/// <summary>
		///     Create a <see cref="Room{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon creation</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we create the room</param>
		/// <returns><see cref="Room{T}" /> via async task</returns>
		public async Task<Room<NoState>> Create(string roomName, Dictionary<string, object> options = null,
			Dictionary<string, string> headers = null)
		{
			return await CreateMatchMakeRequest<NoState>("create", roomName, options, headers);
		}

		/// <summary>
		///     Join a <see cref="Room{T}" />
		/// </summary>
		/// <param name="roomName">Room identifier</param>
		/// <param name="options">Dictionary of options to pass to the room upon joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we join the room</param>
		/// <returns><see cref="Room{T}" /> via async task</returns>
		public async Task<Room<NoState>> Join(string roomName, Dictionary<string, object> options = null,
			Dictionary<string, string> headers = null)
		{
			return await CreateMatchMakeRequest<NoState>("join", roomName, options, headers);
		}

		/// <summary>
		///     Join a <see cref="Room{T}" /> by ID
		/// </summary>
		/// <param name="roomId">ID of the room</param>
		/// <param name="options">Dictionary of options to pass to the room upon joining</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we join the room</param>
		/// <returns><see cref="Room{T}" /> via async task</returns>
		public async Task<Room<NoState>> JoinById(string roomId, Dictionary<string, object> options = null,
			Dictionary<string, string> headers = null)
		{
			return await CreateMatchMakeRequest<NoState>("joinById", roomId, options, headers);
		}

		/// <summary>
		///     Reconnect to a <see cref="Room{T}" />
		/// </summary>
		/// <param name="roomId">ID of the room</param>
		/// <param name="sessionId">Previously connected sessionId</param>
		/// <param name="headers">Dictionary of headers to pass to the server when we reconnect to the room</param>
		/// <returns><see cref="Room{T}" /> via async task</returns>
		public async Task<Room<NoState>> Reconnect(string roomId, string sessionId,
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
		/// <param name="previousRoom">Previous Room{T} instance to re-establish the server connection: Please do not use this devMode param for general purposes</param>
		/// <typeparam name="T">Type of <see cref="Room{T}" /> we're consuming the seat from</typeparam>
		/// <returns><see cref="Room{T}" /> in which we now have a seat via async task</returns>
		public async Task<Room<T>> ConsumeSeatReservation<T>(SeatReservation response, Dictionary<string, string> headers = null)
			where T : Schema.Schema
		{
			Room<T> room = new Room<T>(response.name)
			{
				RoomId = response.roomId,
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

			room.SetConnection(CreateConnection(response, queryString, headers));

			TaskCompletionSource<Room<T>> tcs = new TaskCompletionSource<Room<T>>();

			void OnError(int code, string message)
			{
				room.OnError -= OnError;
				tcs.SetException(new MatchMakeException(code, message));
			}

			void OnJoin()
			{
				room.OnError -= OnError;
				tcs.TrySetResult(room);
			}

			room.OnError += OnError;
			room.OnJoin += OnJoin;

			_ = room.Connect();

			return await tcs.Task;
		}

		/// <summary>
		///     Create a match making request
		/// </summary>
		/// <param name="method">The type of request we're making (join, create, etc)</param>
		/// <param name="roomName">Room identifierroom we're trying to match</param>
		/// <param name="options">Dictionary of options to use in the match making process</param>
		/// <param name="headers">Dictionary of headers to pass to the server</param>
		/// <typeparam name="T">Type of <see cref="Room{T}" /> we want to match with</typeparam>
		/// <returns><see cref="Room{T}" /> we have matched with via async task</returns>
		/// <exception cref="Exception">Thrown if there is a network related error</exception>
		/// <exception cref="MatchMakeException">Thrown if there is an error in the match making process on the server side</exception>
		protected async Task<Room<T>> CreateMatchMakeRequest<T>(string method, string roomName, Dictionary<string, object> options, Dictionary<string, string> headers)
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

			var response = await Http.Post<SeatReservation>($"matchmake/{method}/{roomName}", options, headers);

			if (response == null)
			{
				throw new Exception($"Error with request: {response}");
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
		protected Connection CreateConnection(
			SeatReservation room,
			Dictionary<string, object> options = null,
			Dictionary<string, string> headers = null
		)
		{
			if (room.protocol != null && room.protocol == "h3") {
				// TODO: support h3 protocol (WebTransport)
				throw new Exception("WebTransport protocol is not supported yet. Please use WebSocket protocol instead.");
			}

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

			return new Connection(uriBuilder.ToString(), headers);
		}
	}
}

