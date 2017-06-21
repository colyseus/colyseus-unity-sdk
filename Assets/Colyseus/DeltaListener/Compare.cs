using System;
using System.Collections.Generic;
using System.Linq;
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
			if (mirrorPacked.IsArray && objPacked.IsArray)
			{
				//this is fix for arrays & lists
				var mirrorList = mirrorPacked.AsList();
				var objList = objPacked.AsList();

				//fake the dictionaries for ProcessCollection
				int i = 0;
				var mirrorDict = mirrorList.ToDictionary(k =>
				{
					MessagePackObject key = MessagePackObject.FromObject(i);
					i++;
					return key;
				}, v=>v);
				i = 0;
				var objDict = objList.ToDictionary(k =>
				{
					MessagePackObject key = MessagePackObject.FromObject(i);
					i++;
					return key;
				}, v => v);

				var newKeys = objDict.Keys;
				var oldKeys = mirrorDict.Keys;

				//do it the same way - original way as it was - as with dictionary
				ProcessCollection(objPacked, patches, path, oldKeys, objDict, mirrorDict, newKeys);
			}
			else
			{
				//this was originally here - just for dictionaries
				MessagePackObjectDictionary mirrorDict = mirrorPacked.AsDictionary();
				MessagePackObjectDictionary objDict = objPacked.AsDictionary();

				var newKeys = objDict.Keys;
				var oldKeys = mirrorDict.Keys;

				ProcessCollection(objPacked, patches, path, oldKeys, objDict, mirrorDict, newKeys);
			}
		}

		private static void ProcessCollection(MessagePackObject objPacked, List<PatchObject> patches, List<string> path, ICollection<MessagePackObject> oldKeys,
			IDictionary<MessagePackObject, MessagePackObject> obj, IDictionary<MessagePackObject, MessagePackObject> mirror, ICollection<MessagePackObject> newKeys)
		{
			bool deleted = false;
			foreach (var key in oldKeys)
			{
				if (obj.ContainsKey(key) && !(!obj.ContainsKey(key) && mirror.ContainsKey(key) && !objPacked.IsArray))
				{
					var oldVal = mirror[key];
					var newVal = obj[key];

					ProcessValuePair(patches, path, oldVal, newVal, key);
				}
				else
				{
					List<string> removePath = new List<string>(path);
					removePath.Add(MessagePackObjectToStringKey(key));

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
					
					addPath.Add(MessagePackObjectToStringKey(key));

					patches.Add(new PatchObject
					{
						op = "add",
						path = addPath.ToArray(),
						value = obj[key]
					});
				}
			}
		}

		private static string MessagePackObjectToStringKey(MessagePackObject key)
		{
			if (key.UnderlyingType == typeof(string))
			{
				return key.AsString();
			}
			else if (key.UnderlyingType == typeof(Int32))
			{
				return key.AsInt32().ToString();
			}
			else throw new Exception("Invalid type of key");
		}

		private static void ProcessValuePair(List<PatchObject> patches, List<string> path, MessagePackObject oldVal, MessagePackObject newVal,
			MessagePackObject key)
		{
			if ((oldVal.IsDictionary && !oldVal.IsNil && newVal.IsDictionary && !newVal.IsNil) ||
			    (oldVal.IsArray && !oldVal.IsNil && newVal.IsArray && !newVal.IsNil))
			{
				List<string> deeperPath = new List<string>(path);
				deeperPath.Add(MessagePackObjectToStringKey(key));

				Generate(oldVal, newVal, patches, deeperPath);
			}
			else
			{
				if (oldVal != newVal)
				{
					//changed = true;

					List<string> replacePath = new List<string>(path);
					replacePath.Add(MessagePackObjectToStringKey(key));

					patches.Add(new PatchObject
					{
						op = "replace",
						path = replacePath.ToArray(),
						value = newVal
					});
				}
			}
		}
	}
}