using NUnit.Framework;
using UnityEngine;
using Colyseus;

public class FossilDeltaTest
{
	FossilDeltaSerializer serializer = new FossilDeltaSerializer();

	[SetUp]
	public void Init()
	{

	}

	[TearDown]
	public void Dispose()
	{

	}

	[Test]
	public void ApplyPatchTest()
	{
		Assert.DoesNotThrow(() =>
		{
			byte[] initialState = { (byte)Protocol.ROOM_STATE, 133, 165, 97, 114, 114, 97, 121, 144, 168, 101, 110, 116, 105, 116, 105, 101, 115, 128, 163, 105, 110, 116, 1, 164, 98, 111, 111, 108, 195, 166, 115, 116, 114, 105, 110, 103, 166, 115, 116, 114, 105, 110, 103 };
			serializer.SetState(initialState, 1);

			Assert.IsTrue(serializer.GetState().ContainsKey("array"));
			Assert.IsTrue(serializer.GetState().ContainsKey("entities"));
			Assert.IsTrue(serializer.GetState().ContainsKey("int"));
			Assert.IsTrue(serializer.GetState().ContainsKey("bool"));
			Assert.IsTrue(serializer.GetState().ContainsKey("string"));

			byte[] patchState = { (byte)Protocol.ROOM_STATE_PATCH, 49, 71, 10, 49, 71, 58, 133, 165, 97, 114, 114, 97, 121, 145, 1, 168, 101, 110, 116, 105, 116, 105, 101, 115, 129, 169, 83, 78, 52, 98, 89, 65, 83, 107, 87, 131, 161, 120, 10, 161, 121, 10, 164, 110, 97, 109, 101, 173, 74, 97, 107, 101, 32, 66, 97, 100, 108, 97, 110, 100, 115, 163, 105, 110, 116, 1, 164, 98, 111, 111, 108, 195, 166, 115, 116, 114, 105, 110, 103, 166, 115, 116, 114, 105, 110, 103, 51, 108, 100, 76, 100, 73, 59 };
			serializer.Patch(patchState, 1);
		});
	}

}