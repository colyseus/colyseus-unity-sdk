using System;
using System.Linq;
using Newtonsoft.Json.Linq;

// Patcher implementation heavily based on:
// - https://github.com/mcintyre321/JsonDiffPatch/ (e347344 - Sep 14, 2015)
// - https://github.com/tavis-software/Tavis.JsonPointer (1403ff9 - Jan 11, 2015)

namespace Colyseus
{
	public class Patcher
	{
		public Patcher ()
		{
		}

		public void Patch (ref JToken document, JArray patches)
		{
			for (int i = 0; i < patches.Count; i++) {
				string operation = patches [i]["op"].ToString();
				JsonPointer pointer = new JsonPointer (patches [i] ["path"].ToString ());
				document = ApplyOperation (operation, pointer, patches[i], document);
			}
		}

		protected JToken ApplyOperation (string operation, JsonPointer pointer, JToken patch, JToken target)
		{
			if (operation == "add") Add(pointer, patch["value"], target);
			else if (operation == "copy") Copy(new JsonPointer(patch["from"].ToString()), pointer, patch["value"], target);
			else if (operation == "move") Move(new JsonPointer(patch["from"].ToString()), pointer, patch["value"], target);
			else if (operation == "remove") Remove(pointer, patch["value"], target);
			else if (operation == "replace") target = Replace(pointer, patch["value"], target) ?? target;
			return target;
		}

		protected JToken Replace(JsonPointer pointer, JToken value, JToken target)
		{
			var token = pointer.Find(target);
			if (token.Parent == null)
			{
				return value;
			}
			else
			{
				token.Replace(value);
				return null;
			}
		}

		protected void Add(JsonPointer pointer, JToken value, JToken target)
		{
			JToken token = null;
			JObject parenttoken = null;
			var propertyName = pointer.ToString().Split('/').LastOrDefault();
			try
			{
				var parentArray = pointer.ParentPointer.Find(target) as JArray;

				if (parentArray == null || propertyName == "-")
				{
					if (pointer.IsNewPointer())
					{
						var parentPointer = pointer.ParentPointer;
						token = parentPointer.Find(target) as JArray;
					}
					else
					{
						token = pointer.Find(target);
					}
				}
				else
				{
					parentArray.Insert(int.Parse(propertyName), value);
					return;
				}
			}
			catch (ArgumentException)
			{
				var parentPointer = pointer.ParentPointer;
				parenttoken = parentPointer.Find(target) as JObject;
			}

			if (token == null && parenttoken != null)
			{
				parenttoken.Add(propertyName, value);
			}
			else if (token is JArray)
			{
				var array = token as JArray;

				array.Add(value);
			}
			else if (token.Parent is JProperty)
			{
				var prop = token.Parent as JProperty;
				prop.Value = value;
			}
		}


		protected void Remove(JsonPointer pointer, JToken value, JToken target)
		{
			var token = pointer.Find(target);
			if (token.Parent is JProperty)
			{
				token.Parent.Remove();
			}
			else
			{
				token.Remove();
			}
		}

		protected void Move(JsonPointer fromPointer, JsonPointer pointer, JToken value, JToken target)
		{
			if (pointer.ToString().StartsWith(fromPointer.ToString())) throw new ArgumentException("To path cannot be below from path");

			var token = fromPointer.Find(target);
			Remove (fromPointer, value, target);
			Add(pointer, value, target);
		}

		protected void Copy(JsonPointer fromPointer, JsonPointer pointer, JToken value, JToken target)
		{
			var token = fromPointer.Find(target); // Do I need to clone this?
			Add(pointer, value, target);
		}
	}
}

