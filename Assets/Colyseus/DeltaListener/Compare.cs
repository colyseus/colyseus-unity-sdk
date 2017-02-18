using System;
using System.Collections.Generic;
using MsgPack;
using MsgPack.Serialization;

namespace Colyseus
{
	public struct PatchObject
	{
		public string[] path;
		public string op; // : "add" | "remove" | "replace";
		public MessagePackObject value;
	}

	public class Compare
	{

		public static PatchObject[] GetPatchList(MessagePackObject tree1, MessagePackObject tree2)
		{
			List<PatchObject> patches = new List<PatchObject>();
			List<string> path = new List<string>();

			Generate(tree1, tree2, patches, path);

			return patches.ToArray();
		}

		// Dirty check if obj is different from mirror, generate patches and update mirror
		protected static void Generate(MessagePackObject mirrorPacked, MessagePackObject objPacked, List<PatchObject> patches, List<string> path)
		{
			MessagePackObjectDictionary mirror = mirrorPacked.AsDictionary();
			MessagePackObjectDictionary obj = objPacked.AsDictionary();
			
			var newKeys = obj.Keys;
			var oldKeys = mirror.Keys;
			//var changed = false;
			var deleted = false;

			foreach (var key in oldKeys)
			{
				if (obj.ContainsKey(key) && !(!obj.ContainsKey(key) && mirror.ContainsKey(key) && !objPacked.IsArray))
				{
					var oldVal = mirror[key];
					var newVal = obj[key];

					if (oldVal.IsDictionary && !oldVal.IsNil && newVal.IsDictionary  && !newVal.IsNil)
					{
						List<string> deeperPath = new List<string>(path);
						deeperPath.Add(key.AsString());

						Generate(oldVal, newVal, patches, deeperPath);
					} else {
						if (oldVal != newVal)
						{
							//changed = true;

							List<string> replacePath = new List<string>(path);
							replacePath.Add(key.AsString());

							patches.Add(new PatchObject
							{
								op = "replace",
								path = replacePath.ToArray(),
								value = newVal
							});
						}
					}
				}
				else {
					List<string> removePath = new List<string>(path);
					removePath.Add(key.AsString());

					patches.Add(new PatchObject
					{
						op = "remove",
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
					addPath.Add(key.AsString());

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