using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

using Colyseus;
using GameDevWare.Serialization;

public class RoomTest {
	ClientComponent component;

	[UnityTest]
	public IEnumerator TestConnection()
	{
		var gameObject = new GameObject();
		component = gameObject.AddComponent<ClientComponent>();

		yield return new WaitForFixedUpdate();

		component.client.OnOpen += () => {
			Assert.NotNull (component.client.Id);
		};

		yield return new WaitForSeconds(0.1f);

		component.room.OnJoin += () => {
			Assert.NotNull (component.room.Id);
			Assert.NotNull (component.room.SessionId);
		};

		component.room.OnStateChange += (IndexedDictionary<string, object> state, bool isFirstState) => {
			Assert.NotNull (component.room.State ["players"]);
			Assert.NotNull (component.room.State ["messages"]);
		};
	}
}
