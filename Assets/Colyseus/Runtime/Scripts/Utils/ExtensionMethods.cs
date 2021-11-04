using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace Colyseus
{
    /// <summary>
    ///     Provides extension for Unity specific methods
    /// </summary>
    public static class ColyseusExtensionMethods
    {
        /// <summary>
        ///     Returns our custom <see cref="ColyseusUnityWebRequestAwaiter" /> instead of a standard
        ///     <see cref="UnityWebRequestAsyncOperation" />
        /// </summary>
        /// <returns>An instance of <see cref="ColyseusUnityWebRequestAwaiter" /></returns>
        public static ColyseusUnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            return new ColyseusUnityWebRequestAwaiter(asyncOp);
        }
    }
}