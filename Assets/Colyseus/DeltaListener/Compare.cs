using System;
using System.Collections.Generic;

using GameDevWare.Serialization;
using GameDevWare.Serialization.MessagePack;

namespace Colyseus
{
	public struct PatchObject
	{
		public string[] path;
		public string operation; // : "add" | "remove" | "replace";
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
		protected static void Generate(IndexedDictionary<string, object> mirror, IndexedDictionary<string, object> obj, List<PatchObject> patches, List<string> path)
		{
			var newKeys = obj.Keys;
			var oldKeys = mirror.Keys;
			var deleted = false;

			foreach (var key in oldKeys)
			{
				if (obj.ContainsKey(key) && !(!obj.ContainsKey(key) && mirror.ContainsKey(key) && !(obj is Array)))
				{
					var oldVal = mirror[key];
					var newVal = obj[key];

					if (
						oldVal != null && 
						newVal != null && 
						oldVal is IndexedDictionary<string, object> &&
						newVal is IndexedDictionary<string, object>
					)
					{
						List<string> deeperPath = new List<string>(path);
						deeperPath.Add((string) key);

						Generate(
							(IndexedDictionary<string, object>) oldVal, 
							(IndexedDictionary<string, object>) newVal, 
							patches, 
							deeperPath
						);
					} else {
						if (oldVal != newVal)
						{
							List<string> replacePath = new List<string>(path);
							replacePath.Add((string) key);

							patches.Add(new PatchObject
							{
								operation = "replace",
								path = replacePath.ToArray(),
								value = newVal
							});
						}
					}
				}
				else {
					List<string> removePath = new List<string>(path);
					removePath.Add((string) key);

					patches.Add(new PatchObject
					{
						operation = "remove",
						path = removePath.ToArray()
					});

					deleted = true; // property has been deleted
				}
			}

			if (!deleted && newKeys.Count == oldKeys.Count) {
		        return;
		    }

			foreach (var key in newKeys)
			{

				if (!mirror.ContainsKey(key) && obj.ContainsKey(key))
				{
					List<string> addPath = new List<string>(path);
					addPath.Add((string) key);

					patches.Add(new PatchObject
					{
						operation = "add",
						path = addPath.ToArray(),
						value = obj[key]
					});
				}
			}

		}
	}
}