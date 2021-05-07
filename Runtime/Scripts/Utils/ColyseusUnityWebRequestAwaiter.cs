using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace Colyseus
{
    /// <summary>
    ///     A custom class that awaits the completion of a <see cref="UnityWebRequestAsyncOperation" /> and then performs an
    ///     <see cref="Action" /> upon completion
    /// </summary>
    public class ColyseusUnityWebRequestAwaiter : INotifyCompletion
    {
        private readonly UnityWebRequestAsyncOperation _asyncOp;
        private Action _continuation;

        public ColyseusUnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp)
        {
            _asyncOp = asyncOp;
            asyncOp.completed += OnRequestCompleted;
        }

        /// <summary>
        ///     Public getter to determine if the <see cref="UnityWebRequestAsyncOperation" /> is completed
        /// </summary>
        public bool IsCompleted
        {
            get { return _asyncOp.isDone; }
        }

        /// <summary>
        ///     Provide an action to be invoked when the <see cref="UnityWebRequestAsyncOperation" /> s completed
        /// </summary>
        /// <param name="continuation">The action to perform when the request is completed</param>
        public void OnCompleted(Action continuation)
        {
            _continuation = continuation;
        }

        /// <summary>
        ///     Satifies the <see cref="INotifyCompletion" /> requirement, but currently unused
        /// </summary>
        public void GetResult()
        {
        }

        private void OnRequestCompleted(AsyncOperation obj)
        {
            _continuation?.Invoke();
        }
    }
}