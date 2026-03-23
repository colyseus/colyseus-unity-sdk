using System.Collections.Generic;
using System.Threading.Tasks;

namespace Colyseus
{
    public interface IHttpClient
    {
        Task<string> Request(string method, string url, byte[] body, Dictionary<string, string> headers);
    }
}
