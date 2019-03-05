using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

using Colyseus;

public class RoomTest {
	ClientComponent component;

	[UnityTest]
	public IEnumerator TestConnection()
	{
		var gameObject = new GameObject();
		component = gameObject.AddComponent<ClientComponent>();

		yield return new WaitForFixedUpdate();

		component.client.OnOpen += (object sender, System.EventArgs e) => {
			Assert.NotNull (component.client.id);
		};

		yield return new WaitForSeconds(0.1f);

		component.room.OnJoin += (object sender, System.EventArgs e) => {
			Assert.NotNull (component.room.id);
			Assert.NotNull (component.room.sessionId);
		};

		component.room.OnStateChange += (object sender, StateChangeEventArgs e) => {
			Assert.NotNull (component.room.state ["players"]);
			Assert.NotNull (component.room.state ["messages"]);
		};
	}
}
