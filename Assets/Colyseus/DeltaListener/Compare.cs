using System;
using System.Collections.Generic;
using GameDevWare.Serialization;

namespace Colyseus
{
    public struct PatchObject
    {
        public string[] path;
        public string op; // : "add" | "remove" | "replace";
        public object value;
    }

    public class Compare
    {
        public static PatchObject[] GetPatchList(IndexedDictionary<string, object> tree1, IndexedDictionary<string, object> tree2)
        {
            List<PatchObject> patches = new List<PatchObject>();
            List<string> path = new List<string>();

            Generate(tree1, tree2, patches, path);

            return patches.ToArray();
        }

        // Dirty check if obj is different from mirror, generate patches and update mirror
        protected static void Generate(IndexedDictionary<string, object> mirrorPacked, IndexedDictionary<string, object> objPacked, List<PatchObject> patches, List<string> path)
        {
            var obj = objPacked;
            // if (mirrorPacked.GetType() == typeof(IndexedDictionary<string, object>))
            // mirrorPacked = Utils.ConvertDictionary((IndexedDictionary<string, object>)mirrorPacked);
            var mirror = mirrorPacked;


            var newKeys = obj.Keys;
            var oldKeys = mirror.Keys;
            //var changed = false;
            var deleted = false;

            foreach (var key in oldKeys)
            {
                if (obj.ContainsKey(key)
                 && !(!obj.ContainsKey(key) && mirror.ContainsKey(key) && !(objPacked is Array))) //todo check type
                {
                    var oldVal = mirror[key];
                    var newVal = obj[key];

                    if (oldVal != null && oldVal is IDictionary<string, object>
                        && newVal != null && newVal is IDictionary<string, object>)
                    {
                        List<string> deeperPath = new List<string>(path);
                        deeperPath.Add((string)key);

                        Generate((IndexedDictionary<string, object>)oldVal,
                                 (IndexedDictionary<string, object>)newVal,
                                 patches, deeperPath);
                    }
                    else
                    {
                        if (oldVal != newVal)
                        {
                            //changed = true;

                            List<string> replacePath = new List<string>(path);
                            replacePath.Add((string)key);

                            patches.Add(new PatchObject
                            {
                                op = "replace",
                                path = replacePath.ToArray(),
                                value = newVal
                            });
                        }
                    }
                }
                else
                {
                    List<string> removePath = new List<string>(path);
                    removePath.Add((string)key);

                    patches.Add(new PatchObject
                    {
                        op = "remove",
                        path = removePath.ToArray()
                    });

                    deleted = true; // property has been deleted
                }
            }

            if (!deleted && newKeys.Count == oldKeys.Count)
            {
                return;
            }

            foreach (var key in newKeys)
            {
                if (!mirror.ContainsKey(key) && obj.ContainsKey(key))
                {
                    List<string> addPath = new List<string>(path);
                    addPath.Add((string)key);

                    patches.Add(new PatchObject
                    {
                        op = "add",
                        path = addPath.ToArray(),
                        value = (IndexedDictionary<string, object>)obj[key]
                    });
                }
            }
        }
    }
}