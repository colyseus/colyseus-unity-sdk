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
		byte[] bytes = { 128, 128, 129, 255, 130, 0, 128, 131, 255, 255, 132, 0, 0, 0, 128, 133, 255, 255, 255, 255, 134, 0, 0, 0, 0, 0, 0, 0, 128, 135, 255, 255, 255, 255, 255, 255, 31, 0, 136, 204, 204, 204, 253, 137, 255, 255, 255, 255, 255, 255, 239, 127, 138, 208, 128, 139, 204, 255, 140, 209, 0, 128, 141, 205, 255, 255, 142, 210, 0, 0, 0, 128, 143, 203, 0, 0, 224, 255, 255, 255, 239, 65, 144, 203, 0, 0, 0, 0, 0, 0, 224, 195, 145, 203, 255, 255, 255, 255, 255, 255, 63, 67, 146, 203, 61, 255, 145, 224, 255, 255, 239, 199, 147, 203, 153, 153, 153, 153, 153, 153, 185, 127, 148, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 149, 1 };
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
		byte[] bytes = { 128, 1, 129, 2, 255, 1, 128, 205, 244, 1, 129, 205, 32, 3, 255, 2, 128, 204, 200, 129, 205, 44, 1 };
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
		byte[] bytes = { 128, 1, 129, 2, 130, 3, 131, 4, 255, 1, 128, 0, 5, 128, 1, 6, 255, 2, 128, 0, 0, 128, 1, 10, 128, 2, 20, 128, 3, 205, 192, 13, 255, 3, 128, 0, 163, 111, 110, 101, 128, 1, 163, 116, 119, 111, 128, 2, 165, 116, 104, 114, 101, 101, 255, 4, 128, 0, 232, 3, 0, 0, 128, 1, 192, 13, 0, 0, 128, 2, 72, 244, 255, 255, 255, 5, 128, 100, 129, 208, 156, 255, 6, 128, 100, 129, 208, 156 };

		state.arrayOfSchemas.OnAdd((value, key) => Debug.Log("onAdd, arrayOfSchemas => " + key));
		state.arrayOfNumbers.OnAdd((value, key) => Debug.Log("onAdd, arrayOfNumbers => " + key));
		state.arrayOfStrings.OnAdd((value, key) => Debug.Log("onAdd, arrayOfStrings => " + key));
		state.arrayOfInt32.OnAdd((value, key) => Debug.Log("onAdd, arrayOfInt32 => " + key));

		var refs = new Colyseus.Schema.ColyseusReferenceTracker();
		state.Decode(bytes, null, refs);

		Debug.Log("Decoded 1st time!");

		Assert.AreEqual(2, state.arrayOfSchemas.Count);
		Assert.AreEqual(100, state.arrayOfSchemas[0].x);
		Assert.AreEqual(-100, state.arrayOfSchemas[0].y);
		Assert.AreEqual(100, state.arrayOfSchemas[1].x);
		Assert.AreEqual(-100, state.arrayOfSchemas[1].y);

		Assert.AreEqual(4, state.arrayOfNumbers.Count);
		Assert.AreEqual(0, state.arrayOfNumbers[0]);
		Assert.AreEqual(10, state.arrayOfNumbers[1]);
		Assert.AreEqual(20, state.arrayOfNumbers[2]);
		Assert.AreEqual(3520, state.arrayOfNumbers[3]);

		Assert.AreEqual(3, state.arrayOfStrings.Count);
		Assert.AreEqual("one", state.arrayOfStrings[0]);
		Assert.AreEqual("two", state.arrayOfStrings[1]);
		Assert.AreEqual("three", state.arrayOfStrings[2]);

		Assert.AreEqual(3, state.arrayOfInt32.Count);
		Assert.AreEqual(1000, state.arrayOfInt32[0]);
		Assert.AreEqual(3520, state.arrayOfInt32[1]);
		Assert.AreEqual(-3000, state.arrayOfInt32[2]);

		state.arrayOfSchemas.OnRemove((value, key) => Debug.Log("onRemove, arrayOfSchemas => " + key));
		state.arrayOfNumbers.OnRemove((value, key) => Debug.Log("onRemove, arrayOfNumbers => " + key));
		state.arrayOfStrings.OnRemove((value, key) => Debug.Log("onRemove, arrayOfStrings => " + key));
		state.arrayOfInt32.OnRemove((value, key) => Debug.Log("onRemove, arrayOfInt32 => " + key));

		byte[] popBytes = { 255, 1, 64, 1, 255, 2, 64, 3, 64, 2, 64, 1, 255, 4, 64, 2, 64, 1, 255, 3, 64, 2, 64, 1 };
		state.Decode(popBytes, null, refs);
		Debug.Log("Decoded 2nd time!");

		Assert.AreEqual(1, state.arrayOfSchemas.Count);
		Assert.AreEqual(1, state.arrayOfNumbers.Count);
		Assert.AreEqual(1, state.arrayOfStrings.Count);
		Assert.AreEqual(1, state.arrayOfInt32.Count);
		Debug.Log("FINISHED");
	}

	[Test]
	public void ArraySchemaClearTest()
	{
		var state = new SchemaTest.ArraySchemaClear.ArraySchemaClear();
		var refs = new Colyseus.Schema.ColyseusReferenceTracker();
		int onAddCount = 0;
		int onRemoveCount = 0;
		int onChangeCount = 0;

		state.items.OnAdd((value, key) => onAddCount++);
		state.items.OnRemove((value, key) => onRemoveCount++);
		state.items.OnChange((value, key) => onChangeCount++);

		byte[] bytes = { 128, 1, 255, 1, 128, 0, 1, 128, 1, 2, 128, 2, 3, 128, 3, 4, 128, 4, 5 };
		state.Decode(bytes, null, refs);

		Assert.AreEqual(5, onAddCount);
		Assert.AreEqual(0, onRemoveCount);
		Assert.AreEqual(5, onChangeCount);

		byte[] clearBytes = { 255, 1, 10 };
		state.Decode(clearBytes, null, refs);

		Assert.AreEqual(5, onAddCount);
		Assert.AreEqual(5, onRemoveCount);
		Assert.AreEqual(5, onChangeCount);

		byte[] reAddBytes = { 255, 1, 128, 5, 1, 128, 6, 2, 128, 7, 3, 128, 8, 4, 128, 9, 5 };
		state.Decode(reAddBytes, null, refs);

		Assert.AreEqual(10, onAddCount);
		Assert.AreEqual(5, onRemoveCount);
		Assert.AreEqual(10, onChangeCount);
	}

	[Test]
	public void MapSchemaTypesTest()
	{
		var state = new SchemaTest.MapSchemaTypes.MapSchemaTypes();
		byte[] bytes = { 128, 1, 129, 2, 130, 3, 131, 4, 255, 1, 128, 0, 163, 111, 110, 101, 5, 128, 1, 163, 116, 119, 111, 6, 128, 2, 165, 116, 104, 114, 101, 101, 7, 255, 2, 128, 0, 163, 111, 110, 101, 1, 128, 1, 163, 116, 119, 111, 2, 128, 2, 165, 116, 104, 114, 101, 101, 205, 192, 13, 255, 3, 128, 0, 163, 111, 110, 101, 163, 79, 110, 101, 128, 1, 163, 116, 119, 111, 163, 84, 119, 111, 128, 2, 165, 116, 104, 114, 101, 101, 165, 84, 104, 114, 101, 101, 255, 4, 128, 0, 163, 111, 110, 101, 192, 13, 0, 0, 128, 1, 163, 116, 119, 111, 24, 252, 255, 255, 128, 2, 165, 116, 104, 114, 101, 101, 208, 7, 0, 0, 255, 5, 128, 100, 129, 204, 200, 255, 6, 128, 205, 44, 1, 129, 205, 144, 1, 255, 7, 128, 205, 244, 1, 129, 205, 88, 2 };

		state.mapOfSchemas.OnAdd((value, key) => Debug.Log("OnAdd, mapOfSchemas => " + key));
		state.mapOfNumbers.OnAdd((value, key) => Debug.Log("OnAdd, mapOfNumbers => " + key));
		state.mapOfStrings.OnAdd((value, key) => Debug.Log("OnAdd, mapOfStrings => " + key));
		state.mapOfInt32.OnAdd((value, key) => Debug.Log("OnAdd, mapOfInt32 => " + key));

		state.mapOfSchemas.OnRemove((value, key) => Debug.Log("OnRemove, mapOfSchemas => " + key));
		state.mapOfNumbers.OnRemove((value, key) => Debug.Log("OnRemove, mapOfNumbers => " + key));
		state.mapOfStrings.OnRemove((value, key) => Debug.Log("OnRemove, mapOfStrings => " + key));
		state.mapOfInt32.OnRemove((value, key) => Debug.Log("OnRemove, mapOfInt32 => " + key));

		var refs = new Colyseus.Schema.ColyseusReferenceTracker();
		state.Decode(bytes, null, refs);

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

		byte[] deleteBytes = { 255, 2, 64, 1, 64, 2, 255, 1, 64, 1, 64, 2, 255, 3, 64, 1, 64, 2, 255, 4, 64, 1, 64, 2 };
		state.Decode(deleteBytes, null, refs);

		Assert.AreEqual(state.mapOfSchemas.Count, 1);
		Assert.AreEqual(state.mapOfNumbers.Count, 1);
		Assert.AreEqual(state.mapOfStrings.Count, 1);
		Assert.AreEqual(state.mapOfInt32.Count, 1);
	}

	[Test]
	public void MapSchemaInt8Test()
	{
		var state = new SchemaTest.MapSchemaInt8.MapSchemaInt8();
		byte[] bytes = { 128, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 129, 1, 255, 1, 128, 0, 163, 98, 98, 98, 1, 128, 1, 163, 97, 97, 97, 1, 128, 2, 163, 50, 50, 49, 1, 128, 3, 163, 48, 50, 49, 1, 128, 4, 162, 49, 53, 1, 128, 5, 162, 49, 48, 1 };

		var refs = new Colyseus.Schema.ColyseusReferenceTracker();
		state.Decode(bytes, null, refs);

		Assert.AreEqual(state.status, "Hello world");
		Assert.AreEqual(state.mapOfInt8["bbb"], 1);
		Assert.AreEqual(state.mapOfInt8["aaa"], 1);
		Assert.AreEqual(state.mapOfInt8["221"], 1);
		Assert.AreEqual(state.mapOfInt8["021"], 1);
		Assert.AreEqual(state.mapOfInt8["15"], 1);
		Assert.AreEqual(state.mapOfInt8["10"], 1);

		byte[] addBytes = { 255, 1, 0, 5, 2 };
		state.Decode(addBytes, null, refs);

		Assert.AreEqual(state.mapOfInt8["bbb"], 1);
		Assert.AreEqual(state.mapOfInt8["aaa"], 1);
		Assert.AreEqual(state.mapOfInt8["221"], 1);
		Assert.AreEqual(state.mapOfInt8["021"], 1);
		Assert.AreEqual(state.mapOfInt8["15"], 1);
		Assert.AreEqual(state.mapOfInt8["10"], 2);
	}

	[Test]
	public void MapSchemaMoveNullifyTypeTest()
	{
		var state = new SchemaTest.MapSchemaMoveNullifyType.State();
		byte[] bytes = { 129, 1, 64, 255, 1, 128, 0, 161, 48, 0 };

		var refs = new Colyseus.Schema.ColyseusReferenceTracker();
		state.Decode(bytes, null, refs);

		Assert.DoesNotThrow(() =>
		{
			// FIXME: this test only passes because empty 
			byte[] moveAndNullifyBytes = { 128, 1, 65 };
			state.Decode(moveAndNullifyBytes, null, refs);
		});
	}

	[Test]
	public void InheritedTypesTest()
	{
		var serializer = new Colyseus.ColyseusSchemaSerializer<SchemaTest.InheritedTypes.InheritedTypes>();
		byte[] handshake = { 128, 1, 129, 3, 255, 1, 128, 0, 2, 128, 1, 3, 128, 2, 4, 128, 3, 5, 255, 2, 129, 6, 128, 0, 255, 3, 129, 7, 128, 1, 255, 4, 129, 8, 128, 2, 255, 5, 129, 9, 128, 3, 255, 6, 128, 0, 10, 128, 1, 11, 255, 7, 128, 0, 12, 128, 1, 13, 128, 2, 14, 255, 8, 128, 0, 15, 128, 1, 16, 128, 2, 17, 128, 3, 18, 255, 9, 128, 0, 19, 128, 1, 20, 128, 2, 21, 128, 3, 22, 255, 10, 128, 161, 120, 129, 166, 110, 117, 109, 98, 101, 114, 255, 11, 128, 161, 121, 129, 166, 110, 117, 109, 98, 101, 114, 255, 12, 128, 161, 120, 129, 166, 110, 117, 109, 98, 101, 114, 255, 13, 128, 161, 121, 129, 166, 110, 117, 109, 98, 101, 114, 255, 14, 128, 164, 110, 97, 109, 101, 129, 166, 115, 116, 114, 105, 110, 103, 255, 15, 128, 161, 120, 129, 166, 110, 117, 109, 98, 101, 114, 255, 16, 128, 161, 121, 129, 166, 110, 117, 109, 98, 101, 114, 255, 17, 128, 164, 110, 97, 109, 101, 129, 166, 115, 116, 114, 105, 110, 103, 255, 18, 128, 165, 112, 111, 119, 101, 114, 129, 166, 110, 117, 109, 98, 101, 114, 255, 19, 128, 166, 101, 110, 116, 105, 116, 121, 130, 0, 129, 163, 114, 101, 102, 255, 20, 128, 166, 112, 108, 97, 121, 101, 114, 130, 1, 129, 163, 114, 101, 102, 255, 21, 128, 163, 98, 111, 116, 130, 2, 129, 163, 114, 101, 102, 255, 22, 128, 163, 97, 110, 121, 130, 0, 129, 163, 114, 101, 102 };
		serializer.Handshake(handshake, 0);

		byte[] bytes = { 128, 1, 129, 2, 130, 3, 131, 4, 213, 2, 255, 1, 128, 205, 244, 1, 129, 205, 32, 3, 255, 2, 128, 204, 200, 129, 205, 44, 1, 130, 166, 80, 108, 97, 121, 101, 114, 255, 3, 128, 100, 129, 204, 150, 130, 163, 66, 111, 116, 131, 204, 200, 255, 4, 131, 100 };
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
		byte[] statev1bytes = { 129, 1, 128, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 255, 1, 128, 0, 163, 111, 110, 101, 2, 255, 2, 128, 203, 232, 229, 22, 37, 231, 231, 209, 63, 129, 203, 240, 138, 15, 5, 219, 40, 223, 63 };
		byte[] statev2bytes = { 128, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 130, 10 };

		var statev2 = new SchemaTest.BackwardsForwards.StateV2();
		statev2.Decode(statev1bytes);
		Assert.AreEqual(statev2.str, "Hello world");

		var statev1 = new SchemaTest.BackwardsForwards.StateV1();
		statev1.Decode(statev2bytes);
		Assert.AreEqual(statev1.str, "Hello world");

		/*
		Assert.DoesNotThrow(() =>
		{
			// uses StateV1 handshake with StateV2 structure.
			var serializer = new Colyseus.SchemaSerializer<SchemaTest.BackwardsForwards.StateV2>();
			byte[] handshake = { 128, 1, 129, 1, 255, 1, 128, 0, 0, 2, 128, 1, 1, 3, 128, 2, 2, 4, 128, 3, 3, 5, 255, 2, 129, 6, 128, 0, 255, 3, 129, 7, 128, 1, 255, 4, 129, 8, 128, 2, 255, 5, 129, 9, 128, 3, 255, 6, 128, 0, 0, 10, 128, 1, 1, 11, 255, 7, 128, 0, 0, 12, 128, 1, 1, 13, 255, 8, 128, 0, 0, 14, 128, 1, 1, 15, 128, 2, 2, 16, 128, 3, 3, 17, 255, 9, 128, 0, 0, 18, 128, 1, 1, 19, 128, 2, 2, 20, 255, 10, 128, 161, 120, 129, 166, 110, 117, 109, 98, 101, 114, 255, 11, 128, 161, 121, 129, 166, 110, 117, 109, 98, 101, 114, 255, 12, 128, 163, 115, 116, 114, 129, 166, 115, 116, 114, 105, 110, 103, 255, 13, 128, 163, 109, 97, 112, 130, 0, 129, 163, 109, 97, 112, 255, 14, 128, 161, 120, 129, 166, 110, 117, 109, 98, 101, 114, 255, 15, 128, 161, 121, 129, 166, 110, 117, 109, 98, 101, 114, 255, 16, 128, 164, 110, 97, 109, 101, 129, 166, 115, 116, 114, 105, 110, 103, 255, 17, 128, 174, 97, 114, 114, 97, 121, 79, 102, 83, 116, 114, 105, 110, 103, 115, 130, 255, 129, 172, 97, 114, 114, 97, 121, 58, 115, 116, 114, 105, 110, 103, 255, 18, 128, 163, 115, 116, 114, 129, 166, 115, 116, 114, 105, 110, 103, 255, 19, 128, 163, 109, 97, 112, 130, 2, 129, 163, 109, 97, 112, 255, 20, 128, 169, 99, 111, 117, 110, 116, 100, 111, 119, 110, 129, 166, 110, 117, 109, 98, 101, 114 };
			serializer.Handshake(handshake, 0);
		}, "reflection should be backwards compatible");

		Assert.DoesNotThrow(() =>
		{
			// uses StateV2 handshake with StateV1 structure.
			var serializer = new Colyseus.SchemaSerializer<SchemaTest.BackwardsForwards.StateV1>();
			byte[] handshake = { 128, 1, 129, 3, 255, 1, 128, 0, 0, 2, 128, 1, 1, 3, 128, 2, 2, 4, 128, 3, 3, 5, 255, 2, 129, 6, 128, 0, 255, 3, 129, 7, 128, 1, 255, 4, 129, 8, 128, 2, 255, 5, 129, 9, 128, 3, 255, 6, 128, 0, 0, 10, 128, 1, 1, 11, 255, 7, 128, 0, 0, 12, 128, 1, 1, 13, 255, 8, 128, 0, 0, 14, 128, 1, 1, 15, 128, 2, 2, 16, 128, 3, 3, 17, 255, 9, 128, 0, 0, 18, 128, 1, 1, 19, 128, 2, 2, 20, 255, 10, 128, 161, 120, 129, 166, 110, 117, 109, 98, 101, 114, 255, 11, 128, 161, 121, 129, 166, 110, 117, 109, 98, 101, 114, 255, 12, 128, 163, 115, 116, 114, 129, 166, 115, 116, 114, 105, 110, 103, 255, 13, 128, 163, 109, 97, 112, 130, 0, 129, 163, 109, 97, 112, 255, 14, 128, 161, 120, 129, 166, 110, 117, 109, 98, 101, 114, 255, 15, 128, 161, 121, 129, 166, 110, 117, 109, 98, 101, 114, 255, 16, 128, 164, 110, 97, 109, 101, 129, 166, 115, 116, 114, 105, 110, 103, 255, 17, 128, 174, 97, 114, 114, 97, 121, 79, 102, 83, 116, 114, 105, 110, 103, 115, 130, 255, 129, 172, 97, 114, 114, 97, 121, 58, 115, 116, 114, 105, 110, 103, 255, 18, 128, 163, 115, 116, 114, 129, 166, 115, 116, 114, 105, 110, 103, 255, 19, 128, 163, 109, 97, 112, 130, 2, 129, 163, 109, 97, 112, 255, 20, 128, 169, 99, 111, 117, 110, 116, 100, 111, 119, 110, 129, 166, 110, 117, 109, 98, 101, 114 };
			serializer.Handshake(handshake, 0);
		}, "reflection should be forwards compatible");
		*/
	}

	[Test]
	public void FilteredTypesTest()
	{
		var client1 = new SchemaTest.FilteredTypes.State();
		client1.Decode(new byte[] { 255, 0, 130, 1, 128, 2, 128, 2, 255, 1, 128, 0, 4, 255, 2, 128, 163, 111, 110, 101, 255, 2, 128, 163, 111, 110, 101, 255, 4, 128, 163, 111, 110, 101 });
		Assert.AreEqual("one", client1.playerOne.name);
		Assert.AreEqual("one", client1.players[0].name);
		Assert.AreEqual(null, client1.playerTwo.name);

		var client2 = new SchemaTest.FilteredTypes.State();
		client2.Decode(new byte[] { 255, 0, 130, 1, 129, 3, 129, 3, 255, 1, 128, 1, 5, 255, 3, 128, 163, 116, 119, 111, 255, 3, 128, 163, 116, 119, 111, 255, 5, 128, 163, 116, 119, 111 });
		Assert.AreEqual("two", client2.playerTwo.name);
		Assert.AreEqual("two", client2.players[0].name);
		Assert.AreEqual(null, client2.playerOne.name);
	}

	[Test]
	public void InstanceSharingTypesTest()
	{
		var refs = new Colyseus.Schema.ColyseusReferenceTracker();
		var client = new SchemaTest.InstanceSharingTypes.State();

		client.Decode(new byte[] { 130, 1, 131, 2, 128, 3, 129, 3, 255, 1, 255, 2, 255, 3, 128, 4, 255, 3, 128, 4, 255, 4, 128, 10, 129, 10, 255, 4, 128, 10, 129, 10 }, null, refs);
		Assert.AreEqual(client.player1, client.player2);
		Assert.AreEqual(client.player1.position, client.player2.position);
		Assert.AreEqual(refs.refCounts[client.player1.__refId], 2);
		Assert.AreEqual(5, refs.refs.Count);

		client.Decode(new byte[] { 130, 1, 131, 2, 64, 65 }, null, refs);
		Assert.AreEqual(null, client.player2);
		Assert.AreEqual(null, client.player2);
		Assert.AreEqual(3, refs.refs.Count);

		client.Decode(new byte[] { 255, 1, 128, 0, 5, 128, 1, 5, 128, 2, 5, 128, 3, 6, 255, 5, 128, 7, 255, 6, 128, 8, 255, 7, 128, 10, 129, 10, 255, 8, 128, 10, 129, 10 }, null, refs);
		Assert.AreEqual(client.arrayOfPlayers[0], client.arrayOfPlayers[1]);
		Assert.AreEqual(client.arrayOfPlayers[1], client.arrayOfPlayers[2]);
		Assert.AreNotEqual(client.arrayOfPlayers[2], client.arrayOfPlayers[3]);
		Assert.AreEqual(7, refs.refs.Count);

		client.Decode(new byte[] { 255, 1, 64, 3, 64, 2, 64, 1 }, null, refs);
		Assert.AreEqual(1, client.arrayOfPlayers.Count);
		Assert.AreEqual(5, refs.refs.Count);
		var previousArraySchemaRefId = client.arrayOfPlayers.__refId;

		// Replacing ArraySchema
		client.Decode(new byte[] { 130, 9, 255, 9, 128, 0, 10, 255, 10, 128, 11, 255, 11, 128, 10, 129, 20 }, null, refs);
		Assert.AreEqual(false, refs.refs.ContainsKey(previousArraySchemaRefId));
		Assert.AreEqual(1, client.arrayOfPlayers.Count);
		Assert.AreEqual(5, refs.refs.Count);

		// Clearing ArraySchema
		client.Decode(new byte[] { 255, 9, 10 }, null, refs);
		Assert.AreEqual(0, client.arrayOfPlayers.Count);
		Assert.AreEqual(3, refs.refs.Count);

	}

}