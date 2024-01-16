using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameDevWare.Serialization;

namespace Colyseus
{
	public interface IAuthData
	{
		string Token { get; }
		IndexedDictionary<string, object> RawUser { get; set; }
		Type UserType { get; }
	}

	[Serializable]
	public class AuthData<T> : IAuthData
	{
		public string token;
		public T user;

		private IndexedDictionary<string, object> rawUser;

		public AuthData() { }
		public AuthData(string _token, IndexedDictionary<string, object> userData)
		{
			token = _token;
			rawUser = userData;

			if (typeof(T) == typeof(IndexedDictionary<string, object>))
			{
				user = (T)(object)rawUser;
			}
			else if (userData != null)
			{
				user = ConvertType(userData);
			}
		}

		public string Token
		{
			get => token;
		}

		public IndexedDictionary<string, object> RawUser
		{
			get
			{
				// TODO: refactor here...
				if (rawUser == null && typeof(T) == typeof(IndexedDictionary<string, object>))
				{
					rawUser = (IndexedDictionary<string, object>)(object)user;
				}
				return rawUser;
			}
			set => rawUser = value;
		}

		public Type UserType
		{
			get => typeof(T);
		}

		public static T ConvertType(IndexedDictionary<string, object> rawUser)
		{
			Type targetType = typeof(T);
			T instance = (T)Activator.CreateInstance(targetType);

			for (var i = 0; i < rawUser.Keys.Count; i++)
			{
				var field = targetType.GetField(rawUser.Keys[i]);
				if (field != null)
				{
					try
					{
						field.SetValue(instance, Convert.ChangeType(rawUser.Values[i], field.FieldType));
					}
					catch (Exception e)
					{
						Debug.LogWarning("Colyseus.Auth: cannot convert " + targetType.ToString() + " property '" + field.Name + "' from " + rawUser.Values[i].GetType() + " to " + field.FieldType + " (" + e.Message + ")");
					}
				}
			}
			return instance;
		}
	}

	public interface IAuthChangeHandler
	{
		Type Type { get; }
		Type UserType { get; set; }
		void Invoke(object authData);
	}

	public class AuthChangeHandler<T> : IAuthChangeHandler
	{
		private Type userType;
		public Action<T> Action;
		public void Invoke(object authData) { Action.Invoke((T)authData); }
		public Type Type { get => typeof(T); }
		public Type UserType { get => userType; set => userType = value; }
	}

	/// <summary>
	///     Colyseus.Auth
	/// </summary>
	/// <remarks>
	///     Colyseus Authentication Module Tools.
	///     See https://docs.colyseus.io/authentication/module/
	/// </remarks>
	public class Auth
	{
		public static string PATH = "auth";
		public static string TOKEN_CACHE_KEY = "AuthToken";

		private ColyseusClient _client;
		private List<IAuthChangeHandler> OnChangeHandlers = new List<IAuthChangeHandler>();
		private bool initialized = false;

		public Auth(ColyseusClient client)
		{
			_client = client;
			Token = PlayerPrefs.GetString(TOKEN_CACHE_KEY);
		}

		public string Token
		{
			get => _client.Http.AuthToken;
			set => _client.Http.AuthToken = value;
		}

		public async Task<Action> OnChange<T>(Action<AuthData<T>> callback)
		{
			var handler = new AuthChangeHandler<AuthData<T>>
			{
				Action = callback,
				UserType = typeof(T)
			};

			OnChangeHandlers.Add(handler);

			if (!initialized)
			{
				initialized = true;
				try
				{
					emitChange(new AuthData<T> {
						token = Token,
						user = await GetUserData<T>()
					});
				} catch (Exception _)
				{
					emitChange(new AuthData<object> { user = null, token = null });
				}
			}

			return () => OnChangeHandlers.Remove(handler);
		}

		public async Task<T> GetUserData<T>()
		{
			if (string.IsNullOrEmpty(Token))
			{
				throw new Exception("missing Auth.Token");
			}
			else
			{
				return getAuthData<T>(await _client.Http.Request<AuthData<IndexedDictionary<string, object>>>("GET", $"{PATH}/userdata")).user;
			}
		}

		public async Task<AuthData<T>> RegisterWithEmailAndPassword<T>(string email, string password, Dictionary<string, object> options = null)
		{
			var response = getAuthData<T>(await _client.Http.Request<AuthData<IndexedDictionary<string, object>>>("POST", $"{PATH}/register", new Dictionary<string, object>
			{
				{ "email", email },
				{ "password", password },
				{ "options", options },
			}));

			emitChange(response);

			return response;
		}

		public async Task<IAuthData> RegisterWithEmailAndPassword(string email, string password, Dictionary<string, object> options = null)
		{
			return await RegisterWithEmailAndPassword<IndexedDictionary<string, object>>(email, password, options);
		}

		public async Task<AuthData<T>> SignInWithEmailAndPassword<T>(string email, string password)
		{
			var response = getAuthData<T>(await _client.Http.Request<AuthData<IndexedDictionary<string, object>>>("POST", $"{PATH}/login", new Dictionary<string, object>
			{
				{ "email", email },
				{ "password", password },
			}));

			emitChange(response);

			return response;
		}

		public async Task<IAuthData> SignInWithEmailAndPassword(string email, string password)
		{
			return await SignInWithEmailAndPassword<IndexedDictionary<string, object>>(email, password);
		}

		public async Task<AuthData<T>> SignInAnonymously<T>(Dictionary<string, object> options = null)
		{
			var response = getAuthData<T>(await _client.Http.Request<AuthData<IndexedDictionary<string, object>>>("POST", $"{PATH}/anonymous", options));

			emitChange(response);

			return response;
		}

		public async Task<IAuthData> SignInAnonymously(Dictionary<string, object> options = null)
		{
			return await SignInAnonymously<IndexedDictionary<string, object>>(options);
		}

		public async Task<AuthData<T>> SignInWithProvider<T>(string providerName, Dictionary<string, object> settings = null)
		{
			await Task.Run(() => {/* Satisfy the compiler async/await. This method is not implemented yet. */});

			//
			// Implementation reference: https://github.com/colyseus/colyseus.js/blob/1f2208d4ff49e858a737e4e7d1581148de196cce/src/Auth.ts#L112C26-L161
			//
			throw new Exception("Not implemented. See implementation reference on JavaScript SDK");
		}
		public async Task<IAuthData> SignInWithProvider(string providerName, Dictionary<string, object> settings = null)
		{
			return await SignInWithProvider<IndexedDictionary<string, object>>(providerName, settings);
		}

		public async Task<string> SendResetPasswordEmail(string email, string password)
		{
			return await _client.Http.Request("POST", $"{PATH}/login", new Dictionary<string, object>
			{
				{ "email", email },
				{ "password", password },
			});
		}

		public void SignOut()
		{
			emitChange(new AuthData<object> { token = null, user = null});
		}

		private void emitChange(IAuthData authData)
		{
			Token = authData.Token;

			if (!string.IsNullOrEmpty(Token))
			{
				PlayerPrefs.SetString(TOKEN_CACHE_KEY, authData.Token);
			}
			else
			{
				PlayerPrefs.DeleteKey(TOKEN_CACHE_KEY);
			}

			OnChangeHandlers.ForEach((handler) =>
			{
				if (authData.GetType() == handler.Type)
				{
					handler.Invoke(authData);
				}
				else if (authData.UserType == typeof(IndexedDictionary<string, object>))
				{
					// convert AuthData<handler.UserType>
					object instance = Activator.CreateInstance(handler.Type, authData.Token, authData.RawUser);
					handler.Invoke(instance);
				}
				else if (authData.RawUser == null)
				{
					object instance = Activator.CreateInstance(handler.Type, authData.Token, null);
					handler.Invoke(instance);
				}
				else
				{
					Debug.Log("Not triggering...");
				}
			});
		}

		private AuthData<T> getAuthData<T>(AuthData<IndexedDictionary<string, object>> authData)
		{
			if (typeof(T) == typeof(IndexedDictionary<string, object>))
			{
				return (AuthData<T>)(object)authData;
			}
			else
			{
				return new AuthData<T>(authData.token, authData.RawUser);
			}
		}

	}
}

