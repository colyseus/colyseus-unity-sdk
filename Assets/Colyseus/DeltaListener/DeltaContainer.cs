using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using MsgPack;

namespace Colyseus
{
	using PatchListener = Listener<Action<string[], MessagePackObject>>;
	using FallbackPatchListener = Listener<Action<string[], string, MessagePackObject>>;

	public struct Listener<T>
	{
		public T callback;
		public string operation;
		public Regex[] rules;
	}

	public class DeltaContainer
	{
		public MessagePackObject data;
		private Dictionary<string, List<PatchListener>> listeners;
		private List<FallbackPatchListener> fallbackListeners;

		private Dictionary<string, Regex> matcherPlaceholders = new Dictionary<string, Regex>()
		{
			{ ":id", new Regex(@"^([a-zA-Z0-9\-_]+)$") },
			{ ":number", new Regex(@"^([0-9]+)$") },
			{ ":string", new Regex(@"^(\w+)$") },
			{ ":axis", new Regex(@"^([xyz])$") },
			{ "*", new Regex(@"(.*)") },
		};

		public DeltaContainer (MessagePackObject data)
		{
			this.data = data;
			this.Reset();
		}

		public PatchObject[] Set(MessagePackObject newData) {
			var patches = Compare.GetPatchList(this.data, newData);

			this.CheckPatches(patches);
	        this.data = newData;

	        return patches;
		}

		public void RegisterPlaceholder(string placeholder, Regex matcher)
		{
			this.matcherPlaceholders[placeholder] = matcher;
		}

		public FallbackPatchListener Listen(Action<string[], string, MessagePackObject> callback)
		{
			FallbackPatchListener listener = new FallbackPatchListener {
				callback = callback,
				operation = "",
				rules = new Regex[]{}
			};

			this.fallbackListeners.Add(listener);

			return listener;
		}

		public PatchListener Listen(string segments, string operation, Action<string[], MessagePackObject> callback) {
			var regexpRules = this.ParseRegexRules (segments.Split('/'));

			PatchListener listener = new PatchListener {
	            callback = callback,
	            operation = operation,
	            rules = regexpRules
			};

			this.listeners[operation].Add(listener);

	        return listener;
		}

		public void RemoveListener(PatchListener listener)
		{
			for (var i = this.listeners[listener.operation].Count - 1; i >= 0; i--)
			{
				if (this.listeners[listener.operation][i].Equals(listener))
				{
					this.listeners[listener.operation].RemoveAt(i);
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
						regexpRules[i] = this.matcherPlaceholders["*"];
					}

				} else {
					regexpRules[i] = new Regex(segment);
				}
			}

			return regexpRules;
		}

		private void CheckPatches(PatchObject[] patches)
		{

			for (var i = patches.Length - 1; i >= 0; i--)
			{
				var matched = false;
				var op = patches[i].op;
				for (var j = 0; j < this.listeners[op].Count; j++)
				{
					var listener = this.listeners[op][j];
					var matches = this.CheckPatch(patches[i], listener);
					if (matches.Length > 0)
					{
						listener.callback.Invoke (matches, patches[i].value);
						matched = true;
					}
				}

				// check for fallback listener
				var fallbackListenersCount = this.fallbackListeners.Count;
				if (!matched && fallbackListenersCount > 0)
				{
					for (var j = 0; j < fallbackListenersCount; j++)
					{
						this.fallbackListeners [j].callback.Invoke (patches [i].path, patches [i].op, patches [i].value);
					}
				}

			}

		}

		private string[] CheckPatch(PatchObject patch, PatchListener listener) {
	        // skip if rules count differ from patch
	        if (patch.path.Length != listener.rules.Length) {
				return new string[] { };
	        }

			List<string> pathVars = new List<string>();

			for (var i = 0; i < listener.rules.Length; i++) {
				var matches = listener.rules[i].Matches(patch.path[i]);
				if (matches.Count == 0 || matches.Count > 2) {
	                return new string[] { };

				} else if ( matches[0].Groups.Count > 1 ) {
					pathVars.Add(matches[0].ToString());
					// pathVars = pathVars.concat(matches.slice(1));
	            }
	        }

			return pathVars.ToArray();
	    }

	    private void Reset()
		{
			this.listeners = new Dictionary<string, List<PatchListener>>()
			{
				{ "add", new List<PatchListener>() },
				{ "remove", new List<PatchListener>() },
				{ "replace", new List<PatchListener>() }
	        };
			this.fallbackListeners = new List<FallbackPatchListener>();
	    }

	}
}
