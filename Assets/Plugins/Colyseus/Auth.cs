using System;
using System.Web;
using System.Text;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

using GameDevWare.Serialization;

namespace Colyseus
{
	[DataContract]
	public class DeviceData
	{
		[DataMember(Name = "id")] public string Id = null;
		[DataMember(Name = "platform")] public string Platform = null;
	}

	[DataContract]
	public class UserData
	{
		[DataMember(Name = "token")] public string Token = null;

		[DataMember(Name = "_id")] public string _id = null;
		[DataMember(Name = "username")] public string Username = null;
		[DataMember(Name = "displayName")] public string DisplayName = null;
		[DataMember(Name = "avatarUrl")] public string AvatarUrl = null;

		[DataMember(Name = "isAnonymous")] public bool IsAnonymous = true;
		[DataMember(Name = "email")] public string Email = null;

		[DataMember(Name = "lang")] public string Lang = null;
		[DataMember(Name = "location")] public string Location = null;
		[DataMember(Name = "timezone")] public string Timezone = null;
		[DataMember(Name = "metadata")] public object Metadata = null;

		[DataMember(Name = "devices")] public DeviceData[] Devices = null;

		[DataMember(Name = "facebookId")] public string FacebookId = null;
		[DataMember(Name = "twitterId")] public string TwitterId = null;
		[DataMember(Name = "googleId")] public string GoogleId = null;
		[DataMember(Name = "gameCenterId")] public string GameCenterId = null;
		[DataMember(Name = "steamId")] public string SteamId = null;

		[DataMember(Name = "friendIds")] public string[] FriendIds = null;
		[DataMember(Name = "blockedUserIds")] public string[] BlockedUserIds = null;

		[DataMember(Name = "createdAt")] public DateTime CreatedAt = DateTime.MinValue;
		[DataMember(Name = "updatedAt")] public DateTime UpdatedAt = DateTime.MinValue;
	}

	[DataContract]
	public class StatusData
	{
		[DataMember(Name = "status")] public bool Status = false;
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
			Username = userData.Username;
			DisplayName = userData.DisplayName;
			AvatarUrl = userData.AvatarUrl;

			IsAnonymous = userData.IsAnonymous;
			Email = userData.Email;

			Lang = userData.Lang;
			Location = userData.Location;
			Timezone = userData.Timezone;
			Metadata = userData.Metadata;

			Devices = userData.Devices;

			FacebookId = userData.FacebookId;
			TwitterId = userData.TwitterId;
			GoogleId = userData.GoogleId;
			GameCenterId = userData.GameCenterId;
			SteamId = userData.SteamId;

			FriendIds = userData.FriendIds;
			BlockedUserIds = userData.BlockedUserIds;

			Token = userData.Token;
			PlayerPrefs.SetString("Token", Token);

			return this;
		}

		public async Task<Auth> Save()
		{
			var uploadData = new IndexedDictionary<string, string>();
			if (!string.IsNullOrEmpty(Username)) uploadData["username"] = Username;
			if (!string.IsNullOrEmpty(DisplayName)) uploadData["displayName"] = DisplayName;
			if (!string.IsNullOrEmpty(AvatarUrl)) uploadData["avatarUrl"] = AvatarUrl;
			if (!string.IsNullOrEmpty(Lang)) uploadData["lang"] = Lang;
			if (!string.IsNullOrEmpty(Location)) uploadData["location"] = Location;
			if (!string.IsNullOrEmpty(Timezone)) uploadData["timezone"] = Timezone;

			var bodyString = Json.SerializeToString(uploadData);
			await Request<UserData>("PUT", "/auth", null, new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyString)));

			return this;
		}

		public async Task<UserData[]> GetFriends()
		{
			return await Request<UserData[]>("GET", "/friends/all");
		}

		public async Task<UserData[]> GetOnlineFriends()
		{
			return await Request<UserData[]>("GET", "/friends/online");
		}

		public async Task<UserData[]> GetFriendRequests()
		{
			return await Request<UserData[]>("GET", "/friends/requests");
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
			req.url = uriBuilder.Uri.ToString().Replace("ws", "http");

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

			return Json.Deserialize<T>(req.downloadHandler.text);
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
