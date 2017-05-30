using System;
using System.Collections.Generic;
using GameDevWare.Serialization;

namespace Colyseus
{
    public struct PatchObject
    {
        public string[] path;
        public string op; // : "add" | "remove" | "replace";
        public Object value;
    }

    public class Compare
    {

        public static PatchObject[] GetPatchList(Object tree1, Object tree2)
        {
            List<PatchObject> patches = new List<PatchObject>();
            List<string> path = new List<string>();

            Generate(tree1, tree2, patches, path);

            return patches.ToArray();
        }

        // Dirty check if obj is different from mirror, generate patches and update mirror
        protected static void Generate(Object mirrorPacked, Object objPacked, List<PatchObject> patches, List<string> path)
        {
            var s = mirrorPacked.GetType();
            var obj = (IndexedDictionary<string, object>)objPacked;
            if (mirrorPacked.GetType() == typeof(IndexedDictionary<string, object>))
                mirrorPacked = Utils.ConvertDictionary((IndexedDictionary<string, object>)mirrorPacked);
            var mirror = (Dictionary<string, object>)mirrorPacked;


            var newKeys = obj.Keys;
            var oldKeys = mirror.Keys;
            //var changed = false;
            var deleted = false;

            foreach (var key in oldKeys)
            {
                if (obj.ContainsKey((string)key) && !(!obj.ContainsKey((string)key) && mirror.ContainsKey(key) && objPacked.GetType() != typeof(Array)))
                {
                    var oldVal = mirror[key];
                    var newVal = obj[(string)key];

                    if (oldVal != null && oldVal.GetType() == typeof(IDictionary<string, object>)
                    && newVal != null && newVal.GetType() == typeof(IDictionary<string, object>))
                    {
                        List<string> deeperPath = new List<string>(path);
                        deeperPath.Add((string)key);

                        Generate(oldVal, newVal, patches, deeperPath);
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
                        value = obj[key]
                    });
                }
            }

        }
    }
}