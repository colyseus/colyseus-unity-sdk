using NUnit.Framework;
using System.Collections.Generic;
using Colyseus.Schema;

namespace Colyseus.Tests
{

	[TestFixture]
	public class SchemaDeserializerTest
	{

		[Test]
		public void PrimitiveTypesTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.PrimitiveTypes.PrimitiveTypes>();
			var state = decoder.State;
			byte[] bytes = { 128, 128, 129, 255, 130, 0, 128, 131, 255, 255, 132, 0, 0, 0, 128, 133, 255, 255, 255, 255, 134, 0, 0, 0, 0, 0, 0, 0, 128, 135, 255, 255, 255, 255, 255, 255, 31, 0, 136, 204, 204, 204, 253, 137, 255, 255, 255, 255, 255, 255, 239, 127, 138, 208, 128, 139, 204, 255, 140, 209, 0, 128, 141, 205, 255, 255, 142, 210, 0, 0, 0, 128, 143, 203, 0, 0, 224, 255, 255, 255, 239, 65, 144, 203, 0, 0, 0, 0, 0, 0, 224, 195, 145, 203, 255, 255, 255, 255, 255, 255, 63, 67, 146, 203, 61, 255, 145, 224, 255, 255, 239, 199, 147, 203, 153, 153, 153, 153, 153, 153, 185, 127, 148, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 149, 1 };
			decoder.Decode(bytes);

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
			Assert.AreEqual(state.varint_float64, float.PositiveInfinity);

			Assert.AreEqual(state.str, "Hello world");
			Assert.AreEqual(state.boolean, true);
		}

		[Test]
		public void ChildSchemaTypesTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ChildSchemaTypes.ChildSchemaTypes>();
			var state = decoder.State;
			byte[] bytes = { 128, 1, 129, 2, 255, 1, 128, 205, 244, 1, 129, 205, 32, 3, 255, 2, 128, 204, 200, 129, 205, 44, 1 };
			decoder.Decode(bytes);

			Assert.AreEqual(state.child.x, 500);
			Assert.AreEqual(state.child.y, 800);

			Assert.AreEqual(state.secondChild.x, 200);
			Assert.AreEqual(state.secondChild.y, 300);
		}

		[Test]
		public void ArraySchemaTypesTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ArraySchemaTypes.ArraySchemaTypes>();
			var state = decoder.State;
			byte[] bytes = { 128, 1, 129, 2, 130, 3, 131, 4, 255, 1, 128, 0, 5, 128, 1, 6, 255, 2, 128, 0, 0, 128, 1, 10, 128, 2, 20, 128, 3, 205, 192, 13, 255, 3, 128, 0, 163, 111, 110, 101, 128, 1, 163, 116, 119, 111, 128, 2, 165, 116, 104, 114, 101, 101, 255, 4, 128, 0, 232, 3, 0, 0, 128, 1, 192, 13, 0, 0, 128, 2, 72, 244, 255, 255, 255, 5, 128, 100, 129, 208, 156, 255, 6, 128, 100, 129, 208, 156 };

			var callbacks = Colyseus.Schema.Callbacks.Get(decoder);

			decoder.Decode(bytes);

			var arrayOfSchemasOnAdd = 0;
			var arrayOfNumbersOnAdd = 0;
			var arrayOfStringsOnAdd = 0;
			var arrayOfInt32OnAdd = 0;

			callbacks.OnAdd(state => state.arrayOfSchemas, (key, value) => arrayOfSchemasOnAdd++);
			callbacks.OnAdd(state => state.arrayOfNumbers, (key, value) => arrayOfNumbersOnAdd++);
			callbacks.OnAdd(state => state.arrayOfStrings, (key, value) => arrayOfStringsOnAdd++);
			callbacks.OnAdd(state => state.arrayOfInt32, (key, value) => arrayOfInt32OnAdd++);

			Assert.AreEqual(2, arrayOfSchemasOnAdd);
			Assert.AreEqual(4, arrayOfNumbersOnAdd);
			Assert.AreEqual(3, arrayOfStringsOnAdd);
			Assert.AreEqual(3, arrayOfInt32OnAdd);

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

			var arrayOfSchemasOnRemove = 0;
			var arrayOfNumbersOnRemove = 0;
			var arrayOfStringsOnRemove = 0;
			var arrayOfInt32OnRemove = 0;

			callbacks.OnRemove(state => state.arrayOfSchemas, (key, value) => arrayOfSchemasOnRemove++);
			callbacks.OnRemove(state => state.arrayOfNumbers, (key, value) => arrayOfNumbersOnRemove++);
			callbacks.OnRemove(state => state.arrayOfStrings, (key, value) => arrayOfStringsOnRemove++);
			callbacks.OnRemove(state => state.arrayOfInt32, (key, value) => arrayOfInt32OnRemove++);

			// byte[] popBytes = { 255, 1, 64, 1, 255, 2, 64, 3, 64, 2, 64, 1, 255, 4, 64, 2, 64, 1, 255, 3, 64, 2, 64, 1 };
			byte[] popBytes = { 255, 1, 64, 1, 255, 2, 64, 3, 64, 2, 64, 1, 255, 4, 64, 2, 64, 1, 255, 3, 64, 2, 64, 1 };
			decoder.Decode(popBytes);

			Assert.AreEqual(1, state.arrayOfSchemas.Count);
			Assert.AreEqual(1, state.arrayOfNumbers.Count);
			Assert.AreEqual(1, state.arrayOfStrings.Count);
			Assert.AreEqual(1, state.arrayOfInt32.Count);

			Assert.AreEqual(1, arrayOfSchemasOnRemove);
			Assert.AreEqual(3, arrayOfNumbersOnRemove);
			Assert.AreEqual(2, arrayOfStringsOnRemove);
			Assert.AreEqual(2, arrayOfInt32OnRemove);

			// re-initialize ArraySchema's
			decoder.Decode(new byte[] { 128, 7, 129, 8, 131, 9, 130, 10, 255, 7, 255, 8, 255, 9, 255, 10 });
			Assert.AreEqual(0, state.arrayOfSchemas.Count);
			Assert.AreEqual(0, state.arrayOfNumbers.Count);
			Assert.AreEqual(0, state.arrayOfStrings.Count);
			Assert.AreEqual(0, state.arrayOfInt32.Count);
		}

		[Test]
		public void ArraySchemaClearTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ArraySchemaClear.ArraySchemaClear>();
			var state = decoder.State;
			int onAddCount = 0;
			int onRemoveCount = 0;

			var callbacks = Colyseus.Schema.Callbacks.Get(decoder);
			callbacks.OnAdd(state => state.items, (key, value) => onAddCount++);
			callbacks.OnRemove(state => state.items, (key, value) => onRemoveCount++);

			byte[] bytes = { 128, 1, 255, 1, 128, 0, 1, 128, 1, 2, 128, 2, 3, 128, 3, 4, 128, 4, 5 };
			decoder.Decode(bytes);

			Assert.AreEqual(5, onAddCount);
			Assert.AreEqual(0, onRemoveCount);

			byte[] clearBytes = { 255, 1, 10, 255, 1 };
			decoder.Decode(clearBytes);

			Assert.AreEqual(5, onAddCount);
			Assert.AreEqual(5, onRemoveCount);

			byte[] reAddBytes = { 255, 1, 128, 0, 1, 128, 1, 2, 128, 2, 3, 128, 3, 4, 128, 4, 5, 255, 1, 255, 1 };
			decoder.Decode(reAddBytes);

			Assert.AreEqual(10, onAddCount);
			Assert.AreEqual(5, onRemoveCount);
		}

		[Test]
		public void MapSchemaTypesTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.MapSchemaTypes.MapSchemaTypes>();
			var state = decoder.State;
			byte[] bytes = { 128, 1, 129, 2, 130, 3, 131, 4, 255, 1, 128, 0, 163, 111, 110, 101, 5, 128, 1, 163, 116, 119, 111, 6, 128, 2, 165, 116, 104, 114, 101, 101, 7, 255, 2, 128, 0, 163, 111, 110, 101, 1, 128, 1, 163, 116, 119, 111, 2, 128, 2, 165, 116, 104, 114, 101, 101, 205, 192, 13, 255, 3, 128, 0, 163, 111, 110, 101, 163, 79, 110, 101, 128, 1, 163, 116, 119, 111, 163, 84, 119, 111, 128, 2, 165, 116, 104, 114, 101, 101, 165, 84, 104, 114, 101, 101, 255, 4, 128, 0, 163, 111, 110, 101, 192, 13, 0, 0, 128, 1, 163, 116, 119, 111, 24, 252, 255, 255, 128, 2, 165, 116, 104, 114, 101, 101, 208, 7, 0, 0, 255, 5, 128, 100, 129, 204, 200, 255, 6, 128, 205, 44, 1, 129, 205, 144, 1, 255, 7, 128, 205, 244, 1, 129, 205, 88, 2 };

			var callbacks = Colyseus.Schema.Callbacks.Get(decoder);

			var mapOfSchemasAdd = 0;
			var mapOfNumbersAdd = 0;
			var mapOfStringsAdd = 0;
			var mapOfIntAdd = 0;
			callbacks.OnAdd(state => state.mapOfSchemas, (key, value) => mapOfSchemasAdd++);
			callbacks.OnAdd(state => state.mapOfNumbers, (key, value) => mapOfNumbersAdd++);
			callbacks.OnAdd(state => state.mapOfStrings, (key, value) => mapOfStringsAdd++);
			callbacks.OnAdd(state => state.mapOfInt32, (key, value) => mapOfIntAdd++);

			var mapOfSchemasRemove = 0;
			var mapOfNumbersRemove = 0;
			var mapOfStringsRemove = 0;
			var mapOfIntRemove = 0;
			callbacks.OnRemove(state => state.mapOfSchemas, (key, value) => mapOfSchemasRemove++);
			callbacks.OnRemove(state => state.mapOfNumbers, (key, value) => mapOfNumbersRemove++);
			callbacks.OnRemove(state => state.mapOfStrings, (key, value) => mapOfStringsRemove++);
			callbacks.OnRemove(state => state.mapOfInt32, (key, value) => mapOfIntRemove++);

			var mapOfSchemasChange = 0;
			var mapOfNumbersChange = 0;
			var mapOfStringsChange = 0;
			var mapOfIntChange = 0;
			callbacks.OnChange(state => state.mapOfSchemas, (key, value) => mapOfSchemasChange++);
			callbacks.OnChange(state => state.mapOfNumbers, (key, value) => mapOfNumbersChange++);
			callbacks.OnChange(state => state.mapOfStrings, (key, value) => mapOfStringsChange++);
			callbacks.OnChange(state => state.mapOfInt32, (key, value) => mapOfIntChange++);

			decoder.Decode(bytes);

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

			Assert.AreEqual(mapOfSchemasAdd, 3);
			Assert.AreEqual(mapOfNumbersAdd, 3);
			Assert.AreEqual(mapOfStringsAdd, 3);
			Assert.AreEqual(mapOfIntAdd, 3);

			byte[] deleteBytes = { 255, 2, 64, 1, 64, 2, 255, 1, 64, 1, 64, 2, 255, 3, 64, 1, 64, 2, 255, 4, 64, 1, 64, 2 };
			decoder.Decode(deleteBytes);

			Assert.AreEqual(state.mapOfSchemas.Count, 1);
			Assert.AreEqual(state.mapOfNumbers.Count, 1);
			Assert.AreEqual(state.mapOfStrings.Count, 1);
			Assert.AreEqual(state.mapOfInt32.Count, 1);

			Assert.AreEqual(mapOfSchemasRemove, 2);
			Assert.AreEqual(mapOfNumbersRemove, 2);
			Assert.AreEqual(mapOfStringsRemove, 2);
			Assert.AreEqual(mapOfIntRemove, 2);

			Assert.AreEqual(mapOfSchemasChange, 5);
			Assert.AreEqual(mapOfNumbersChange, 5);
			Assert.AreEqual(mapOfStringsChange, 5);
			Assert.AreEqual(mapOfIntChange, 5);
		}

		[Test]
		public void MapSchemaInt8Test()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.MapSchemaInt8.MapSchemaInt8>();
			var state = decoder.State;
			byte[] bytes = { 128, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 129, 1, 255, 1, 128, 0, 163, 98, 98, 98, 1, 128, 1, 163, 97, 97, 97, 1, 128, 2, 163, 50, 50, 49, 1, 128, 3, 163, 48, 50, 49, 1, 128, 4, 162, 49, 53, 1, 128, 5, 162, 49, 48, 1 };

			decoder.Decode(bytes);

			Assert.AreEqual(state.status, "Hello world");
			Assert.AreEqual(state.mapOfInt8["bbb"], 1);
			Assert.AreEqual(state.mapOfInt8["aaa"], 1);
			Assert.AreEqual(state.mapOfInt8["221"], 1);
			Assert.AreEqual(state.mapOfInt8["021"], 1);
			Assert.AreEqual(state.mapOfInt8["15"], 1);
			Assert.AreEqual(state.mapOfInt8["10"], 1);

			byte[] addBytes = { 255, 1, 0, 5, 2 };
			decoder.Decode(addBytes);

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
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.MapSchemaMoveNullifyType.State>();
			var state = decoder.State;
			byte[] bytes = { 129, 1, 64, 255, 1, 128, 0, 161, 48, 0 };

			decoder.Decode(bytes);

			Assert.DoesNotThrow(() =>
			{
				// FIXME: this test only passes because empty
				byte[] moveAndNullifyBytes = { 128, 1, 65 };
				decoder.Decode(moveAndNullifyBytes);
			});
		}

		[Test]
		public void InheritedTypesTest()
		{
			var serializer = new Colyseus.SchemaSerializer<SchemaTest.InheritedTypes.InheritedTypes>();
			byte[] handshake = { 128, 1, 255, 1, 128, 0, 2, 128, 1, 8, 128, 2, 12, 128, 3, 15, 255, 2, 130, 3, 128, 0, 255, 3, 128, 0, 4, 128, 1, 5, 128, 2, 6, 128, 3, 7, 255, 4, 128, 166, 101, 110, 116, 105, 116, 121, 130, 1, 129, 163, 114, 101, 102, 255, 5, 128, 166, 112, 108, 97, 121, 101, 114, 130, 2, 129, 163, 114, 101, 102, 255, 6, 128, 163, 98, 111, 116, 130, 3, 129, 163, 114, 101, 102, 255, 7, 128, 163, 97, 110, 121, 130, 1, 129, 163, 114, 101, 102, 255, 8, 130, 9, 128, 1, 255, 9, 128, 0, 10, 128, 1, 11, 255, 10, 128, 161, 120, 129, 166, 110, 117, 109, 98, 101, 114, 255, 11, 128, 161, 121, 129, 166, 110, 117, 109, 98, 101, 114, 255, 12, 130, 13, 128, 2, 129, 1, 255, 13, 128, 0, 14, 255, 14, 128, 164, 110, 97, 109, 101, 129, 166, 115, 116, 114, 105, 110, 103, 255, 15, 130, 16, 128, 3, 129, 2, 255, 16, 128, 0, 17, 255, 17, 128, 165, 112, 111, 119, 101, 114, 129, 166, 110, 117, 109, 98, 101, 114 };
			serializer.Handshake(handshake, 0);

			byte[] bytes = { 128, 1, 129, 2, 130, 3, 131, 4, 213, 3, 255, 1, 128, 205, 244, 1, 129, 205, 32, 3, 255, 2, 128, 204, 200, 129, 205, 44, 1, 130, 166, 80, 108, 97, 121, 101, 114, 255, 3, 128, 100, 129, 204, 150, 130, 163, 66, 111, 116, 131, 204, 200, 255, 4, 131, 100 };
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

			var v2decoder = new Colyseus.Schema.Decoder<SchemaTest.BackwardsForwards.StateV2>();
			var statev2 = v2decoder.State;
			v2decoder.Decode(statev1bytes);
			Assert.AreEqual(statev2.str, "Hello world");

			var v1decoder = new Colyseus.Schema.Decoder<SchemaTest.BackwardsForwards.StateV1>();
			var statev1 = v1decoder.State;
			v1decoder.Decode(statev2bytes);
			Assert.AreEqual(statev1.str, "Hello world");
		}

		[Test]
		public void FilteredTypesTest()
		{
			// var client1 = new SchemaTest.FilteredTypes.State();
			// client1.Decode(new byte[] { 255, 0, 130, 1, 128, 2, 128, 2, 255, 1, 128, 0, 4, 255, 2, 128, 163, 111, 110, 101, 255, 2, 128, 163, 111, 110, 101, 255, 4, 128, 163, 111, 110, 101 });
			// Assert.AreEqual("one", client1.playerOne.name);
			// Assert.AreEqual("one", client1.players[0].name);
			// Assert.AreEqual(null, client1.playerTwo.name);

			// var client2 = new SchemaTest.FilteredTypes.State();
			// client2.Decode(new byte[] { 255, 0, 130, 1, 129, 3, 129, 3, 255, 1, 128, 1, 5, 255, 3, 128, 163, 116, 119, 111, 255, 3, 128, 163, 116, 119, 111, 255, 5, 128, 163, 116, 119, 111 });
			// Assert.AreEqual("two", client2.playerTwo.name);
			// Assert.AreEqual("two", client2.players[0].name);
			// Assert.AreEqual(null, client2.playerOne.name);
		}

		[Test]
		public void InstanceSharingTypesTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.InstanceSharingTypes.State>();
			var refs = decoder.Refs;
			var state = decoder.State;

			decoder.Decode(new byte[]  { 130, 1, 131, 2, 128, 3, 129, 3, 255, 1, 255, 2, 255, 3, 128, 4, 255, 4, 128, 10, 129, 10 });
			Assert.AreEqual(state.player1, state.player2);
			Assert.AreEqual(state.player1.position, state.player2.position);
			Assert.AreEqual(refs.refCounts[state.player1.__refId], 2);
			Assert.AreEqual(5, refs.refs.Count);

			decoder.Decode(new byte[] { 64, 65 });
			Assert.AreEqual(null, state.player2);
			Assert.AreEqual(null, state.player2);
			Assert.AreEqual(3, refs.refs.Count);

			decoder.Decode(new byte[] { 255, 1, 128, 0, 5, 128, 1, 5, 128, 2, 5, 128, 3, 7, 255, 5, 128, 6, 255, 6, 128, 10, 129, 10, 255, 7, 128, 8, 255, 8, 128, 10, 129, 10 });
			Assert.AreEqual(state.arrayOfPlayers[0], state.arrayOfPlayers[1]);
			Assert.AreEqual(state.arrayOfPlayers[1], state.arrayOfPlayers[2]);
			Assert.AreNotEqual(state.arrayOfPlayers[2], state.arrayOfPlayers[3]);
			Assert.AreEqual(7, refs.refs.Count);

			decoder.Decode(new byte[] { 255, 1, 64, 3, 64, 2, 64, 1 });
			Assert.AreEqual(1, state.arrayOfPlayers.Count);
			Assert.AreEqual(5, refs.refs.Count);
			var previousArraySchemaRefId = state.arrayOfPlayers.__refId;

			// Replacing ArraySchema
			decoder.Decode(new byte[] { 194, 9, 255, 9, 128, 0, 10, 255, 10, 128, 11, 255, 11, 128, 10, 129, 20 });
			Assert.AreEqual(false, refs.refs.ContainsKey(previousArraySchemaRefId));
			Assert.AreEqual(1, state.arrayOfPlayers.Count);
			Assert.AreEqual(5, refs.refs.Count);

			// Clearing ArraySchema
			decoder.Decode(new byte[] { 255, 9, 10 });
			Assert.AreEqual(0, state.arrayOfPlayers.Count);
			Assert.AreEqual(3, refs.refs.Count);
		}

		[Test]
		public void CallbacksTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.Callbacks.CallbacksState>();
			var state = decoder.State;
			var callbacks = Colyseus.Schema.Callbacks.Get(decoder);

			var onListenContainer = 0;
			var onPlayerAdd = 0;
			var onPlayerChange = 0;
			var onPlayerRemove = 0;

			var onItemAdd = 0;
			var onItemChange = 0;
			var onItemRemove = 0;

			callbacks.Listen(state => state.container, (container, _) =>
			{
				onListenContainer++;

				callbacks.OnAdd(container, container => container.playersMap, (sessionId, player) =>
				{
					onPlayerAdd++;

					callbacks.OnAdd(player, player => player.items, (key, item) =>
					{
						onItemAdd++;
					});

					callbacks.OnChange(player, player => player.items, (key, item) =>
					{
						onItemChange++;
					});

					callbacks.OnRemove(player, player => player.items, (key, item) =>
					{
						onItemRemove++;
					});
				});

				callbacks.OnChange(container, container => container.playersMap, (sessionId, player) =>
				{
					onPlayerChange++;
				});

				callbacks.OnRemove(container, container => container.playersMap, (sessionId, player) =>
				{
					onPlayerRemove++;
				});
			});

			// (initial)
			decoder.Decode(new byte[] { 128, 1, 255, 1, 128, 2, 255, 2 });

			// (1st encode)
			decoder.Decode(new byte[] { 255, 1, 255, 2, 128, 0, 163, 111, 110, 101, 3, 128, 1, 163, 116, 119, 111, 9, 255, 2, 255, 3, 128, 4, 129, 5, 255, 4, 128, 1, 129, 2, 130, 3, 255, 5, 128, 0, 166, 105, 116, 101, 109, 45, 49, 6, 128, 1, 166, 105, 116, 101, 109, 45, 50, 7, 128, 2, 166, 105, 116, 101, 109, 45, 51, 8, 255, 6, 128, 166, 73, 116, 101, 109, 32, 49, 129, 1, 255, 7, 128, 166, 73, 116, 101, 109, 32, 50, 129, 2, 255, 8, 128, 166, 73, 116, 101, 109, 32, 51, 129, 3, 255, 9, 128, 10, 129, 11, 255, 10, 128, 1, 129, 2, 130, 3, 255, 11, 128, 0, 166, 105, 116, 101, 109, 45, 49, 12, 128, 1, 166, 105, 116, 101, 109, 45, 50, 13, 128, 2, 166, 105, 116, 101, 109, 45, 51, 14, 255, 12, 128, 166, 73, 116, 101, 109, 32, 49, 129, 1, 255, 13, 128, 166, 73, 116, 101, 109, 32, 50, 129, 2, 255, 14, 128, 166, 73, 116, 101, 109, 32, 51, 129, 3 });

			Assert.AreEqual(1, onListenContainer);
			Assert.AreEqual(2, onPlayerAdd);
			Assert.AreEqual(2, onPlayerChange);
			Assert.AreEqual(6, onItemAdd);
			Assert.AreEqual(6, onItemChange);

			// (2nd encode)
			decoder.Decode(new byte[] { 255, 1, 255, 2, 64, 1, 128, 2, 165, 116, 104, 114, 101, 101, 16, 255, 2, 255, 3, 255, 4, 255, 5, 64, 0, 64, 1, 128, 3, 166, 105, 116, 101, 109, 45, 52, 15, 255, 8, 255, 5, 255, 5, 255, 15, 128, 166, 73, 116, 101, 109, 32, 52, 129, 4, 255, 2, 255, 16, 128, 17, 129, 18, 255, 17, 128, 1, 129, 2, 130, 3, 255, 18, 128, 0, 166, 105, 116, 101, 109, 45, 49, 19, 128, 1, 166, 105, 116, 101, 109, 45, 50, 20, 128, 2, 166, 105, 116, 101, 109, 45, 51, 21, 255, 19, 128, 166, 73, 116, 101, 109, 32, 49, 129, 1, 255, 20, 128, 166, 73, 116, 101, 109, 32, 50, 129, 2, 255, 21, 128, 166, 73, 116, 101, 109, 32, 51, 129, 3 });

			// (new container)
			decoder.Decode(new byte[] { 128, 22, 255, 2, 255, 5, 255, 5, 255, 2, 255, 0, 255, 22, 128, 23, 255, 23, 128, 0, 164, 108, 97, 115, 116, 24, 255, 24, 128, 25, 129, 26, 255, 25, 128, 10, 129, 10, 130, 10, 255, 26, 128, 0, 163, 111, 110, 101, 27, 255, 27, 128, 166, 73, 116, 101, 109, 32, 49, 129, 1 });

			Assert.AreEqual(2, onListenContainer);
			Assert.AreEqual(4, onPlayerAdd);
			Assert.AreEqual(1, onPlayerRemove);
			Assert.AreEqual(5, onPlayerChange);

			Assert.AreEqual(11, onItemAdd);
			Assert.AreEqual(2, onItemRemove);
			Assert.AreEqual(13, onItemChange);
		}

		[Test]
		public void ReflectionDecodeTest()
		{
			byte[] handshake = { 128, 1, 255, 1, 128, 0, 2, 128, 1, 8, 128, 2, 12, 128, 3, 15, 255, 2, 130, 3, 128, 0, 255, 3, 128, 0, 4, 128, 1, 5, 128, 2, 6, 128, 3, 7, 255, 4, 128, 166, 101, 110, 116, 105, 116, 121, 130, 1, 129, 163, 114, 101, 102, 255, 5, 128, 166, 112, 108, 97, 121, 101, 114, 130, 2, 129, 163, 114, 101, 102, 255, 6, 128, 163, 98, 111, 116, 130, 3, 129, 163, 114, 101, 102, 255, 7, 128, 163, 97, 110, 121, 130, 1, 129, 163, 114, 101, 102, 255, 8, 130, 9, 128, 1, 255, 9, 128, 0, 10, 128, 1, 11, 255, 10, 128, 161, 120, 129, 166, 110, 117, 109, 98, 101, 114, 255, 11, 128, 161, 121, 129, 166, 110, 117, 109, 98, 101, 114, 255, 12, 130, 13, 128, 2, 129, 1, 255, 13, 128, 0, 14, 255, 14, 128, 164, 110, 97, 109, 101, 129, 166, 115, 116, 114, 105, 110, 103, 255, 15, 130, 16, 128, 3, 129, 2, 255, 16, 128, 0, 17, 255, 17, 128, 165, 112, 111, 119, 101, 114, 129, 166, 110, 117, 109, 98, 101, 114 };

			var it = new Iterator { Offset = 0 };
			var reflectionDecoder = new Decoder<Reflection>();
			reflectionDecoder.Decode(handshake, it);

			var reflection = reflectionDecoder.State;

			Assert.IsNotNull(reflection.types, "types should not be null");
			Assert.Greater(reflection.types.Count, 0, "types should have entries");
			Assert.AreEqual(4, reflection.types.Count, $"expected 4 types, rootType={reflection.rootType}");

			for (int i = 0; i < reflection.types.Count; i++)
			{
				var t = reflection.types[i];
				Assert.IsNotNull(t.fields, $"Type[{i}] id={t.id} should have fields");
			}
		}

		//
		// Fixtures below generated by schema-5.0
		// test-external/generate-arrayschema-fixtures.ts (self-verified against
		// the 5.0 JS Decoder). See TODO/sdk-decoders-arrayschema-insert.md.
		//

		[Test]
		public void ArraySchemaUnshiftTest()
		{
			// ADD at an occupied index = insert (not only index 0)
			byte[] snapshot = { 128, 1, 129, 2, 130, 3, 255, 1, 128, 0, 1, 128, 1, 2, 128, 2, 3 };

			// consecutive unshift: [1,2,3] -> unshift(0); unshift(-1)
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ArraySchemaInsertOps.ArraySchemaInsertOps>();
			decoder.Decode(snapshot);
			decoder.Decode(new byte[] { 255, 1, 128, 0, 255, 128, 1, 0 });
			Assert.AreEqual(5, decoder.State.numbers.Count);
			Assert.AreEqual(-1, decoder.State.numbers[0]);
			Assert.AreEqual(0, decoder.State.numbers[1]);
			Assert.AreEqual(1, decoder.State.numbers[2]);
			Assert.AreEqual(2, decoder.State.numbers[3]);
			Assert.AreEqual(3, decoder.State.numbers[4]);

			// multi-item unshift: [1,2,3] -> unshift(-1, -2)
			decoder = new Colyseus.Schema.Decoder<SchemaTest.ArraySchemaInsertOps.ArraySchemaInsertOps>();
			decoder.Decode(snapshot);
			decoder.Decode(new byte[] { 255, 1, 128, 0, 255, 128, 1, 254 });
			Assert.AreEqual(5, decoder.State.numbers.Count);
			Assert.AreEqual(-1, decoder.State.numbers[0]);
			Assert.AreEqual(-2, decoder.State.numbers[1]);
			Assert.AreEqual(1, decoder.State.numbers[2]);

			// pending same-tick ops: arr[2]=99; push(4); unshift(0)
			decoder = new Colyseus.Schema.Decoder<SchemaTest.ArraySchemaInsertOps.ArraySchemaInsertOps>();
			decoder.Decode(snapshot);
			decoder.Decode(new byte[] { 255, 1, 128, 0, 0, 0, 3, 99, 128, 4, 4 });
			Assert.AreEqual(5, decoder.State.numbers.Count);
			Assert.AreEqual(0, decoder.State.numbers[0]);
			Assert.AreEqual(1, decoder.State.numbers[1]);
			Assert.AreEqual(2, decoder.State.numbers[2]);
			Assert.AreEqual(99, decoder.State.numbers[3]);
			Assert.AreEqual(4, decoder.State.numbers[4]);

			// clear + unshift in the same tick: clear(); unshift(9); unshift(8)
			decoder = new Colyseus.Schema.Decoder<SchemaTest.ArraySchemaInsertOps.ArraySchemaInsertOps>();
			decoder.Decode(snapshot);
			decoder.Decode(new byte[] { 255, 1, 10, 128, 0, 8, 128, 1, 9 });
			Assert.AreEqual(2, decoder.State.numbers.Count);
			Assert.AreEqual(8, decoder.State.numbers[0]);
			Assert.AreEqual(9, decoder.State.numbers[1]);
		}

		[Test]
		public void ArraySchemaUnshiftSchemaInstancesTest()
		{
			// [Item(1), Item(2)] -> unshift(Item(0)); unshift(Item(-1))
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ArraySchemaInsertOps.ArraySchemaInsertOps>();
			decoder.Decode(new byte[] { 128, 1, 129, 2, 130, 3, 255, 2, 128, 0, 4, 128, 1, 5, 255, 4, 128, 1, 255, 5, 128, 2 });
			decoder.Decode(new byte[] { 255, 2, 128, 0, 7, 128, 1, 6, 255, 6, 128, 0, 255, 7, 128, 255 });

			var state = decoder.State;
			Assert.AreEqual(4, state.items.Count);
			Assert.AreEqual(-1, state.items[0].value);
			Assert.AreEqual(0, state.items[1].value);
			Assert.AreEqual(1, state.items[2].value);
			Assert.AreEqual(2, state.items[3].value);

			// no refId leaks
			Assert.AreEqual(8, decoder.Refs.refs.Count);
		}

		[Test]
		public void ArraySchemaSortNoInsertTest()
		{
			// sort() emits REPLACE at occupied indexes (incl. 0) — must NOT insert
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ArraySchemaInsertOps.ArraySchemaInsertOps>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 130, 3, 255, 3, 128, 0, 4, 128, 1, 5, 128, 2, 6, 128, 3, 7, 128, 4, 8, 255, 4, 128, 163, 79, 110, 101, 129, 10, 130, 0, 255, 5, 128, 163, 84, 119, 111, 129, 30, 130, 1, 255, 6, 128, 165, 84, 104, 114, 101, 101, 129, 20, 130, 2, 255, 7, 128, 164, 70, 111, 117, 114, 129, 50, 130, 3, 255, 8, 128, 164, 70, 105, 118, 101, 129, 40, 130, 4 });
			Assert.AreEqual(5, state.players.Count);
			Assert.AreEqual("One", state.players[0].name);
			Assert.AreEqual("Five", state.players[4].name);

			// sort by y desc
			decoder.Decode(new byte[] { 255, 3, 0, 0, 8, 0, 1, 7, 0, 2, 6, 0, 3, 5, 0, 4, 4 });
			Assert.AreEqual(5, state.players.Count);
			Assert.AreEqual("Five", state.players[0].name);
			Assert.AreEqual("Four", state.players[1].name);
			Assert.AreEqual("Three", state.players[2].name);
			Assert.AreEqual("Two", state.players[3].name);
			Assert.AreEqual("One", state.players[4].name);

			// sort by x
			decoder.Decode(new byte[] { 255, 3, 0, 0, 4, 0, 1, 6, 0, 2, 5, 0, 3, 8, 0, 4, 7 });
			Assert.AreEqual(5, state.players.Count);
			Assert.AreEqual("One", state.players[0].name);
			Assert.AreEqual("Three", state.players[1].name);
			Assert.AreEqual("Two", state.players[2].name);
			Assert.AreEqual("Five", state.players[3].name);
			Assert.AreEqual("Four", state.players[4].name);
		}

		[Test]
		public void ArraySchemaStaleDeleteByRefIdTest()
		{
			// mid-tick join: the same shared patch is decoded by an old client
			// (knows all refIds) and a fresh client (bootstrapped mid-tick, only
			// knows the survivors). Stale DELETE_BY_REFIDs must be skipped
			// silently: no write, no OnRemove — and no crash (previously threw
			// ArgumentOutOfRangeException via DeleteByIndex(-1)).
			byte[] sharedPatch = { 255, 2, 33, 4, 33, 5, 33, 6 };

			var oldDecoder = new Colyseus.Schema.Decoder<SchemaTest.ArraySchemaInsertOps.ArraySchemaInsertOps>();
			oldDecoder.Decode(new byte[] { 128, 1, 129, 2, 130, 3, 255, 2, 128, 0, 4, 128, 1, 5, 128, 2, 6, 128, 3, 7, 128, 4, 8, 255, 4, 128, 0, 255, 5, 128, 1, 255, 6, 128, 2, 255, 7, 128, 3, 255, 8, 128, 4 });

			var freshDecoder = new Colyseus.Schema.Decoder<SchemaTest.ArraySchemaInsertOps.ArraySchemaInsertOps>();
			freshDecoder.Decode(new byte[] { 128, 1, 129, 2, 130, 3, 255, 2, 128, 0, 6, 128, 1, 7, 128, 2, 8, 255, 6, 128, 2, 255, 7, 128, 3, 255, 8, 128, 4 });

			var oldRemoveCount = 0;
			var freshRemoveCount = 0;
			var oldCallbacks = Colyseus.Schema.Callbacks.Get(oldDecoder);
			var freshCallbacks = Colyseus.Schema.Callbacks.Get(freshDecoder);
			oldCallbacks.OnRemove(state => state.items, (key, value) => oldRemoveCount++);
			freshCallbacks.OnRemove(state => state.items, (key, value) => freshRemoveCount++);

			oldDecoder.Decode(sharedPatch);
			freshDecoder.Decode(sharedPatch);

			// both converge to the server state: [3, 4]
			Assert.AreEqual(2, oldDecoder.State.items.Count);
			Assert.AreEqual(3, oldDecoder.State.items[0].value);
			Assert.AreEqual(4, oldDecoder.State.items[1].value);
			Assert.AreEqual(2, freshDecoder.State.items.Count);
			Assert.AreEqual(3, freshDecoder.State.items[0].value);
			Assert.AreEqual(4, freshDecoder.State.items[1].value);

			// old client applied 3 deletes; fresh client skipped the 2 stale ones
			Assert.AreEqual(3, oldRemoveCount);
			Assert.AreEqual(1, freshRemoveCount);

			// ref counts converge too (stale refIds were never tracked by fresh)
			Assert.AreEqual(6, oldDecoder.Refs.refs.Count);
			Assert.AreEqual(6, freshDecoder.Refs.refs.Count);
		}

		//
		// DecodeResync fixtures below generated by schema-5.0
		// test-external/generate-resync-fixtures.ts (self-verified against the
		// 5.0 JS Decoder). Contract: PORTING_RESYNC.md / test/ResyncSweep.test.ts.
		//

		[Test]
		public void ResyncGhostMapTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 162, 101, 49, 3, 128, 1, 162, 101, 50, 5, 255, 3, 128, 163, 111, 110, 101, 129, 100, 130, 4, 255, 5, 128, 163, 116, 119, 111, 129, 100, 130, 6 });

			var removedItems = new List<SchemaTest.ResyncFixtures.Unit>();
			var removedKeys = new List<string>();
			var callbacks = Colyseus.Schema.Callbacks.Get(decoder);
			callbacks.OnRemove(s => s.units, (key, value) => { removedItems.Add(value); removedKeys.Add(key); });

			var ghost = state.units["e2"];

			// offline: e2 died; its DELETE patch was never delivered
			decoder.DecodeResync(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 162, 101, 49, 3, 255, 3, 128, 163, 111, 110, 101, 129, 100, 130, 4 });

			Assert.AreEqual(1, state.units.Count);
			Assert.IsNotNull(state.units["e1"]);
			Assert.AreEqual(1, removedItems.Count);
			Assert.AreSame(ghost, removedItems[0]); // the REAL previous instance
			Assert.AreEqual("e2", removedKeys[0]);
			Assert.AreEqual(5, decoder.Refs.refs.Count);
		}

		[Test]
		public void ResyncPrunePrimitiveMapTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 2, 128, 0, 162, 116, 49, 10, 128, 1, 162, 116, 50, 20, 128, 2, 162, 116, 51, 30 });

			var removed = new List<(float, string)>();
			var callbacks = Colyseus.Schema.Callbacks.Get(decoder);
			callbacks.OnRemove(s => s.trees, (key, value) => removed.Add((value, key)));

			decoder.DecodeResync(new byte[] { 128, 1, 129, 2, 255, 2, 128, 0, 162, 116, 49, 10, 128, 2, 162, 116, 51, 30 });

			Assert.AreEqual(2, state.trees.Count);
			Assert.AreEqual(10, state.trees["t1"]);
			Assert.AreEqual(30, state.trees["t3"]);
			Assert.AreEqual(1, removed.Count);
			Assert.AreEqual(20, removed[0].Item1);
			Assert.AreEqual("t2", removed[0].Item2);
		}

		[Test]
		public void ResyncNestedCollectionTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 164, 104, 101, 114, 111, 3, 255, 3, 128, 164, 104, 101, 114, 111, 129, 100, 130, 4, 255, 4, 128, 0, 5, 128, 1, 6, 128, 2, 7, 255, 5, 128, 0, 255, 6, 128, 1, 255, 7, 128, 2 });

			var heroBefore = state.units["hero"];

			// offline: hero lost the middle gem
			decoder.DecodeResync(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 164, 104, 101, 114, 111, 3, 255, 3, 128, 164, 104, 101, 114, 111, 129, 100, 130, 4, 255, 4, 128, 0, 5, 128, 1, 7, 255, 5, 128, 0, 255, 7, 128, 2 });

			Assert.AreSame(heroBefore, state.units["hero"]); // retained parent keeps identity
			Assert.AreEqual(2, state.units["hero"].gems.Count);
			Assert.AreEqual(0, state.units["hero"].gems[0].price);
			Assert.AreEqual(2, state.units["hero"].gems[1].price);
		}

		[Test]
		public void ResyncViewInteriorTest()
		{
			// snapshots are view-encoded (ADD_BY_REFID ops)
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncArrayState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 255, 0, 128, 1, 255, 1, 129, 2, 2, 129, 4, 4, 129, 6, 6, 255, 2, 128, 161, 97, 129, 100, 130, 3, 255, 4, 128, 161, 98, 129, 100, 130, 5, 255, 6, 128, 161, 99, 129, 100, 130, 7 });
			Assert.AreEqual(3, state.arr.Count);

			// offline: b left the view — an interior index, not the tail
			decoder.DecodeResync(new byte[] { 255, 0, 128, 1, 255, 1, 129, 2, 2, 129, 6, 6, 255, 2, 128, 161, 97, 129, 100, 130, 3, 255, 6, 128, 161, 99, 129, 100, 130, 7 });

			Assert.AreEqual(2, state.arr.Count);
			Assert.AreEqual("a", state.arr[0].name);
			Assert.AreEqual("c", state.arr[1].name);
		}

		[Test]
		public void ResyncEmptiedCollectionTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 162, 101, 49, 3, 128, 1, 162, 101, 50, 5, 255, 3, 128, 163, 111, 110, 101, 129, 100, 130, 4, 255, 5, 128, 163, 116, 119, 111, 129, 100, 130, 6 });

			var removals = 0;
			var callbacks = Colyseus.Schema.Callbacks.Get(decoder);
			callbacks.OnRemove(s => s.units, (key, value) => removals++);

			decoder.DecodeResync(new byte[] { 128, 1, 129, 2 });

			Assert.AreEqual(0, state.units.Count);
			Assert.AreEqual(2, removals);
		}

		[Test]
		public void ResyncNoDoubleDecrementTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 164, 114, 105, 99, 104, 3, 128, 1, 164, 107, 101, 101, 112, 10, 255, 3, 128, 164, 114, 105, 99, 104, 129, 100, 130, 4, 255, 4, 128, 0, 5, 128, 1, 6, 128, 2, 7, 128, 3, 8, 128, 4, 9, 255, 5, 128, 0, 255, 6, 128, 1, 255, 7, 128, 2, 255, 8, 128, 3, 255, 9, 128, 4, 255, 10, 128, 164, 107, 101, 101, 112, 129, 100, 130, 11, 255, 11, 128, 0, 12, 128, 1, 13, 255, 12, 128, 0, 255, 13, 128, 1 });

			// offline: 'rich' (5 nested gems) died — GC must release the subtree exactly once
			decoder.DecodeResync(new byte[] { 128, 1, 129, 2, 255, 1, 128, 1, 164, 107, 101, 101, 112, 10, 255, 10, 128, 164, 107, 101, 101, 112, 129, 100, 130, 11, 255, 11, 128, 0, 12, 128, 1, 13, 255, 12, 128, 0, 255, 13, 128, 1 });

			Assert.AreEqual(1, state.units.Count);
			Assert.AreEqual(2, state.units["keep"].gems.Count);
			Assert.AreEqual(7, decoder.Refs.refs.Count);
		}

		[Test]
		public void ResyncLateDeleteTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 162, 101, 49, 3, 128, 1, 162, 101, 50, 5, 255, 3, 128, 163, 111, 110, 101, 129, 100, 130, 4, 255, 5, 128, 163, 116, 119, 111, 129, 100, 130, 6 });

			var removals = 0;
			var callbacks = Colyseus.Schema.Callbacks.Get(decoder);
			callbacks.OnRemove(s => s.units, (key, value) => removals++);

			decoder.DecodeResync(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 162, 101, 49, 3, 255, 3, 128, 163, 111, 110, 101, 129, 100, 130, 4 });
			Assert.AreEqual(1, removals);

			// the DELETE patch encoded before the resync arrives late — no re-fire
			decoder.Decode(new byte[] { 255, 1, 64, 1 });

			Assert.AreEqual(1, removals);
			Assert.AreEqual(1, state.units.Count);
		}

		[Test]
		public void ResyncSurvivorIdentityTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 162, 101, 49, 3, 128, 1, 164, 103, 111, 110, 101, 5, 255, 3, 128, 163, 111, 110, 101, 129, 100, 130, 4, 255, 5, 128, 164, 103, 111, 110, 101, 129, 100, 130, 6 });

			var survivor = state.units["e1"];

			var adds = 0;
			var hpValues = new List<float>();
			var callbacks = Colyseus.Schema.Callbacks.Get(decoder);
			callbacks.OnAdd(s => s.units, (key, value) => adds++, false);
			callbacks.Listen(survivor, u => u.hp, (hp, previousHp) => hpValues.Add(hp), false);

			// offline: survivor's hp changed to 55, sibling died
			decoder.DecodeResync(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 162, 101, 49, 3, 255, 3, 128, 163, 111, 110, 101, 129, 55, 130, 4 });

			Assert.AreSame(survivor, state.units["e1"]); // same instance across resync
			Assert.AreEqual(0, adds); // OnAdd must not re-fire for survivors
			Assert.AreEqual(1, hpValues.Count);
			Assert.AreEqual(55, hpValues[0]);

			// callbacks stay wired for live patches after the resync
			decoder.Decode(new byte[] { 255, 3, 129, 77 });
			Assert.AreEqual(2, hpValues.Count);
			Assert.AreEqual(77, hpValues[1]);
		}

		[Test]
		public void ResyncRekeyTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 163, 111, 108, 100, 3, 255, 3, 128, 165, 109, 111, 118, 101, 114, 129, 100, 130, 4 });

			var instance = state.units["old"];

			var adds = new List<string>();
			var removes = new List<string>();
			var callbacks = Colyseus.Schema.Callbacks.Get(decoder);
			callbacks.OnAdd(s => s.units, (key, value) => adds.Add(key), false);
			callbacks.OnRemove(s => s.units, (key, value) => removes.Add(key));

			// offline: the same server instance moved to a new key
			decoder.DecodeResync(new byte[] { 128, 1, 129, 2, 255, 1, 128, 1, 163, 110, 101, 119, 3, 255, 3, 128, 165, 109, 111, 118, 101, 114, 129, 100, 130, 4 });

			Assert.AreSame(instance, state.units["new"]); // same instance under the new key
			Assert.IsNull(state.units["old"]);
			Assert.AreEqual(new List<string> { "new" }, adds);
			Assert.AreEqual(new List<string> { "old" }, removes);
			Assert.AreEqual(5, decoder.Refs.refs.Count);
		}

		[Test]
		public void ResyncReplaceSameKeyTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 161, 107, 3, 255, 3, 128, 165, 102, 105, 114, 115, 116, 129, 100, 130, 4, 255, 4, 128, 0, 5, 128, 1, 6, 255, 5, 128, 0, 255, 6, 128, 1 });

			var oldInstance = state.units["k"];
			var oldRefId = oldInstance.__refId;

			var adds = 0;
			var removes = new List<SchemaTest.ResyncFixtures.Unit>();
			var callbacks = Colyseus.Schema.Callbacks.Get(decoder);
			callbacks.OnAdd(s => s.units, (key, value) => adds++, false);
			callbacks.OnRemove(s => s.units, (key, value) => removes.Add(value));

			// offline: the entity at "k" died and a NEW one spawned at the same key
			decoder.DecodeResync(new byte[] { 128, 1, 129, 2, 255, 1, 128, 1, 161, 107, 7, 255, 7, 128, 166, 115, 101, 99, 111, 110, 100, 129, 100, 130, 8 });

			Assert.AreEqual("second", state.units["k"].name);
			Assert.AreNotSame(oldInstance, state.units["k"]); // fresh instance
			Assert.AreEqual(1, removes.Count);
			Assert.AreSame(oldInstance, removes[0]);
			Assert.AreEqual(1, adds);
			Assert.IsFalse(decoder.Refs.Has(oldRefId)); // old ref GC'd
			Assert.AreEqual(5, decoder.Refs.refs.Count);
		}

		[Test]
		public void ResyncDamagedTest()
		{
			// version skew: the resync payload comes from a server whose Player
			// has an extra field → schema-mismatch skip → damage flag → sweep
			// ABORTED (keeping a ghost beats deleting live entries on bad data)
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncStateV1>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 255, 1, 128, 0, 162, 112, 49, 2, 128, 1, 162, 112, 50, 3, 255, 2, 128, 1, 255, 3, 128, 2 });
			Assert.AreEqual(2, state.players.Count);

			decoder.DecodeResync(new byte[] { 128, 1, 255, 1, 128, 0, 162, 112, 49, 2, 255, 2, 128, 1, 129, 162, 104, 105 });

			Assert.AreEqual(2, state.players.Count); // ghost p2 retained, no crash
		}

		[Test]
		public void ResyncTransientTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncTransientState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 255, 1, 128, 0, 162, 101, 49, 3, 255, 3, 128, 163, 111, 110, 101, 129, 100, 130, 4 });
			// transient entries arrive via patch only — never via full snapshots
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 162, 101, 49, 3, 255, 2, 128, 0, 162, 108, 49, 1, 128, 1, 162, 108, 50, 2, 255, 3, 128, 163, 111, 110, 101, 129, 100, 130, 4 });
			Assert.AreEqual(2, state.locals.Count);

			// the resync snapshot never mentions locals → presence rule leaves it alone
			decoder.DecodeResync(new byte[] { 128, 1, 255, 1, 128, 0, 162, 101, 49, 3, 128, 1, 162, 101, 50, 5, 255, 3, 128, 163, 111, 110, 101, 129, 100, 130, 4, 255, 5, 128, 163, 116, 119, 111, 129, 100, 130, 6 });

			Assert.AreEqual(2, state.locals.Count); // transient data survives the sweep
			Assert.AreEqual(2, state.units.Count);
		}

		[Test]
		public void ResyncPlainAdditiveTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 162, 101, 49, 3, 255, 3, 128, 163, 111, 110, 101, 129, 100, 130, 4 });
			decoder.Decode(new byte[] { 255, 1, 128, 1, 162, 101, 50, 5, 255, 5, 128, 163, 116, 119, 111, 129, 100, 130, 6 });
			// (a DELETE patch for e1 was dropped — never delivered)
			decoder.Decode(new byte[] { 255, 5, 129, 42 });

			Assert.AreEqual(2, state.units.Count); // plain decode stays additive

			decoder.DecodeResync(new byte[] { 128, 1, 129, 2, 255, 1, 128, 1, 162, 101, 50, 5, 255, 5, 128, 163, 116, 119, 111, 129, 42, 130, 6 });

			Assert.AreEqual(1, state.units.Count); // resync reconciles
			Assert.AreEqual(42, state.units["e2"].hp);
		}

		[Test]
		public void ResyncTailTrimTest()
		{
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ResyncFixtures.ResyncState>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 164, 104, 101, 114, 111, 3, 255, 3, 128, 164, 104, 101, 114, 111, 129, 100, 130, 4, 255, 4, 128, 0, 5, 128, 1, 6, 128, 2, 7, 128, 3, 8, 255, 5, 128, 0, 255, 6, 128, 1, 255, 7, 128, 2, 255, 8, 128, 3 });

			// offline: hero lost the last two gems
			decoder.DecodeResync(new byte[] { 128, 1, 129, 2, 255, 1, 128, 0, 164, 104, 101, 114, 111, 3, 255, 3, 128, 164, 104, 101, 114, 111, 129, 100, 130, 4, 255, 4, 128, 0, 5, 128, 1, 6, 255, 5, 128, 0, 255, 6, 128, 1 });

			Assert.AreEqual(2, state.units["hero"].gems.Count);
			Assert.AreEqual(0, state.units["hero"].gems[0].price);
			Assert.AreEqual(1, state.units["hero"].gems[1].price);
			Assert.AreEqual(7, decoder.Refs.refs.Count);
		}

		[Test]
		public void ArraySchemaMoveTest()
		{
			// MOVE (32): existing instances swapped via move(); positional write,
			// must NOT insert. MOVE_AND_ADD (160): new instance at occupied slot.
			var decoder = new Colyseus.Schema.Decoder<SchemaTest.ArraySchemaInsertOps.ArraySchemaInsertOps>();
			var state = decoder.State;
			decoder.Decode(new byte[] { 128, 1, 129, 2, 130, 3, 255, 2, 128, 0, 4, 128, 1, 5, 128, 2, 6, 255, 4, 128, 1, 255, 5, 128, 2, 255, 6, 128, 3 });

			// move(): swap arr[0] <-> arr[1] — carries MOVE (32) ops
			decoder.Decode(new byte[] { 255, 2, 32, 0, 5, 32, 1, 4, 255, 4, 128, 1 });
			Assert.AreEqual(3, state.items.Count);
			Assert.AreEqual(2, state.items[0].value);
			Assert.AreEqual(1, state.items[1].value);
			Assert.AreEqual(3, state.items[2].value);

			// move(): new Item(9) written at occupied slot 2 — MOVE_AND_ADD (160)
			decoder.Decode(new byte[] { 255, 2, 160, 2, 7, 255, 7, 128, 9 });
			Assert.AreEqual(3, state.items.Count);
			Assert.AreEqual(2, state.items[0].value);
			Assert.AreEqual(1, state.items[1].value);
			Assert.AreEqual(9, state.items[2].value);
		}

	}

}
