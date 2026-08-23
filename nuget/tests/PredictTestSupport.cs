using System;
using System.Collections.Generic;
using Colyseus;
using Colyseus.Predict;
using Colyseus.Schema;

namespace Colyseus.Tests
{
	/// <summary>Shared Predict-layer fixtures: a stub transport, a driven-by-hand callbacks face, a capturing logger.</summary>
	internal static class PredictTestSupport
	{
		public class StubConnection : Connection
		{
			public StubConnection() : base("ws://localhost", null)
			{
				IsOpen = true;
				Transmit = _ => System.Threading.Tasks.Task.CompletedTask;
			}
		}

		public static InputHandle MakeHandle(Schema.Schema input)
		{
			var encoder = new InputEncoder(input);
			var stub = new StubConnection();
			return new InputHandle(input, encoder, false, false, 0, null,
				null, null, null, () => stub, () => null);
		}

		/// <summary>
		///     Callbacks face the test drives by hand — no decoder bytes. Listeners
		///     key on (refId, field); add/remove handlers on the collection name.
		/// </summary>
		public class FakeCallbacks : IPredictCallbacks
		{
			private readonly Dictionary<(int, string), Action<object>> listeners = new Dictionary<(int, string), Action<object>>();
			private readonly Dictionary<string, List<Action<Schema.Schema, object>>> adds = new Dictionary<string, List<Action<Schema.Schema, object>>>();
			private readonly Dictionary<string, List<Action<Schema.Schema, object>>> removes = new Dictionary<string, List<Action<Schema.Schema, object>>>();
			/// <summary>Children already in a collection, replayed to an immediate OnAdd.</summary>
			public readonly Dictionary<string, List<(Schema.Schema child, object key)>> Existing = new Dictionary<string, List<(Schema.Schema, object)>>();
			public int ListenerCount => listeners.Count;

			public Action Listen(Schema.Schema instance, string field, Action<object> handler, bool immediate)
			{
				var key = (instance.__refId, field);
				listeners[key] = handler;
				if (immediate) { handler(instance[field]); }
				return () => listeners.Remove(key);
			}

			public Action OnAdd(string collection, Action<Schema.Schema, object> handler, bool immediate)
			{
				if (!adds.TryGetValue(collection, out var list)) { adds[collection] = list = new List<Action<Schema.Schema, object>>(); }
				list.Add(handler);
				if (immediate && Existing.TryGetValue(collection, out var existing))
				{
					foreach (var (child, key) in existing) { handler(child, key); }
				}
				return () => list.Remove(handler);
			}

			public Action OnRemove(string collection, Action<Schema.Schema, object> handler)
			{
				if (!removes.TryGetValue(collection, out var list)) { removes[collection] = list = new List<Action<Schema.Schema, object>>(); }
				list.Add(handler);
				return () => list.Remove(handler);
			}

			public void Push(Schema.Schema instance, string field, object value)
			{
				instance[field] = value;
				if (listeners.TryGetValue((instance.__refId, field), out var handler)) { handler(value); }
			}

			public void Add(string collection, Schema.Schema child, object key)
			{
				if (adds.TryGetValue(collection, out var list))
				{
					foreach (var handler in list.ToArray()) { handler(child, key); }
				}
			}

			public void Remove(string collection, Schema.Schema child, object key)
			{
				if (removes.TryGetValue(collection, out var list))
				{
					foreach (var handler in list.ToArray()) { handler(child, key); }
				}
			}
		}

		/// <summary>A Predict over a hand-driven callbacks face and a fresh clock.</summary>
		public static (FakeCallbacks cb, RoomClock clock, Colyseus.Predict.Predict predict) MakePredict(PredictGetOptions opts = null, Func<string> sessionId = null)
		{
			var cb = new FakeCallbacks();
			var clock = new RoomClock();
			return (cb, clock, new Colyseus.Predict.Predict(cb, clock, opts, sessionId));
		}

		public class CapturingLogger : ILogger
		{
			public readonly List<string> Warnings = new List<string>();
			public readonly List<string> Logs = new List<string>();
			public readonly List<string> Errors = new List<string>();
			public void Log(string message) => Logs.Add(message);
			public void LogWarning(string message) => Warnings.Add(message);
			public void LogError(string message) => Errors.Add(message);
		}

		/// <summary>Swap the SDK logger for the scope; restore on dispose.</summary>
		public static IDisposable CaptureLogs(out CapturingLogger logger)
		{
			var previous = ColyseusContext.Logger;
			logger = new CapturingLogger();
			ColyseusContext.Logger = logger;
			return new Restore(() => ColyseusContext.Logger = previous);
		}

		/// <summary>Pin <see cref="RoomClock.GetNow" /> to a mutable cell; restore on dispose.</summary>
		public static IDisposable FreezeClock(Func<double> now)
		{
			var previous = RoomClock.GetNow;
			RoomClock.GetNow = now;
			return new Restore(() => RoomClock.GetNow = previous);
		}

		private class Restore : IDisposable
		{
			private readonly Action undo;
			public Restore(Action undo) { this.undo = undo; }
			public void Dispose() => undo();
		}
	}
}
