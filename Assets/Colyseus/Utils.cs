using System.Collections.Generic;
using GameDevWare.Serialization;

namespace Colyseus
{
    public static class Utils
    {
        public static Dictionary<string, object> ConvertIndexedDictionary(IndexedDictionary<string, object> dic)
        {
            var newDic = new Dictionary<string, object>();
            foreach (var keyValue in dic)
            {
                if (keyValue.Value.GetType() == typeof(IndexedDictionary<string, object>))
                    newDic.Add(keyValue.Key, ConvertIndexedDictionary((IndexedDictionary<string, object>)keyValue.Value));
                else
                    newDic.Add(keyValue.Key, keyValue.Value);
            }

            return newDic;
        }
    }
}