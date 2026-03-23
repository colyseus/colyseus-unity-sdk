using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// Class for building out server requests
    /// </summary>
    public class HTTP
    {
        public string AuthToken;

        private Settings _settings;

        public HTTP(Settings settings)
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
            byte[] body = null;
            if (jsonBody != null)
            {
                MemoryStream jsonBodyStream = new MemoryStream();
                Json.Serialize(jsonBody, jsonBodyStream);
                body = jsonBodyStream.ToArray();
            }

            var allHeaders = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> pair in _settings.Headers)
            {
                allHeaders[pair.Key] = pair.Value;
            }

            if (!string.IsNullOrEmpty(AuthToken))
            {
                allHeaders["Authorization"] = "Bearer " + AuthToken;
            }

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    allHeaders[header.Key] = header.Value;
                }
            }

            return await ColyseusContext.HttpClient.Request(uriMethod, GetRequestURL(uriPath), body, allHeaders);
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
