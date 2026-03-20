using System.Collections.Generic;

namespace Colyseus
{
    public class InMemoryTokenStorage : ITokenStorage
    {
        private Dictionary<string, string> _tokens = new Dictionary<string, string>();

        public string GetToken(string key)
        {
            _tokens.TryGetValue(key, out var value);
            return value ?? string.Empty;
        }

        public void SetToken(string key, string value)
        {
            _tokens[key] = value;
        }

        public void DeleteToken(string key)
        {
            _tokens.Remove(key);
        }
    }
}
