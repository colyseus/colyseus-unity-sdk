using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using GameDevWare.Serialization;

namespace Colyseus
{
    [Serializable]
    public class ErrorResponse
	{
        public string error;
	}

    /// <summary>
    /// Class for building out server requests using <see cref="UnityWebRequest"/>
    /// </summary>
    public class HTTP
    {
        public string AuthToken;

        private ColyseusSettings _settings;

        public HTTP(ColyseusSettings settings)
        {
            _settings = settings;
        }

        public async Task<string> Get(string uriPath, Dictionary<string, string> headers = null)
        {
            return await Request("GET", uriPath, null, headers);
        }

        public async Task<T> Get<T>(string uriPath, Dictionary<string, string> headers = null)
        {
            return await Request<T>("GET", uriPath, null, headers);
        }

        public async Task<string> Post(string uriPath, Dictionary<string, object> jsonBody = null, Dictionary<string, string> headers = null)
		{
            return await Request("POST", uriPath, jsonBody, headers);
		}

        public async Task<T> Post<T>(string uriPath, Dictionary<string, object> jsonBody = null, Dictionary<string, string> headers = null)
        {
            return await Request<T>("POST", uriPath, jsonBody, headers);
        }

        public async Task<string> Delete(string uriPath, Dictionary<string, object> jsonBody = null, Dictionary<string, string> headers = null)
        {
            return await Request("DELETE", uriPath, jsonBody, headers);
        }

        public async Task<T> Delete<T>(string uriPath, Dictionary<string, object> jsonBody = null, Dictionary<string, string> headers = null)
        {
            return await Request<T>("DELETE", uriPath, jsonBody, headers);
        }

        public async Task<string> Put(string uriPath, Dictionary<string, object> jsonBody = null, Dictionary<string, string> headers = null)
        {
            return await Request("PUT", uriPath, jsonBody, headers);
        }

        public async Task<T> Put<T>(string uriPath, Dictionary<string, object> jsonBody = null, Dictionary<string, string> headers = null)
        {
            return await Request<T>("PUT", uriPath, jsonBody, headers);
        }

        public async Task<T> Request<T>(string uriMethod, string uriPath, Dictionary<string, object> jsonBody = null, Dictionary<string, string> headers = null)
		{
            return Json.Deserialize<T>(await Request(uriMethod, uriPath, jsonBody, headers));
        }

        public async Task<string> Request(string uriMethod, string uriPath, Dictionary<string, object> jsonBody = null, Dictionary<string, string> headers = null)
        {
            using (UnityWebRequest req = new UnityWebRequest())
            {
                req.method = uriMethod;
                req.url = GetRequestURL(uriPath);
                //Debug.Log($"Requesting from URL: {req.url}");

                // Send JSON on request body
                if (jsonBody != null)
                {
                    MemoryStream jsonBodyStream = new MemoryStream();
                    Json.Serialize(jsonBody, jsonBodyStream); //TODO: Replace GameDevWare serialization

                    req.uploadHandler = new UploadHandlerRaw(jsonBodyStream.ToArray())
                    {
                        contentType = "application/json"
                    };
                }

                foreach (KeyValuePair<string, string> pair in _settings.Headers)
                {
                    req.SetRequestHeader(pair.Key, pair.Value);
                }

                if (!string.IsNullOrEmpty(AuthToken))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + AuthToken);
                }

                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        req.SetRequestHeader(header.Key, header.Value);
                    }
                }

                req.downloadHandler = new DownloadHandlerBuffer();
                await req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    if (_settings.useSecureProtocol)
                    {
                        //We failed to make this call with a secure protocol, try with non-secure and if that works we'll stick with it
                        _settings.useSecureProtocol = false;
                        Debug.LogError($"Failed to make request to {req.url} with secure protocol, trying again without!");
                        return await Request(uriMethod, uriPath, jsonBody, headers);
                    }
                    else
                    {
                        var errorMessage = req.error;

                        //
                        // Parse JSON from response
                        //
                        if (!string.IsNullOrEmpty(req.downloadHandler.text))
						{
                            var data = Json.Deserialize<ErrorResponse>(req.downloadHandler.text);
                            if (!string.IsNullOrEmpty(data.error))
							{
                                errorMessage = data.error;
                                throw new HttpException((int)req.responseCode, errorMessage);
                            }
						}

                        throw new HttpException((int)req.responseCode, errorMessage);
                    }
                }

                return req.downloadHandler.text;
            };
        }

        public string GetRequestURL(string pathWithQueryString)
        {
            var splittedPath = pathWithQueryString.Split('?');
            var path = splittedPath[0];
            var query = (splittedPath.Length > 1) ? splittedPath[1] : "";

            string forwardSlash = "";
            if (!_settings.WebRequestEndpoint.EndsWith("/"))
            {
                forwardSlash = "/";
            }

            // WebRequestEndpoint will include any path that is included with the server address field of the server settings object so we need to add the request specific path to the WebRequestEndpoint value
            UriBuilder uriBuilder = new UriBuilder($"{_settings.WebRequestEndpoint}{forwardSlash}{path}");

            uriBuilder.Port = _settings.GetPort();

            if (!string.IsNullOrEmpty(query))
			{
                uriBuilder.Query = query;
            }

            return uriBuilder.ToString();
        }
    }
}
