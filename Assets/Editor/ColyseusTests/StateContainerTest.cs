using NUnit.Framework;
using System.Collections.Generic;

using Colyseus;
using GameDevWare.Serialization;

public class StateContainerTest {
	StateContainer container;

	[SetUp]
	public void Init () {
		container = new StateContainer (GetRawData());
	}

	[TearDown]
	public void Dispose () {
		container.RemoveAllListeners ();
	}

	[Test]
	public void ListenAddString() {
		var newData = GetRawData ();
		newData ["some_string"] = "hello!";

		var listenCalls = 0;
		container.Listen ("some_string", (DataChange change) => {
			listenCalls++;
			Assert.AreEqual("add", change.operation);
			Assert.AreEqual("hello!", change.value);
		});

		container.Set (newData);
		Assert.AreEqual (1, listenCalls);
	}

	[Test]
	public void ListenReplaceNull() {
		var newData = GetRawData ();

		var listenCalls = 0;
		container.Listen ("null", (DataChange change) => {
			listenCalls++;
			Assert.AreEqual("replace", change.operation);
			Assert.AreEqual(10, change.value);
		});
		newData ["null"] = 10;

		container.Set (newData);
		Assert.AreEqual (1, listenCalls);
	}

	[Test]
	public void ListenAddNull() {
		var newData = GetRawData ();
		newData ["null_new"] = null;

		var listenCalls = 0;
		container.Listen ("null_new", (DataChange change) => {
			listenCalls++;
			Assert.AreEqual("add", change.operation);
			Assert.AreEqual(null, change.value);
		});

		container.Set (newData);
		Assert.AreEqual (1, listenCalls);
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

		container.Set (newData);
		Assert.AreEqual (2, listenCalls);
	}

	[Test]
	public void ListenReplaceString() {
		var newData = GetRawData ();
		newData ["turn"] = "mutated";

		var listenCalls = 0;
		container.Listen ("turn", (DataChange change) => {
			listenCalls++;
			Assert.AreEqual(change.value, "mutated");
		});

		container.Set (newData);
		Assert.AreEqual (1, listenCalls);
	}


	[Test]
	public void ListenWithoutPlaceholder() {
		var newData = GetRawData ();

		var game = (IndexedDictionary<string, object>) newData ["game"];
		game ["turn"] = 1;

		var listenCalls = 0;
		container.Listen ("game/turn", (DataChange change) => {
			listenCalls++;
			Assert.AreEqual(change.operation, "replace");
			Assert.AreEqual(change.value, 1);
		});

		container.Set (newData);
		Assert.AreEqual (1, listenCalls);
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
			if (listenCalls == 1) {
				Assert.AreEqual("remove", change.operation);
				Assert.AreEqual("2", change.path["number"]);
				Assert.AreEqual(null, change.value);

			} else if (listenCalls == 2) {
				Assert.AreEqual("replace", change.operation);
				Assert.AreEqual("1", change.path["number"]);
				Assert.AreEqual("three", change.value);

			} else if (listenCalls == 3) {
				Assert.AreEqual("replace", change.operation);
				Assert.AreEqual("0", change.path["number"]);
				Assert.AreEqual("two", change.value);
			}
		});

		container.Set (newData);
		Assert.AreEqual (3, listenCalls);
	}

	[Test]
	public void ListenInitialState() {
		var container = new StateContainer (new IndexedDictionary<string, object>());
		var listenCalls = 0;

		container.Listen ("players/:id/position/:attribute", (DataChange change) => {
			listenCalls++;
		});

		container.Listen ("turn", (DataChange change) => {
			listenCalls++;
		});

		container.Listen ("game/turn", (DataChange change) => {
			listenCalls++;
		});

		container.Listen ("messages/:number", (DataChange change) => {
			listenCalls++;
		});

		container.Set (GetRawData ());

		Assert.AreEqual (9, listenCalls);
	}

	[Test]
	public void ListenWithImmediate()
	{
		var container = new StateContainer(GetRawData());
		var listenCalls = 0;

		container.Listen("players/:id/position/:attribute", (DataChange change) => {
			listenCalls++;
		}, true);

		container.Listen("turn", (DataChange change) => {
			listenCalls++;
		}, true);

		container.Listen("game/turn", (DataChange change) => {
			listenCalls++;
		}, true);

		container.Listen("messages/:number", (DataChange change) => {
			listenCalls++;
		}, true);

		Assert.AreEqual(9, listenCalls);
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

		data.Add ("game", new IndexedDictionary<string, object>(new Dictionary<string, object> { 
			{"turn", 0}
		}));
		data.Add ("players", players);
		data.Add ("turn", "none");
		data.Add ("null", null);
		data.Add ("messages", new List<object> { "one", "two", "three" });
		return data;
	}

}
