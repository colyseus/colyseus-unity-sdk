#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace Colyseus
{
    /// <summary>
    /// Auto-initializes ColyseusContext with Unity-specific implementations.
    /// This runs before any scene loads, ensuring all Unity users get the correct platform bindings.
    /// </summary>
    public static class UnityPlatform
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            ColyseusContext.Logger = new UnityLogger();
            ColyseusContext.HttpClient = new UnityHttpClient();
            ColyseusContext.TokenStorage = new UnityTokenStorage();
        }
    }
}
#endif
