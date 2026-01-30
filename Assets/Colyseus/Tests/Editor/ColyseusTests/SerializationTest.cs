using NUnit.Framework;
using Colyseus;

public class SerializationTest
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
	public void NoStateSerializerTest()
	{
		var serializer = (ISerializer<NoState>)new NoneSerializer();
		Assert.AreEqual(true, serializer.GetState() is NoState);
	}
}