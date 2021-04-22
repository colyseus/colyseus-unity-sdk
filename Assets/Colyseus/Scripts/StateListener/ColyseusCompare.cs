using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GameDevWare.Serialization;

namespace Colyseus
{
    /// <summary>
    ///     Utility class for getting lists of <see cref="PatchObject" /> and other related functionality
    /// </summary>
    public class ColyseusCompare
    {
        /// <summary>
        ///     Get a Patch List based off of an old and new state
        /// </summary>
        /// <param name="tree1">The old state</param>
        /// <param name="tree2">The incoming state to compare against the old</param>
        /// <returns>
        ///     An array of type <see cref="PatchObject" /> representing the difference from <paramref name="tree1" /> to
        ///     <paramref name="tree2" />
        /// </returns>
        public static PatchObject[] GetPatchList(IndexedDictionary<string, object> tree1,
            IndexedDictionary<string, object> tree2)
        {
            List<PatchObject> patches = new List<PatchObject>();
            List<string> path = new List<string>();

            Generate(tree1, tree2, patches, path);

            return patches.ToArray();
        }

        /// <summary>
        ///     Overridden Generate function for when we have a <see cref="List{T}" /> instead of an
        ///     <see cref="IndexedDictionary{KeyT,ValueT}" />
        /// </summary>
        protected static void Generate(List<object> mirror, List<object> obj, List<PatchObject> patches,
            List<string> path)
        {
            IndexedDictionary<string, object> mirrorDict = new IndexedDictionary<string, object>();
            for (int i = 0; i < mirror.Count; i++)
            {
                mirrorDict.Add(i.ToString(), mirror.ElementAt(i));
            }

            IndexedDictionary<string, object> objDict = new IndexedDictionary<string, object>();
            for (int i = 0; i < obj.Count; i++)
            {
                objDict.Add(i.ToString(), obj.ElementAt(i));
            }

            Generate(mirrorDict, objDict, patches, path);
        }

        /// <summary>
        ///     Dirty check if obj is different from mirror, generate patches and update mirror
        /// </summary>
        protected static void Generate(IndexedDictionary<string, object> mirror, IndexedDictionary<string, object> obj,
            List<PatchObject> patches, List<string> path)
        {
            ReadOnlyCollection<string> newKeys = obj.Keys;
            ReadOnlyCollection<string> oldKeys = mirror.Keys;
            bool deleted = false;

            for (int i = 0; i < oldKeys.Count; i++)
            {
                string key = oldKeys[i];
                if (
                    obj.ContainsKey(key) &&
                    obj[key] != null &&
                    !(!obj.ContainsKey(key) && mirror.ContainsKey(key))
                )
                {
                    object oldVal = mirror[key];
                    object newVal = obj[key];

                    if (
                        oldVal != null && newVal != null &&
                        !oldVal.GetType().IsPrimitive && oldVal.GetType() != typeof(string) &&
                        !newVal.GetType().IsPrimitive && newVal.GetType() != typeof(string) &&
                        ReferenceEquals(oldVal.GetType(), newVal.GetType())
                    )
                    {
                        List<string> deeperPath = new List<string>(path);
                        deeperPath.Add(key);

                        if (oldVal is IndexedDictionary<string, object>)
                        {
                            Generate(
                                (IndexedDictionary<string, object>) oldVal,
                                (IndexedDictionary<string, object>) newVal,
                                patches,
                                deeperPath
                            );
                        }
                        else if (oldVal is List<object>)
                        {
                            Generate(
                                (List<object>) oldVal,
                                (List<object>) newVal,
                                patches,
                                deeperPath
                            );
                        }
                    }
                    else
                    {
                        if (
                            oldVal == null && newVal != null ||
                            !oldVal.Equals(newVal)
                        )
                        {
                            List<string> replacePath = new List<string>(path);
                            replacePath.Add(key);

                            patches.Add(new PatchObject
                            {
                                operation = "replace",
                                path = replacePath.ToArray(),
                                value = newVal
                            });
                        }
                    }
                }
                else
                {
                    List<string> removePath = new List<string>(path);
                    removePath.Add(key);

                    patches.Add(new PatchObject
                    {
                        operation = "remove",
                        path = removePath.ToArray()
                    });

                    deleted = true; // property has been deleted
                }
            }

            if (!deleted && newKeys.Count == oldKeys.Count)
            {
                return;
            }

            foreach (string key in newKeys)
            {
                if (!mirror.ContainsKey(key) && obj.ContainsKey(key))
                {
                    List<string> addPath = new List<string>(path);
                    addPath.Add(key);

                    object newVal = obj[key];
                    if (newVal != null)
                    {
                        Type newValType = newVal.GetType();

                        // compare deeper additions
                        if (
                            !newValType.IsPrimitive &&
                            newValType != typeof(string)
                        )
                        {
                            if (newVal is IDictionary)
                            {
                                Generate(new IndexedDictionary<string, object>(),
                                    newVal as IndexedDictionary<string, object>, patches, addPath);
                            }
                            else if (newVal is IList)
                            {
                                Generate(new List<object>(), newVal as List<object>, patches, addPath);
                            }
                        }
                    }

                    patches.Add(new PatchObject
                    {
                        operation = "add",
                        path = addPath.ToArray(),
                        value = newVal
                    });
                }
            }
        }
    }
}