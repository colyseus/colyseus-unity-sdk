using NUnit.Framework;
using Colyseus;

namespace Colyseus.Tests
{

	[TestFixture]
	public class SerializationTest
	{

		[Test]
		public void NoStateSerializerTest()
		{
			var serializer = (ISerializer<NoState>)new NoneSerializer();
			Assert.AreEqual(true, serializer.GetState() is NoState);
		}
	}

}
