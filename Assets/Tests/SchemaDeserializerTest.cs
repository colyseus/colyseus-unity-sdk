using NUnit.Framework;
using SchemaTest.PrimitiveTypes;
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
		PrimitiveTypes state = new PrimitiveTypes();

		byte[] bytes = { 0, 128, 1, 255, 2, 0, 128, 3, 255, 255, 4, 0, 0, 0, 128, 5, 255, 255, 255, 255, 6, 0, 0, 0, 0, 0, 0, 0, 128, 7, 255, 255, 255, 255, 255, 255, 31, 0, 8, 255, 255, 127, 255, 9, 255, 255, 255, 255, 255, 255, 239, 127, 10, 205, 208, 7, 11, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 12, 1 };
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
		Assert.AreEqual(state.varint, 2000);
		Assert.AreEqual(state.str, "Hello world");
		Assert.AreEqual(state.boolean, true);
	}

}
