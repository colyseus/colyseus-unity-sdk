using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Threading.Tasks;


public class RoomTest {
	ClientComponent component;

	[UnityTest]
	public async void TestConnection()
	{
		var gameObject = new GameObject();
		component = gameObject.AddComponent<ClientComponent>();

		await Task.Run(() =>
		{
			component.room.OnJoin += () => {
				Assert.NotNull(component.room.Id);
				Assert.NotNull(component.room.SessionId);
			};

			component.room.OnStateChange += (State state, bool isFirstState) => {
				Assert.NotNull(component.room.State.entities);
			};
		});
	}
}
