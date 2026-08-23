using System;
using System.Collections.Generic;

namespace Colyseus.Predict
{
	/// <summary>
	///     Declarative settlement for a <see cref="PredictedEventChannel{T}" />
	///     (port of predict/confirmOn.ts): the confirm signal is STATE itself —
	///     a crate's <c>alive</c> flipping, a banana joining or leaving its
	///     collection — so the channel wires the schema listeners for you and
	///     tears them down with the channel. Three forms, all plain data:
	///     <code>
	///     new ConfirmOn { Collection = "crates",  Field = "alive", Equals = false }
	///     new ConfirmOn { Collection = "bananas", Event = "remove" }
	///     new ConfirmOn { Collection = "bananas", Event = "add", Mine = "owner" }
	///     </code>
	///     Root-level collections only. Confirm-only: rejects are never
	///     auto-bound — the ack-anchored auto-reject already covers the miss case,
	///     and a wrong binding producing silent rejects would be hard to debug.
	/// </summary>
	public class ConfirmOn
	{
		/// <summary>Root-state collection to watch.</summary>
		public string Collection;
		/// <summary>
		///     Field-flip form: when a child's <see cref="Field" /> becomes
		///     <see cref="Equals" />, the entry keyed by that child's COLLECTION
		///     KEY confirms — so key your entries (<c>UniqueBy</c>) by collection key.
		/// </summary>
		public string Field;
		public new object Equals;
		/// <summary>
		///     Membership form: <c>"remove"</c> confirms the removed key;
		///     <c>"add"</c> confirms KEYLESS (a predicted spawn can't know the key
		///     the server will assign).
		/// </summary>
		public string Event;
		/// <summary>
		///     With <c>Event = "add"</c>: a child field compared to this client's
		///     session id, so a remote player's arrival doesn't settle our pending drop.
		/// </summary>
		public string Mine;
	}

	internal static class ConfirmOnWiring
	{
		/// <summary>
		///     Subscribe the schema listeners for a binding and route them into
		///     <paramref name="confirm" />. Returns one idempotent detacher.
		/// </summary>
		public static Action Wire(IPredictCallbacks callbacks, Func<object, int> confirm, ConfirmOn on, Func<string> sessionId)
		{
			if (on == null || string.IsNullOrEmpty(on.Collection))
			{
				throw new Exception("ConfirmOn: `Collection` is required.");
			}
			if (on.Field == null && on.Event != "add" && on.Event != "remove")
			{
				throw new Exception("ConfirmOn: set `Field` + `Equals`, or `Event` = \"add\" | \"remove\".");
			}

			var offs = new List<Action>();
			if (on.Field != null)
			{
				// field-flip: per-child listener, keyed by collection key. The
				// target is classified once so each sample is one compare.
				var listeners = new Dictionary<Schema.Schema, Action>();
				double? numericTarget = FieldKinds.IsNumericValue(on.Equals) ? Convert.ToDouble(on.Equals) : (double?)null;
				bool Matches(object value) => numericTarget.HasValue
					? FieldKinds.IsNumericValue(value) && Convert.ToDouble(value) == numericTarget.Value
					: object.Equals(value, on.Equals);
				offs.Add(callbacks.OnAdd(on.Collection, (child, key) =>
				{
					if (listeners.ContainsKey(child)) { return; }   // decoder can re-fire onAdd for one ref
					// no immediate fire: a child already flipped at bind time is
					// history, not a settle signal (it would fire OnUnpredicted)
					listeners[child] = callbacks.Listen(child, on.Field, value =>
					{
						if (Matches(value)) { confirm(key); }
					}, false);
				}, true));
				offs.Add(callbacks.OnRemove(on.Collection, (child, key) =>
				{
					if (listeners.TryGetValue(child, out var off))
					{
						off();
						listeners.Remove(child);
					}
				}));
				offs.Add(() =>
				{
					foreach (var off in listeners.Values) { off(); }
					listeners.Clear();
				});
			}
			else if (on.Event == "remove")
			{
				offs.Add(callbacks.OnRemove(on.Collection, (child, key) => confirm(key)));
			}
			else
			{
				// existing children are history, not arrivals
				offs.Add(callbacks.OnAdd(on.Collection, (child, key) =>
				{
					if (on.Mine != null)
					{
						string mine = sessionId?.Invoke();
						if (mine == null || !object.Equals(child[on.Mine], mine)) { return; }
					}
					confirm(null);
				}, false));
			}

			return () => { foreach (var off in offs) { off(); } };
		}
	}
}
