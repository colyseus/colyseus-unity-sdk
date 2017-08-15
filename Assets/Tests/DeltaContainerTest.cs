using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

using Colyseus;
using GameDevWare.Serialization;

public class DeltaContainerTest {
	DeltaContainer container;

	[SetUp]
	public void Init () {
		container = new DeltaContainer (GetRawData());
	}

	[TearDown]
	public void Dispose () {
		container.RemoveAllListeners ();
	}

	[Test]
	public void ListenAddRemove() {
		var newData = GetRawData ();

		var players = (IndexedDictionary<string, object>) newData ["players"];
		players.Remove ("key1");
		players.Add ("key3", new { value = "new" });

		var listenCalls = 0;
		container.Listen ("players/:id", (DataChange change) => {
			listenCalls++;

			if (change.operation == "add") {
				Assert.AreEqual("key3", change.path["id"]);
				Assert.AreEqual(new { value = "new" }, change.value);

			} else if (change.operation == "remove") {
				Assert.AreEqual("key1", change.path["id"]);
			}
		});

		container.Set (new IndexedDictionary<string, object>(newData));
		Assert.AreEqual (2, listenCalls);
	}

	[Test]
	public void ListenReplace() {
		var newData = GetRawData ();

		var players = (IndexedDictionary<string, object>) newData ["players"];
		players ["key1"] = new IndexedDictionary<string, object>(new Dictionary<string, object> {
			{"id", "key1"},
			{"position", new IndexedDictionary<string, object>(new Dictionary<string, object> {
				{"x", 50},
				{"y", 100}
			})}
		});
		newData ["players"] = players;

		var listenCalls = 0;
		container.Listen ("players/:id/position/:axis", (DataChange change) => {
			listenCalls++;

			Assert.AreEqual(change.path["id"], "key1");

			if (change.path["axis"] == "x") {
				Assert.AreEqual(change.value, 50);

			} else if (change.path["axis"] == "y") {
				Assert.AreEqual(change.value, 100);
			}
		});

		container.Set (new IndexedDictionary<string, object>(newData));
		Assert.AreEqual (2, listenCalls);
	}

	[Test]
	public void ListenAddArray() {
		var newData = GetRawData ();
		var messages = (List<object>) newData ["messages"];
		messages.Add ("new value");

		var listenCalls = 0;
		container.Listen ("messages/:number", (DataChange change) => {
			listenCalls++;
			Assert.AreEqual("add", change.operation);
			Assert.AreEqual("new value", change.value);
		});

		container.Set (newData);
		Assert.AreEqual (1, listenCalls);
	}

	[Test]
	public void ListenRemoveArray() {
		var newData = GetRawData ();
		var messages = (List<object>) newData ["messages"];
		messages.RemoveAt (0);

		var listenCalls = 0;
		container.Listen ("messages/:number", (DataChange change) => {
			listenCalls++;
			Assert.AreEqual("remove", change.operation);
			Assert.AreEqual("2", change.path["number"]);
		});

		container.Set (newData);
		Assert.AreEqual (1, listenCalls);
	}

	protected IndexedDictionary<string, object> GetRawData () {
		var data = new IndexedDictionary<string, object> ();
		var players = new IndexedDictionary<string, object> ();

		players.Add("key1", new IndexedDictionary<string, object>(new Dictionary<string, object> { 
			{"id", "key1"},
			{"position", new IndexedDictionary<string, object>(new Dictionary<string, object> { 
				{"x", 0},
				{"y", 10}
			})}
		}));
		players.Add("key2", new IndexedDictionary<string, object>(new Dictionary<string, object> { 
			{"id", "key2"},
			{"position", new IndexedDictionary<string, object>(new Dictionary<string, object>{ 
				{"x", 10}, 
				{"y", 20}
			})}
		}));

		data.Add ("players", players);
		data.Add ("messages", new List<object> { "one", "two", "three" });
		return data;
	}

}
