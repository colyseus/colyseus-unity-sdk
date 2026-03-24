#if !UNITY_EDITOR

using System;
using System.Threading;
using NUnit.Framework;

namespace Colyseus.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class WebSocketTransportTest
    {
        private Action<NativeWebSocket.WebSocket> _previousRegister;
        private Action<NativeWebSocket.WebSocket> _previousUnregister;

        [SetUp]
        public void SetUp()
        {
            _previousRegister = ColyseusContext.RegisterWebSocketForDispatch;
            _previousUnregister = ColyseusContext.UnregisterWebSocketForDispatch;

            ColyseusContext.RegisterWebSocketForDispatch = null;
            ColyseusContext.UnregisterWebSocketForDispatch = null;
        }

        [TearDown]
        public void TearDown()
        {
            ColyseusContext.RegisterWebSocketForDispatch = _previousRegister;
            ColyseusContext.UnregisterWebSocketForDispatch = _previousUnregister;
        }

        [Test]
        public void UsesSharedDispatchLoopWithoutContextOrExternalDispatcher()
        {
            Assert.IsTrue(WebSocketTransport.ShouldUseSharedDispatchLoop(null));
        }

        [Test]
        public void SkipsSharedDispatchLoopWhenSynchronizationContextExists()
        {
            Assert.IsFalse(WebSocketTransport.ShouldUseSharedDispatchLoop(new SynchronizationContext()));
        }

        [Test]
        public void SkipsSharedDispatchLoopWhenExternalDispatcherExists()
        {
            ColyseusContext.RegisterWebSocketForDispatch = _ => { };

            Assert.IsFalse(WebSocketTransport.ShouldUseSharedDispatchLoop(null));
        }
    }
}

#endif