using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameDevWare.Serialization;
using System.Reflection;

namespace Colyseus
{
    public interface IAuthData
	{
        string GetToken();
        object GetUser();
        Type GetUserType();
    }

    [Serializable]
    public class AuthData<T> : IAuthData
	{
        public string token;
        public T user;

        public string GetToken()
		{
            return token;
		}

        public object GetUser()
		{
            return GetUser<object>();
		}

		public T1 GetUser<T1>()
		{
            if (user is T1)
            {
                return (T1)(object)user;
            }
            else if (user is IndexedDictionary<string, object> userDict)
			{
                Type targetType = typeof(T1);
				T1 instance = (T1)Activator.CreateInstance(targetType);

                for (var i = 0; i < userDict.Keys.Count; i++)
                {
                    var field = targetType.GetField(userDict.Keys[i]);
                    if (field != null)
					{
                        try
						{
                            field.SetValue(instance, Convert.ChangeType(userDict.Values[i], field.FieldType));
                        } catch (Exception e)
						{
                            Debug.LogWarning("Colyseus.Auth: cannot convert " + targetType.ToString() + " property '" + field.Name + "' from " + userDict.Values[i].GetType() + " to " + field.FieldType + " (" + e.Message + ")");
						}
                    }
                }
				return instance;
			}
			else
			{
				throw new InvalidCastException($"Cannot convert '{typeof(T1)}' from '{user.GetType()}'");
			}
		}

        public Type GetUserType()
        {
            return typeof(T);
        }
    }

	public delegate void OnAuthDataChange(IAuthData authData);

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
        private List<IColyseusMessageHandler> OnChangeHandlers;

        public Auth(ColyseusClient client)
        {
            _client = client;
        }

        public Action OnChange<T>(Action<T> callback)
		{
            var handler = new ColyseusMessageHandler<T> { Action = callback };

            OnChangeHandlers.Add(handler);

            return () => OnChangeHandlers.Remove(handler);
		}

        public async Task<AuthData<T>> RegisterWithEmailAndPassword<T>(string email, string password, Dictionary<string, object> options = null)
        {
            var response = ConvertAuthData<T>(await _client.Http.Request<AuthData<IndexedDictionary<string, object>>>("POST", $"{PATH}/register", new Dictionary<string, object>
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
            var response = ConvertAuthData<T>(await _client.Http.Request<AuthData<IndexedDictionary<string, object>>>("POST", $"{PATH}/login", new Dictionary<string, object>
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
            var response = ConvertAuthData<T>(await _client.Http.Request<AuthData<IndexedDictionary<string, object>>>("POST", $"{PATH}/anonymous", options));

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
            emitChange(new AuthData<object> { token = null, user = null, });
		}

        private void emitChange(IAuthData authData)
		{
            _client.Http.AuthToken = authData.GetToken();

            if (!string.IsNullOrEmpty(_client.Http.AuthToken))
			{
                PlayerPrefs.SetString(TOKEN_CACHE_KEY, authData.GetToken());
            } else
			{
                PlayerPrefs.DeleteKey(TOKEN_CACHE_KEY);
            }

            OnChangeHandlers.ForEach((handler) =>
            {
                handler.Invoke(authData);
            });
		}

        private AuthData<T> ConvertAuthData<T>(AuthData<IndexedDictionary<string, object>> authData)
		{
            if (authData is AuthData<T>)
			{
                return authData;

			} else
			{
                var user = authData.user;
                Type targetType = typeof(T);
                T instance = (T)Activator.CreateInstance(targetType);

                for (var i = 0; i < user.Keys.Count; i++)
                {
                    var field = targetType.GetField(user.Keys[i]);
                    if (field != null)
                    {
                        try
                        {
                            field.SetValue(instance, Convert.ChangeType(user.Values[i], field.FieldType));
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("Colyseus.Auth: cannot convert " + targetType.ToString() + " property '" + field.Name + "' from " + user.Values[i].GetType() + " to " + field.FieldType + " (" + e.Message + ")");
                        }
                    }
                }
                return new AuthData<T>
                {
                    user = instance,
                    token = authData.token,
                };
            }
        }

    }
}

