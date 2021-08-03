using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using GameDevWare.Serialization;
using LucidSightTools;

namespace Colyseus
{
    /// <summary>
    /// Class for building out server requests using <see cref="UnityWebRequest"/>
    /// </summary>
    public class ColyseusRequest
    {
        private ColyseusSettings _serverSettings;

        public ColyseusRequest(ColyseusSettings settings)
        {
            _serverSettings = settings;
        }

        public async Task<string> Request(string uriMethod, string uriPath, string uriQuery, string Token = "", UploadHandlerRaw data = null)
        {
            UriBuilder uriBuilder = new UriBuilder(_serverSettings.WebRequestEndpoint);
            uriBuilder.Path = uriPath;
            uriBuilder.Query = uriQuery;

            using (UnityWebRequest req = new UnityWebRequest())
            {
                req.method = uriMethod;
        
                req.url = uriBuilder.Uri.ToString();
                // Send JSON on request body
                if (data != null)
                {
                    req.uploadHandler = data;
                }
        
                foreach (KeyValuePair<string, string> pair in _serverSettings.HeadersDictionary)
                {
                    req.SetRequestHeader(pair.Key, pair.Value);
                }
        
                if (!string.IsNullOrEmpty(Token))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + Token);
                }
        
                // req.uploadHandler = new UploadHandlerRaw(bytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                await req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
                {
                    if (_serverSettings.useSecureProtocol)
                    {
                         //We failed to make this call with a secure protocol, try with non-secure and if that works we'll stick with it
                         _serverSettings.useSecureProtocol = false;
                         LSLog.LogError($"Failed to make request to {req.url} with secure protocol, trying again without!");
                         return await Request(uriMethod, uriPath, uriQuery, Token, data);
                    }
                    else
                    {
                        throw new Exception(req.error);
                    }
                }
            
                string json = req.downloadHandler.text;
            
                return json;
            } ;
        }

        public async Task<string> Request(string uriMethod, string uriPath, Dictionary<string, object> options = null, Dictionary<string, string> headers = null)
        {
            UriBuilder uriBuilder = new UriBuilder(_serverSettings.WebRequestEndpoint);
            uriBuilder.Path = uriPath;

            using (UnityWebRequest req = new UnityWebRequest())
            {
                req.method = uriMethod;
                req.url = uriBuilder.Uri.ToString();
                LSLog.Log($"Requesting from URL: {req.url}");
                if (options != null)
                {
                    // Send JSON options on request body
                    MemoryStream jsonBodyStream = new MemoryStream();
                    Json.Serialize(options, jsonBodyStream); //TODO: Replace GameDevWare serialization
                    
                    req.uploadHandler = new UploadHandlerRaw(jsonBodyStream.ToArray())
                    {
                        contentType = "application/json"
                    };
                }
                
                foreach (KeyValuePair<string, string> pair in _serverSettings.HeadersDictionary)
                {
                    req.SetRequestHeader(pair.Key, pair.Value);
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
                    if (_serverSettings.useSecureProtocol)
                    {
                        //We failed to make this call with a secure protocol, try with non-secure and if that works we'll stick with it
                        _serverSettings.useSecureProtocol = false;
                        LSLog.LogError($"Failed to make request to {req.url} with secure protocol, trying again without!");
                        return await Request(uriMethod, uriPath, options, headers);
                    }
                    else
                    {
                        throw new Exception(req.error);
                    }
                }
                
                return req.downloadHandler.text;
            };
        }
    }
}