using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GameDevWare.Serialization;

namespace Colyseus
{
    using PatchListener = Listener<Action<DataChange>>;
    using FallbackPatchListener = Listener<Action<PatchObject>>;

    /// <summary>
    ///     The container class for the current state, with listeners for any changes to the state
    /// </summary>
    public class ColyseusStateContainer
    {
        private FallbackPatchListener defaultListener;
        private List<PatchListener> listeners;

        private readonly Dictionary<string, Regex> matcherPlaceholders = new Dictionary<string, Regex>
        {
            {":id", new Regex(@"^([a-zA-Z0-9\-_]+)$")},
            {":number", new Regex(@"^([0-9]+)$")},
            {":string", new Regex(@"^(\w+)$")},
            {":axis", new Regex(@"^([xyz])$")},
            {":*", new Regex(@"(.*)")}
        };

        /// <summary>
        ///     The current state
        /// </summary>
        public IndexedDictionary<string, object> state;

        public ColyseusStateContainer(IndexedDictionary<string, object> state)
        {
            this.state = state;
            Reset();
        }

        /// <summary>
        ///     Sets the state of the container
        /// </summary>
        /// <param name="newData">The new state</param>
        /// <returns>An array of <see cref="PatchObject" /> representing the update</returns>
        public PatchObject[] Set(IndexedDictionary<string, object> newData)
        {
            PatchObject[] patches = ColyseusCompare.GetPatchList(state, newData);

            CheckPatches(patches);
            state = newData;

            return patches;
        }

        /// <summary>
        ///     Adds a new placeHolder value to <see cref="matcherPlaceholders" />
        /// </summary>
        /// <param name="placeholder">The placeholder string</param>
        /// <param name="matcher">The <see cref="Regex" /> value we will apply</param>
        public void RegisterPlaceholder(string placeholder, Regex matcher)
        {
            matcherPlaceholders[placeholder] = matcher;
        }

        /// <summary>
        ///     Set a new default <exception cref="FallbackPatchListener"></exception>
        /// </summary>
        /// <param name="callback">The callback we trigger on a state change</param>
        /// <returns>The <see cref="FallbackPatchListener" /> we're going to use</returns>
        public FallbackPatchListener Listen(Action<PatchObject> callback)
        {
            FallbackPatchListener listener = new FallbackPatchListener
            {
                callback = callback,
                rules = new Regex[] { }
            };

            defaultListener = listener;

            return listener;
        }

        /// <summary>
        ///     Add a new <see cref="PatchListener" /> to <see cref="listeners" />
        /// </summary>
        /// <param name="segments">"/" separated rules to listen for</param>
        /// <param name="callback">The callback function for the <see cref="PatchListener" /> to perform</param>
        /// <param name="immediate">If true, triggers an immediate invocation of <paramref name="callback" /></param>
        /// <returns>The <see cref="PatchListener" /> we added</returns>
        public PatchListener Listen(string segments, Action<DataChange> callback, bool immediate = false)
        {
            string[] rawRules = segments.Split('/');
            Regex[] regexpRules = ParseRegexRules(rawRules);

            PatchListener listener = new PatchListener
            {
                callback = callback,
                rules = regexpRules,
                rawRules = rawRules
            };

            listeners.Add(listener);

            if (immediate)
            {
                List<PatchListener> onlyListener = new List<PatchListener>();
                onlyListener.Add(listener);
                CheckPatches(ColyseusCompare.GetPatchList(new IndexedDictionary<string, object>(), state), onlyListener);
            }

            return listener;
        }

        /// <summary>
        ///     Remove a <see cref="PatchListener" /> from <see cref="listeners" />
        /// </summary>
        /// <param name="listener">The <see cref="PatchListener" /> to remove</param>
        public void RemoveListener(PatchListener listener)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (listeners[i].Equals(listener))
                {
                    listeners.RemoveAt(i);
                }
            }
        }

        /// <summary>
        ///     Resets the StateContainer and clears the listener lists as well as the default listener
        /// </summary>
        public void RemoveAllListeners()
        {
            Reset();
        }

        /// <summary>
        ///     Parse incoming raw rules and convert to <see cref="Listener{T}.Action{Colyseus.DataChange}}.rules" />
        /// </summary>
        /// <param name="rules">The <see cref="PatchListener" /> raw rules</param>
        /// <returns></returns>
        protected Regex[] ParseRegexRules(string[] rules)
        {
            Regex[] regexpRules = new Regex[rules.Length];

            for (int i = 0; i < rules.Length; i++)
            {
                string segment = rules[i];
                if (segment.IndexOf(':') == 0)
                {
                    if (matcherPlaceholders.ContainsKey(segment))
                    {
                        regexpRules[i] = matcherPlaceholders[segment];
                    }
                    else
                    {
                        regexpRules[i] = matcherPlaceholders[":*"];
                    }
                }
                else
                {
                    regexpRules[i] = new Regex("^" + segment + "$");
                }
            }

            return regexpRules;
        }

        private void CheckPatches(PatchObject[] patches, List<PatchListener> _listeners = null)
        {
            if (_listeners == null)
            {
                _listeners = listeners;
            }

            for (int i = patches.Length - 1; i >= 0; i--)
            {
                bool matched = false;

                for (int j = 0; j < _listeners.Count; j++)
                {
                    PatchListener listener = _listeners[j];
                    Dictionary<string, string> pathVariables = GetPathVariables(patches[i], listener);
                    if (pathVariables != null)
                    {
                        DataChange dataChange = new DataChange
                        {
                            path = pathVariables,
                            operation = patches[i].operation,
                            value = patches[i].value
                        };

                        listener.callback.Invoke(dataChange);
                        matched = true;
                    }
                }

                // check for fallback listener
                if (!matched && !Equals(defaultListener, default(FallbackPatchListener)))
                {
                    defaultListener.callback.Invoke(patches[i]);
                }
            }
        }

        private Dictionary<string, string> GetPathVariables(PatchObject patch, PatchListener listener)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            // skip if rules count differ from patch
            if (patch.path.Length != listener.rules.Length)
            {
                return null;
            }

            for (int i = 0; i < listener.rules.Length; i++)
            {
                MatchCollection matches = listener.rules[i].Matches(patch.path[i]);
                if (matches.Count == 0 || matches.Count > 2)
                {
                    return null;
                }

                if (listener.rawRules[i][0] == ':')
                {
                    result.Add(listener.rawRules[i].Substring(1), matches[0].ToString());
                }
            }

            return result;
        }

        private void Reset()
        {
            listeners = new List<PatchListener>();

            defaultListener = default;
        }
    }
}