using System;
using System.Text;
using System.Collections.Specialized;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

#if NET_LEGACY
using System.Web;
#endif

namespace Colyseus
{
	[Serializable]
	public class DeviceData
	{
		public string id = null;
		public string platform = null;
	}

	[Serializable]
	public class UserData
	{
		public string token = null;

		public string _id = null;
		public string username = null;
		public string displayName = null;
		public string avatarUrl = null;

		public bool isAnonymous = true;
		public string email = null;

		public string lang = null;
		public string location = null;
		public string timezone = null;
		public object metadata = null;

		public DeviceData[] devices = null;

		public string facebookId = null;
		public string twitterId = null;
		public string googleId = null;
		public string gameCenterId = null;
		public string steamId = null;

		public string[] friendIds = null;
		public string[] blockedUserIds = null;

		public DateTime createdAt = DateTime.MinValue;
		public DateTime updatedAt = DateTime.MinValue;
	}

	[Serializable]
	public class UserDataCollection
	{
		public UserData[] users;
	}

	[Serializable]
	public class StatusData
	{
		public bool Status = false;
	}

	public class Auth
	{
		protected Uri Endpoint;

		public string Token;

		public string _id;
		public string Username;
		public string DisplayName;
		public string AvatarUrl;

		public bool IsAnonymous;
		public string Email;

		public string Lang;
		public string Location;
		public string Timezone;
		public object Metadata;

		public DeviceData[] Devices;

		public string FacebookId;
		public string TwitterId;
		public string GoogleId;
		public string GameCenterId;
		public string SteamId;

		public string[] FriendIds;
		public string[] BlockedUserIds;

		public Auth(Uri endpoint)
		{
			Endpoint = endpoint;
			Token = PlayerPrefs.GetString("Token", string.Empty);
		}

		public bool HasToken
		{
			get { return !string.IsNullOrEmpty(Token); }
		}

		public async Task<Auth> Login(/* anonymous */)
		{
			return await Login(HttpUtility.ParseQueryString(string.Empty));
		}

		public async Task<Auth> Login(string facebookAccessToken)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["accessToken"] = facebookAccessToken;
			return await Login(query);
		}

		public async Task<Auth> Login(string email, string password)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["email"] = email;
			query["password"] = password;
			return await Login(query);
		}

		public async Task<Auth> Login(NameValueCollection queryParams)
		{
			queryParams["deviceId"] = GetDeviceId();
			queryParams["platform"] = GetPlatform();

			var userData = await Request<UserData>("POST", "/auth", queryParams);

			_id = userData._id;
			Username = userData.username;
			DisplayName = userData.displayName;
			AvatarUrl = userData.avatarUrl;

			IsAnonymous = userData.isAnonymous;
			Email = userData.email;

			Lang = userData.lang;
			Location = userData.location;
			Timezone = userData.timezone;
			Metadata = userData.metadata;

			Devices = userData.devices;

			FacebookId = userData.facebookId;
			TwitterId = userData.twitterId;
			GoogleId = userData.googleId;
			GameCenterId = userData.gameCenterId;
			SteamId = userData.steamId;

			FriendIds = userData.friendIds;
			BlockedUserIds = userData.blockedUserIds;

			Token = userData.token;
			PlayerPrefs.SetString("Token", Token);

			return this;
		}

		public async Task<Auth> Save()
		{
			UserData uploadData = new UserData();
			if (!string.IsNullOrEmpty(Username)) uploadData.username = Username;
			if (!string.IsNullOrEmpty(DisplayName)) uploadData.displayName = DisplayName;
			if (!string.IsNullOrEmpty(AvatarUrl)) uploadData.avatarUrl = AvatarUrl;
			if (!string.IsNullOrEmpty(Lang)) uploadData.lang = Lang;
			if (!string.IsNullOrEmpty(Location)) uploadData.location = Location;
			if (!string.IsNullOrEmpty(Timezone)) uploadData.timezone = Timezone;

			var bodyString = JsonUtility.ToJson(uploadData);
			await Request<UserData>("PUT", "/auth", null, new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyString)));

			return this;
		}

		public async Task<UserDataCollection> GetFriends()
		{
			return await Request<UserDataCollection>("GET", "/friends/all");
		}

		public async Task<UserDataCollection> GetOnlineFriends()
		{
			return await Request<UserDataCollection>("GET", "/friends/online");
		}

		public async Task<UserDataCollection> GetFriendRequests()
		{
			return await Request<UserDataCollection>("GET", "/friends/requests");
		}

		public async Task<StatusData> SendFriendRequest(string friendId)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["userId"] = friendId;
			return await Request<StatusData>("POST", "/friends/requests", query);
		}

		public async Task<StatusData> AcceptFriendRequest(string friendId)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["userId"] = friendId;
			return await Request<StatusData>("PUT", "/friends/requests", query);
		}

		public async Task<StatusData> DeclineFriendRequest(string friendId)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["userId"] = friendId;
			return await Request<StatusData>("DELETE", "/friends/requests", query);
		}

		public async Task<StatusData> BlockUser(string friendId)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["userId"] = friendId;
			return await Request<StatusData>("POST", "/friends/block", query);
		}

		public async Task<StatusData> UnblockUser(string friendId)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["userId"] = friendId;
			return await Request<StatusData>("PUT", "/friends/block", query);
		}

		public void Logout()
		{
			Token = string.Empty;
			PlayerPrefs.SetString("Token", Token);
		}

		protected async Task<T> Request<T>(string method, string segments, NameValueCollection query = null, UploadHandlerRaw data = null)
		{
			if (query == null)
			{
				query = HttpUtility.ParseQueryString(string.Empty);
			}

			// Append auth token, if it exists
			if (HasToken) query["token"] = Token;

			var uriBuilder = new UriBuilder(Endpoint);
			uriBuilder.Path = segments;
			uriBuilder.Query = query.ToString();

			var req = new UnityWebRequest();
			req.method = method;

			// FIXME: replacing "ws" with "http" is too hacky!
			uriBuilder.Scheme = uriBuilder.Scheme.Replace("ws", "http");

			req.url = uriBuilder.Uri.ToString();

			// Send JSON on request body
			if (data != null)
			{
				req.uploadHandler = data;
				req.SetRequestHeader("Content-Type", "application/json");
			}

			// Request headers
			req.SetRequestHeader("Accept", "application/json");
			if (HasToken) req.SetRequestHeader("Authorization", "Bearer " + Token);

			// req.uploadHandler = new UploadHandlerRaw(bytes);
			req.downloadHandler = new DownloadHandlerBuffer();
			await req.SendWebRequest();

			if (req.isNetworkError || req.isHttpError)
			{
				throw new Exception(req.error);
			}

			var json = req.downloadHandler.text;

			// Workaround for decoding a UserDataCollection
			if (json.StartsWith("[", StringComparison.CurrentCulture))
			{
				json = "{\"users\": " + json + "}";
			}

			return JsonUtility.FromJson<T>(json);
		}

		protected string GetDeviceId()
		{
			// TODO: Create a random id and assign it to PlayerPrefs for WebGL
			// #if UNITY_WEBGL
			return SystemInfo.deviceUniqueIdentifier;
		}

		protected string GetPlatform()
		{
#if UNITY_EDITOR
			return "unity_editor";
#elif UNITY_STANDALONE_OSX
		return "osx";
#elif UNITY_STANDALONE_WIN
		return "windows";
#elif UNITY_STANDALONE_LINUX
		return "linux";
#elif UNITY_WII
		return "wii";
#elif UNITY_IOS
		return "ios";
#elif UNITY_ANDROID
		return "android";
#elif UNITY_PS4
		return "ps2";
#elif UNITY_XBOXONE
		return "xboxone";
#elif UNITY_TIZEN
		return "tizen";
#elif UNITY_TVOS
		return "tvos";
#elif UNITY_WSA || UNITY_WSA_10_0
		return "wsa";
#elif UNITY_WINRT || UNITY_WINRT_10_0
		return "winrt";
#elif UNITY_WEBGL
		return "html5";
#elif UNITY_FACEBOOK
		return "facebook";
#elif UNITY_ADS
		return "unity_ads";
#elif UNITY_ANALYTICS
		return "unity_analytics";
#elif UNITY_ASSERTIONS
		return "unity_assertions";
#endif
		}
	}

}
