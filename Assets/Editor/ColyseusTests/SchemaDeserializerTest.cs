using NUnit.Framework;
using UnityEngine;

public class SchemaDeserializerTest
{

	[SetUp]
	public void Init()
	{

	}

	[TearDown]
	public void Dispose()
	{

	}

	[Test]
	public void PrimitiveTypesTest()
	{
		var state = new SchemaTest.PrimitiveTypes.PrimitiveTypes();
		byte[] bytes = { 0, 128, 1, 255, 2, 0, 128, 3, 255, 255, 4, 0, 0, 0, 128, 5, 255, 255, 255, 255, 6, 0, 0, 0, 0, 0, 0, 0, 128, 7, 255, 255, 255, 255, 255, 255, 31, 0, 8, 204, 204, 204, 253, 9, 255, 255, 255, 255, 255, 255, 239, 127, 10, 208, 128, 11, 204, 255, 12, 209, 0, 128, 13, 205, 255, 255, 14, 210, 0, 0, 0, 128, 15, 203, 0, 0, 224, 255, 255, 255, 239, 65, 16, 203, 0, 0, 0, 0, 0, 0, 224, 195, 17, 203, 255, 255, 255, 255, 255, 255, 63, 67, 18, 203, 61, 255, 145, 224, 255, 255, 239, 199, 19, 203, 153, 153, 153, 153, 153, 153, 185, 127, 20, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 21, 1 };
		state.Decode(bytes);

		Assert.AreEqual(state.int8, -128);
		Assert.AreEqual(state.uint8, 255);
		Assert.AreEqual(state.int16, -32768);
		Assert.AreEqual(state.uint16, 65535);
		Assert.AreEqual(state.int32, -2147483648);
		Assert.AreEqual(state.uint32, 4294967295);
		Assert.AreEqual(state.int64, -9223372036854775808);
		Assert.AreEqual(state.uint64, 9007199254740991);
		Assert.AreEqual(state.float32, -3.40282347E+37f);
		Assert.AreEqual(state.float64, 1.7976931348623157e+308);

		Assert.AreEqual(state.varint_int8, -128);
		Assert.AreEqual(state.varint_uint8, 255);
		Assert.AreEqual(state.varint_int16, -32768);
		Assert.AreEqual(state.varint_uint16, 65535);
		Assert.AreEqual(state.varint_int32, -2147483648);
		Assert.AreEqual(state.varint_uint32, 4294967295);
		Assert.AreEqual(state.varint_int64, -9223372036854775808);
		Assert.AreEqual(state.varint_uint64, 9007199254740991);
		Assert.AreEqual(state.varint_float32, -3.40282347E+38f);
		Assert.AreEqual(state.varint_float64, Mathf.Infinity);

		Assert.AreEqual(state.str, "Hello world");
		Assert.AreEqual(state.boolean, true);
	}

	[Test]
	public void ChildSchemaTypesTest()
	{
		var state = new SchemaTest.ChildSchemaTypes.ChildSchemaTypes();
		byte[] bytes = { 0, 0, 205, 244, 1, 1, 205, 32, 3, 193, 1, 0, 204, 200, 1, 205, 44, 1, 193 };
		state.Decode(bytes);

		Assert.AreEqual(state.child.x, 500);
		Assert.AreEqual(state.child.y, 800);

		Assert.AreEqual(state.secondChild.x, 200);
		Assert.AreEqual(state.secondChild.y, 300);
	}

	[Test]
	public void ArraySchemaTypesTest()
	{
		var state = new SchemaTest.ArraySchemaTypes.ArraySchemaTypes();
		byte[] bytes = { 0, 2, 2, 0, 0, 100, 1, 208, 156, 193, 1, 0, 100, 1, 208, 156, 193, 1, 4, 4, 0, 0, 1, 10, 2, 20, 3, 205, 192, 13, 2, 3, 3, 0, 163, 111, 110, 101, 1, 163, 116, 119, 111, 2, 165, 116, 104, 114, 101, 101, 3, 3, 3, 0, 232, 3, 0, 0, 1, 192, 13, 0, 0, 2, 72, 244, 255, 255 };

		state.arrayOfSchemas.OnAdd += (value, key) => Debug.Log("onAdd, arrayOfSchemas => " + key);
		state.arrayOfNumbers.OnAdd += (value, key) => Debug.Log("onAdd, arrayOfNumbers => " + key);
		state.arrayOfStrings.OnAdd += (value, key) => Debug.Log("onAdd, arrayOfStrings => " + key);
		state.arrayOfInt32.OnAdd += (value, key) => Debug.Log("onAdd, arrayOfInt32 => " + key);
		state.Decode(bytes);

		Assert.AreEqual(state.arrayOfSchemas.Count, 2);
		Assert.AreEqual(state.arrayOfSchemas[0].x, 100);
		Assert.AreEqual(state.arrayOfSchemas[0].y, -100);
		Assert.AreEqual(state.arrayOfSchemas[1].x, 100);
		Assert.AreEqual(state.arrayOfSchemas[1].y, -100);

		Assert.AreEqual(state.arrayOfNumbers.Count, 4);
		Assert.AreEqual(state.arrayOfNumbers[0], 0);
		Assert.AreEqual(state.arrayOfNumbers[1], 10);
		Assert.AreEqual(state.arrayOfNumbers[2], 20);
		Assert.AreEqual(state.arrayOfNumbers[3], 3520);

		Assert.AreEqual(state.arrayOfStrings.Count, 3);
		Assert.AreEqual(state.arrayOfStrings[0], "one");
		Assert.AreEqual(state.arrayOfStrings[1], "two");
		Assert.AreEqual(state.arrayOfStrings[2], "three");

		Assert.AreEqual(state.arrayOfInt32.Count, 3);
		Assert.AreEqual(state.arrayOfInt32[0], 1000);
		Assert.AreEqual(state.arrayOfInt32[1], 3520);
		Assert.AreEqual(state.arrayOfInt32[2], -3000);

		state.arrayOfSchemas.OnRemove += (value, key) => Debug.Log("onRemove, arrayOfSchemas => " + key);
		state.arrayOfNumbers.OnRemove += (value, key) => Debug.Log("onRemove, arrayOfNumbers => " + key);
		state.arrayOfStrings.OnRemove += (value, key) => Debug.Log("onRemove, arrayOfStrings => " + key);
		state.arrayOfInt32.OnRemove += (value, key) => Debug.Log("onRemove, arrayOfInt32 => " + key);

		byte[] popBytes = { 0, 1, 0, 1, 1, 0, 3, 1, 0, 2, 1, 0 };
		state.Decode(popBytes);

		Assert.AreEqual(state.arrayOfSchemas.Count, 1);
		Assert.AreEqual(state.arrayOfNumbers.Count, 1);
		Assert.AreEqual(state.arrayOfStrings.Count, 1);
		Assert.AreEqual(state.arrayOfInt32.Count, 1);
		Debug.Log("FINISHED");
	}

	[Test]
	public void MapSchemaTypesTest()
	{
		var state = new SchemaTest.MapSchemaTypes.MapSchemaTypes();
		byte[] bytes = { 0, 3, 163, 111, 110, 101, 0, 100, 1, 204, 200, 193, 163, 116, 119, 111, 0, 205, 44, 1, 1, 205, 144, 1, 193, 165, 116, 104, 114, 101, 101, 0, 205, 244, 1, 1, 205, 88, 2, 193, 1, 3, 163, 111, 110, 101, 1, 163, 116, 119, 111, 2, 165, 116, 104, 114, 101, 101, 205, 192, 13, 2, 3, 163, 111, 110, 101, 163, 79, 110, 101, 163, 116, 119, 111, 163, 84, 119, 111, 165, 116, 104, 114, 101, 101, 165, 84, 104, 114, 101, 101, 3, 3, 163, 111, 110, 101, 192, 13, 0, 0, 163, 116, 119, 111, 24, 252, 255, 255, 165, 116, 104, 114, 101, 101, 208, 7, 0, 0 };

		state.mapOfSchemas.OnAdd += (value, key) => Debug.Log("OnAdd, mapOfSchemas => " + key);
		state.mapOfNumbers.OnAdd += (value, key) => Debug.Log("OnAdd, mapOfNumbers => " + key);
		state.mapOfStrings.OnAdd += (value, key) => Debug.Log("OnAdd, mapOfStrings => " + key);
		state.mapOfInt32.OnAdd += (value, key) => Debug.Log("OnAdd, mapOfInt32 => " + key);

		state.mapOfSchemas.OnRemove += (value, key) => Debug.Log("OnRemove, mapOfSchemas => " + key);
		state.mapOfNumbers.OnRemove += (value, key) => Debug.Log("OnRemove, mapOfNumbers => " + key);
		state.mapOfStrings.OnRemove += (value, key) => Debug.Log("OnRemove, mapOfStrings => " + key);
		state.mapOfInt32.OnRemove += (value, key) => Debug.Log("OnRemove, mapOfInt32 => " + key);

		state.Decode(bytes);

		Assert.AreEqual(state.mapOfSchemas.Count, 3);
		Assert.AreEqual(state.mapOfSchemas["one"].x, 100);
		Assert.AreEqual(state.mapOfSchemas["one"].y, 200);
		Assert.AreEqual(state.mapOfSchemas["two"].x, 300);
		Assert.AreEqual(state.mapOfSchemas["two"].y, 400);
		Assert.AreEqual(state.mapOfSchemas["three"].x, 500);
		Assert.AreEqual(state.mapOfSchemas["three"].y, 600);

		Assert.AreEqual(state.mapOfNumbers.Count, 3);
		Assert.AreEqual(state.mapOfNumbers["one"], 1);
		Assert.AreEqual(state.mapOfNumbers["two"], 2);
		Assert.AreEqual(state.mapOfNumbers["three"], 3520);

		Assert.AreEqual(state.mapOfStrings.Count, 3);
		Assert.AreEqual(state.mapOfStrings["one"], "One");
		Assert.AreEqual(state.mapOfStrings["two"], "Two");
		Assert.AreEqual(state.mapOfStrings["three"], "Three");

		Assert.AreEqual(state.mapOfInt32.Count, 3);
		Assert.AreEqual(state.mapOfInt32["one"], 3520);
		Assert.AreEqual(state.mapOfInt32["two"], -1000);
		Assert.AreEqual(state.mapOfInt32["three"], 2000);

		byte[] deleteBytes = { 1, 2, 192, 1, 192, 2, 0, 2, 192, 1, 192, 2, 2, 2, 192, 1, 192, 2, 3, 2, 192, 1, 192, 2 };
		state.Decode(deleteBytes);

		Assert.AreEqual(state.mapOfSchemas.Count, 1);
		Assert.AreEqual(state.mapOfNumbers.Count, 1);
		Assert.AreEqual(state.mapOfStrings.Count, 1);
		Assert.AreEqual(state.mapOfInt32.Count, 1);
	}

	[Test]
	public void MapSchemaInt8Test()
	{
		var state = new SchemaTest.MapSchemaInt8.MapSchemaInt8();
		byte[] bytes = { 0, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 1, 6, 163, 98, 98, 98, 1, 163, 97, 97, 97, 1, 163, 50, 50, 49, 1, 163, 48, 50, 49, 1, 162, 49, 53, 1, 162, 49, 48, 1 };

		state.Decode(bytes);

		Assert.AreEqual(state.status, "Hello world");
		Assert.AreEqual(state.mapOfInt8["bbb"], 1);
		Assert.AreEqual(state.mapOfInt8["aaa"], 1);
		Assert.AreEqual(state.mapOfInt8["221"], 1);
		Assert.AreEqual(state.mapOfInt8["021"], 1);
		Assert.AreEqual(state.mapOfInt8["15"], 1);
		Assert.AreEqual(state.mapOfInt8["10"], 1);

		byte[] addBytes = { 1, 1, 5, 2 };
		state.Decode(addBytes);

		Assert.AreEqual(state.mapOfInt8["bbb"], 1);
		Assert.AreEqual(state.mapOfInt8["aaa"], 1);
		Assert.AreEqual(state.mapOfInt8["221"], 1);
		Assert.AreEqual(state.mapOfInt8["021"], 1);
		Assert.AreEqual(state.mapOfInt8["15"], 1);
		Assert.AreEqual(state.mapOfInt8["10"], 2);
	}

	[Test]
	public void InheritedTypesTest()
	{
		var serializer = new Colyseus.SchemaSerializer<SchemaTest.InheritedTypes.InheritedTypes>();
		byte[] handshake = { 0, 4, 4, 0, 0, 0, 1, 2, 2, 0, 0, 161, 120, 1, 166, 110, 117, 109, 98, 101, 114, 193, 1, 0, 161, 121, 1, 166, 110, 117, 109, 98, 101, 114, 193, 193, 1, 0, 1, 1, 3, 3, 0, 0, 161, 120, 1, 166, 110, 117, 109, 98, 101, 114, 193, 1, 0, 161, 121, 1, 166, 110, 117, 109, 98, 101, 114, 193, 2, 0, 164, 110, 97, 109, 101, 1, 166, 115, 116, 114, 105, 110, 103, 193, 193, 2, 0, 2, 1, 4, 4, 0, 0, 161, 120, 1, 166, 110, 117, 109, 98, 101, 114, 193, 1, 0, 161, 121, 1, 166, 110, 117, 109, 98, 101, 114, 193, 2, 0, 164, 110, 97, 109, 101, 1, 166, 115, 116, 114, 105, 110, 103, 193, 3, 0, 165, 112, 111, 119, 101, 114, 1, 166, 110, 117, 109, 98, 101, 114, 193, 193, 3, 0, 3, 1, 4, 4, 0, 0, 166, 101, 110, 116, 105, 116, 121, 1, 163, 114, 101, 102, 2, 0, 193, 1, 0, 166, 112, 108, 97, 121, 101, 114, 1, 163, 114, 101, 102, 2, 1, 193, 2, 0, 163, 98, 111, 116, 1, 163, 114, 101, 102, 2, 2, 193, 3, 0, 163, 97, 110, 121, 1, 163, 114, 101, 102, 2, 0, 193, 193, 1, 3 };
		serializer.Handshake(handshake, 0);

		byte[] bytes = { 0, 0, 205, 244, 1, 1, 205, 32, 3, 193, 1, 0, 204, 200, 1, 205, 44, 1, 2, 166, 80, 108, 97, 121, 101, 114, 193, 2, 0, 100, 1, 204, 150, 2, 163, 66, 111, 116, 3, 204, 200, 193, 3, 213, 2, 3, 100, 193 };
		serializer.SetState(bytes);

		var state = serializer.GetState();

		Assert.IsInstanceOf(typeof(SchemaTest.InheritedTypes.Entity), state.entity);
		Assert.AreEqual(state.entity.x, 500);
		Assert.AreEqual(state.entity.y, 800);

		Assert.IsInstanceOf(typeof(SchemaTest.InheritedTypes.Player), state.player);
		Assert.AreEqual(state.player.x, 200);
		Assert.AreEqual(state.player.y, 300);
		Assert.AreEqual(state.player.name, "Player");

		Assert.IsInstanceOf(typeof(SchemaTest.InheritedTypes.Bot), state.bot);
		Assert.AreEqual(state.bot.x, 100);
		Assert.AreEqual(state.bot.y, 150);
		Assert.AreEqual(state.bot.name, "Bot");
		Assert.AreEqual(state.bot.power, 200);

		Assert.IsInstanceOf(typeof(SchemaTest.InheritedTypes.Bot), state.any);
	}

	[Test]
	public void BackwardsForwardsTest()
	{
		byte[] statev1bytes = { 1, 1, 163, 111, 110, 101, 0, 203, 64, 45, 212, 207, 108, 69, 148, 63, 1, 203, 120, 56, 150, 252, 58, 73, 224, 63, 193, 0, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100 };
		byte[] statev2bytes = { 0, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 2, 10 };

		var statev2 = new SchemaTest.Forwards.StateV2();
		statev2.Decode(statev1bytes);
		Assert.AreEqual(statev2.str, "Hello world");

		var statev1 = new SchemaTest.Backwards.StateV1();
		statev1.Decode(statev2bytes);
		Assert.AreEqual(statev1.str, "Hello world");

		/*
		Assert.DoesNotThrow(() =>
		{
			// uses StateV1 handshake with StateV2 structure.
			var serializer = new Colyseus.SchemaSerializer<SchemaTest.Forwards.StateV2>();
			byte[] handshake = { 0, 4, 4, 0, 0, 0, 1, 2, 2, 0, 0, 161, 120, 1, 166, 110, 117, 109, 98, 101, 114, 193, 1, 0, 161, 121, 1, 166, 110, 117, 109, 98, 101, 114, 193, 193, 1, 0, 1, 1, 2, 2, 0, 0, 163, 115, 116, 114, 1, 166, 115, 116, 114, 105, 110, 103, 193, 1, 0, 163, 109, 97, 112, 1, 163, 109, 97, 112, 2, 0, 193, 193, 2, 0, 2, 1, 4, 4, 0, 0, 161, 120, 1, 166, 110, 117, 109, 98, 101, 114, 193, 1, 0, 161, 121, 1, 166, 110, 117, 109, 98, 101, 114, 193, 2, 0, 164, 110, 97, 109, 101, 1, 166, 115, 116, 114, 105, 110, 103, 193, 3, 0, 174, 97, 114, 114, 97, 121, 79, 102, 83, 116, 114, 105, 110, 103, 115, 1, 172, 97, 114, 114, 97, 121, 58, 115, 116, 114, 105, 110, 103, 2, 255, 193, 193, 3, 0, 3, 1, 3, 3, 0, 0, 163, 115, 116, 114, 1, 166, 115, 116, 114, 105, 110, 103, 193, 1, 0, 163, 109, 97, 112, 1, 163, 109, 97, 112, 2, 2, 193, 2, 0, 169, 99, 111, 117, 110, 116, 100, 111, 119, 110, 1, 166, 110, 117, 109, 98, 101, 114, 193, 193, 1, 1 };
			serializer.Handshake(handshake, 0);
		}, "reflection should be backwards compatible");

		Assert.DoesNotThrow(() =>
		{
			// uses StateV2 handshake with StateV1 structure.
			var serializer = new Colyseus.SchemaSerializer<SchemaTest.Backwards.StateV1>();
			byte[] handshake = { 0, 4, 4, 0, 0, 0, 1, 2, 2, 0, 0, 161, 120, 1, 166, 110, 117, 109, 98, 101, 114, 193, 1, 0, 161, 121, 1, 166, 110, 117, 109, 98, 101, 114, 193, 193, 1, 0, 1, 1, 2, 2, 0, 0, 163, 115, 116, 114, 1, 166, 115, 116, 114, 105, 110, 103, 193, 1, 0, 163, 109, 97, 112, 1, 163, 109, 97, 112, 2, 0, 193, 193, 2, 0, 2, 1, 4, 4, 0, 0, 161, 120, 1, 166, 110, 117, 109, 98, 101, 114, 193, 1, 0, 161, 121, 1, 166, 110, 117, 109, 98, 101, 114, 193, 2, 0, 164, 110, 97, 109, 101, 1, 166, 115, 116, 114, 105, 110, 103, 193, 3, 0, 174, 97, 114, 114, 97, 121, 79, 102, 83, 116, 114, 105, 110, 103, 115, 1, 172, 97, 114, 114, 97, 121, 58, 115, 116, 114, 105, 110, 103, 2, 255, 193, 193, 3, 0, 3, 1, 3, 3, 0, 0, 163, 115, 116, 114, 1, 166, 115, 116, 114, 105, 110, 103, 193, 1, 0, 163, 109, 97, 112, 1, 163, 109, 97, 112, 2, 2, 193, 2, 0, 169, 99, 111, 117, 110, 116, 100, 111, 119, 110, 1, 166, 110, 117, 109, 98, 101, 114, 193, 193, 1, 3 };
			serializer.Handshake(handshake, 0);
		}, "reflection should be forwards compatible");
		*/
	}

}