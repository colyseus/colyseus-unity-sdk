using System;
using System.Linq;
using System.Collections;
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

		protected static void Generate(List<object> mirror, List<object> obj, List<PatchObject> patches, List<string> path)
		{
			var mirrorDict = new IndexedDictionary<string, object> ();
			for (int i = 0; i < mirror.Count; i++) {
				mirrorDict.Add (i.ToString(), mirror.ElementAt (i));
			}

			var objDict = new IndexedDictionary<string, object> ();
			for (int i = 0; i < obj.Count; i++) {
				objDict.Add (i.ToString(), obj.ElementAt (i));
			}

			Generate (mirrorDict, objDict, patches, path);
		}

		// Dirty check if obj is different from mirror, generate patches and update mirror
		protected static void Generate(IndexedDictionary<string, object> mirror, IndexedDictionary<string, object> obj, List<PatchObject> patches, List<string> path)
		{
			var newKeys = obj.Keys;
			var oldKeys = mirror.Keys;
			var deleted = false;

			for (int i = 0; i < oldKeys.Count; i++) 
			{
				var key = oldKeys [i];
				if (
					obj.ContainsKey(key) && 
					obj[key] != null &&
					!(!obj.ContainsKey(key) && mirror.ContainsKey(key))
				)
				{
					var oldVal = mirror[key];
					var newVal = obj[key];

					if (
						oldVal != null && newVal != null &&
						!oldVal.GetType ().IsPrimitive && oldVal.GetType () != typeof(string) &&
						!newVal.GetType ().IsPrimitive && newVal.GetType () != typeof(string) && 
						Object.ReferenceEquals(oldVal.GetType (), newVal.GetType ())
					)
					{
						List<string> deeperPath = new List<string>(path);
						deeperPath.Add((string) key);

						if (oldVal is IndexedDictionary<string, object>) {
							Generate(
								(IndexedDictionary<string, object>) oldVal,
								(IndexedDictionary<string, object>) newVal,
								patches,
								deeperPath
							);

						} else if (oldVal is List<object>) {
							Generate(
								((List<object>) oldVal),
								((List<object>) newVal),
								patches,
								deeperPath
							);
						}
							
					} else {
						if (
							(oldVal == null && newVal != null) ||
							!oldVal.Equals(newVal)
						)
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

					var newVal = obj [key];
					if (newVal != null) {
						var newValType = newVal.GetType ();

						// compare deeper additions
						if (
							!newValType.IsPrimitive && 
							newValType != typeof(string)
						) {
							if (newVal is IDictionary) {
								Generate(new IndexedDictionary<string, object>(), newVal as IndexedDictionary<string, object>, patches, addPath);

							} else if (newVal is IList) {
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

//		protected static List<string> GetObjectKeys (object data)
//		{
//			if (data is IndexedDictionary<string, object>) {
//				var d = (IndexedDictionary<string, object>)data;
//				return d.Keys;
//
//			} else if (data is List<object>) {
//				var d = (IndexedDictionary<string, object>)data;
////				d.Keys
//				return d.Keys;
//			}
//			
//		}

	}
}
