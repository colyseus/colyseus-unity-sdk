using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using GameDevWare.Serialization;

namespace Colyseus
{
	using PatchListener = Listener<Action<DataChange>>;
	using FallbackPatchListener = Listener<Action<PatchObject>>;

	public struct DataChange
	{
		public Dictionary<string, string> path;
		public string operation; // : "add" | "remove" | "replace";
		public object value;
	}

	public struct Listener<T>
	{
		public T callback;
		public Regex[] rules;
		public string[] rawRules;
	}

	public class StateContainer
	{
		public IndexedDictionary<string, object> state;
		private List<PatchListener> listeners;
		private FallbackPatchListener defaultListener;

		private Dictionary<string, Regex> matcherPlaceholders = new Dictionary<string, Regex>()
		{
			{ ":id", new Regex(@"^([a-zA-Z0-9\-_]+)$") },
			{ ":number", new Regex(@"^([0-9]+)$") },
			{ ":string", new Regex(@"^(\w+)$") },
			{ ":axis", new Regex(@"^([xyz])$") },
			{ ":*", new Regex(@"(.*)") },
		};

		public StateContainer (IndexedDictionary<string, object> state)
		{
			this.state = state;
			this.Reset();
		}

		public PatchObject[] Set(IndexedDictionary<string, object> newData) {
			var patches = Compare.GetPatchList(this.state, newData);

			this.CheckPatches(patches);
			this.state = newData;

			return patches;
		}

		public void RegisterPlaceholder(string placeholder, Regex matcher)
		{
			this.matcherPlaceholders[placeholder] = matcher;
		}

		public FallbackPatchListener Listen(Action<PatchObject> callback)
		{
			FallbackPatchListener listener = new FallbackPatchListener {
				callback = callback,
				rules = new Regex[]{}
			};

			this.defaultListener = listener;

			return listener;
		}

		public PatchListener Listen(string segments, Action<DataChange> callback, bool immediate = false) {
			var rawRules = segments.Split ('/');
			var regexpRules = this.ParseRegexRules (rawRules);

			PatchListener listener = new PatchListener {
				callback = callback,
				rules = regexpRules,
				rawRules = rawRules
			};

			this.listeners.Add(listener);

			if (immediate) {
				List<PatchListener> onlyListener = new List<PatchListener>();
				onlyListener.Add(listener);
				this.CheckPatches(Compare.GetPatchList(new IndexedDictionary<string, object>(), this.state), onlyListener);
        	}

			return listener;
		}

		public void RemoveListener(PatchListener listener)
		{
			for (var i = this.listeners.Count - 1; i >= 0; i--)
			{
				if (this.listeners[i].Equals(listener))
				{
					this.listeners.RemoveAt(i);
				}
			}
		}

		public void RemoveAllListeners()
		{
			this.Reset();
		}

		protected Regex[] ParseRegexRules (string[] rules)
		{
			Regex[] regexpRules = new Regex[rules.Length];

			for (int i = 0; i < rules.Length; i++)
			{
				var segment = rules[i];
				if (segment.IndexOf(':') == 0)
				{
					if (this.matcherPlaceholders.ContainsKey(segment))
					{
						regexpRules[i] = this.matcherPlaceholders[segment];
					}
					else {
						regexpRules[i] = this.matcherPlaceholders[":*"];
					}

				} else {
					regexpRules[i] = new Regex("^" + segment + "$");
				}
			}

			return regexpRules;
		}

		private void CheckPatches(PatchObject[] patches, List<PatchListener> _listeners = null)
		{
			if (_listeners == null) 
			{
				_listeners = this.listeners;
			}

			for (var i = patches.Length - 1; i >= 0; i--)
			{
				var matched = false;

				for (var j = 0; j < _listeners.Count; j++)
				{
					var listener = _listeners[j];
					var pathVariables = this.GetPathVariables(patches[i], listener);
					if (pathVariables != null)
					{
						DataChange dataChange = new DataChange
						{
							path = pathVariables,
							operation = patches[i].operation,
							value = patches[i].value
						};

						listener.callback.Invoke (dataChange);
						matched = true;
					}
				}

				// check for fallback listener
				if (!matched && !object.Equals(this.defaultListener, default(FallbackPatchListener)))
				{
					this.defaultListener.callback.Invoke (patches [i]);
				}

			}

		}

		private Dictionary<string, string> GetPathVariables(PatchObject patch, PatchListener listener) {
			var result = new Dictionary<string, string> ();

			// skip if rules count differ from patch
			if (patch.path.Length != listener.rules.Length) {
				return null;
			}

			for (var i = 0; i < listener.rules.Length; i++) {
				var matches = listener.rules[i].Matches(patch.path[i]);
				if (matches.Count == 0 || matches.Count > 2) {
					return null;

				} else if (listener.rawRules[i][0] == ':') {
					result.Add ( listener.rawRules[i].Substring(1), matches[0].ToString() );
				}
			}

			return result;
		}

		private void Reset()
		{
			this.listeners = new List<PatchListener> ();

			this.defaultListener = default(FallbackPatchListener);
		}

	}
}
