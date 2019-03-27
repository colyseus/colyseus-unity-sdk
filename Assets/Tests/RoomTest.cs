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

		component.client.OnOpen += (object sender, System.EventArgs e) => {
			Assert.NotNull (component.client.Id);
		};

		yield return new WaitForSeconds(0.1f);

		component.room.OnJoin += (object sender, System.EventArgs e) => {
			Assert.NotNull (component.room.Id);
			Assert.NotNull (component.room.SessionId);
		};

		component.room.OnStateChange += (object sender, StateChangeEventArgs<IndexedDictionary<string, object>> e) => {
			Assert.NotNull (component.room.State ["players"]);
			Assert.NotNull (component.room.State ["messages"]);
		};
	}
}
