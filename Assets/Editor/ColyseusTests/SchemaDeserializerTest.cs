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
		byte[] bytes = { 0, 128, 1, 255, 2, 0, 128, 3, 255, 255, 4, 0, 0, 0, 128, 5, 255, 255, 255, 255, 6, 0, 0, 0, 0, 0, 0, 0, 128, 7, 255, 255, 255, 255, 255, 255, 31, 0, 8, 255, 255, 127, 255, 9, 255, 255, 255, 255, 255, 255, 239, 127, 10, 208, 128, 11, 204, 255, 12, 209, 0, 128, 13, 205, 255, 255, 14, 210, 0, 0, 0, 128, 15, 203, 0, 0, 224, 255, 255, 255, 239, 65, 16, 203, 0, 0, 0, 0, 0, 0, 224, 195, 17, 203, 255, 255, 255, 255, 255, 255, 63, 67, 18, 203, 61, 255, 145, 224, 255, 255, 239, 199, 19, 203, 255, 255, 255, 255, 255, 255, 239, 127, 20, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 21, 1 };
		state.Decode(bytes);

		Assert.AreEqual(state.int8, -128);
		Assert.AreEqual(state.uint8, 255);
		Assert.AreEqual(state.int16, -32768);
		Assert.AreEqual(state.uint16, 65535);
		Assert.AreEqual(state.int32, -2147483648);
		Assert.AreEqual(state.uint32, 4294967295);
		Assert.AreEqual(state.int64, -9223372036854775808);
		Assert.AreEqual(state.uint64, 9007199254740991);
		Assert.AreEqual(state.float32, -3.40282347E+38f);
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
		byte[] bytes = { 0, 2, 2, 0, 0, 100, 1, 208, 156, 193, 1, 0, 100, 1, 208, 156, 193, 1, 4, 4, 0, 0, 1, 10, 2, 20, 3, 30 };
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
		Assert.AreEqual(state.arrayOfNumbers[3], 30);
	}

	[Test]
	public void MapSchemaTypesTest()
	{
		var state = new SchemaTest.MapSchemaTypes.MapSchemaTypes();
		byte[] bytes = { 0, 2, 163, 111, 110, 101, 0, 100, 1, 204, 200, 193, 163, 116, 119, 111, 0, 205, 44, 1, 1, 205, 144, 1, 193, 1, 3, 163, 111, 110, 101, 1, 163, 116, 119, 111, 2, 165, 116, 104, 114, 101, 101, 3 };
		state.Decode(bytes);

		Assert.AreEqual(state.mapOfSchemas.Count, 2);
		Assert.AreEqual(state.mapOfSchemas["one"].x, 100);
		Assert.AreEqual(state.mapOfSchemas["one"].y, 200);
		Assert.AreEqual(state.mapOfSchemas["two"].x, 300);
		Assert.AreEqual(state.mapOfSchemas["two"].y, 400);

		Assert.AreEqual(state.mapOfNumbers.Count, 3);
		Assert.AreEqual(state.mapOfNumbers["one"], 1);
		Assert.AreEqual(state.mapOfNumbers["two"], 2);
		Assert.AreEqual(state.mapOfNumbers["three"], 3);
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

}
