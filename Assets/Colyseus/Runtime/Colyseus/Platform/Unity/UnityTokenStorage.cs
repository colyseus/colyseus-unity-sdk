#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace Colyseus
{
    public class UnityTokenStorage : ITokenStorage
    {
        public string GetToken(string key)
        {
            return PlayerPrefs.GetString(key);
        }

        public void SetToken(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public void DeleteToken(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }
    }
}
#endif
