#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GameDevWare.Serialization;

namespace Colyseus
{
    public class UnityHttpClient : IHttpClient
    {
        public async Task<string> Request(string method, string url, byte[] body, Dictionary<string, string> headers)
        {
            using (UnityWebRequest req = new UnityWebRequest())
            {
                req.method = method;
                req.url = url;

                if (body != null)
                {
                    req.uploadHandler = new UploadHandlerRaw(body)
                    {
                        contentType = "application/json"
                    };
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
                    var errorMessage = req.error;

                    if (!string.IsNullOrEmpty(req.downloadHandler.text))
                    {
                        try
                        {
                            var data = Json.Deserialize<ErrorResponse>(req.downloadHandler.text);
                            if (!string.IsNullOrEmpty(data.error))
                            {
                                errorMessage = data.error;
                            }
                        }
                        catch { }
                    }

                    throw new HttpException((int)req.responseCode, errorMessage);
                }

                return req.downloadHandler.text;
            }
        }
    }
}
#endif
