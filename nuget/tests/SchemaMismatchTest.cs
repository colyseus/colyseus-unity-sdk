using System;
using NUnit.Framework;
using Colyseus.Schema;
using Colyseus.Tests.CollectionDefaults;

namespace Colyseus.Tests
{
	/// <summary>
	///     Client and server schemas that disagree (issue #247) must fail with a readable error,
	///     not an <see cref="OverflowException" /> from <see cref="Convert.ToInt32(float)" />.
	/// </summary>
	[TestFixture]
	public class SchemaMismatchTest
	{
		[Test]
		public void StringWhereMapIsDeclaredThrowsSchemaMismatch()
		{
			// encodeAll() of `class MyRoomState { @type("string") mySynchronizedProperty = "Hello world" }`
			// (create-colyseus-app 0.16 template), decoded by a client whose field 0 is a map
			byte[] bytes = { 128, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100 };

			var ex = Assert.Throws<InvalidOperationException>(() => new Decoder<DState>().Decode(bytes));
			StringAssert.StartsWith("Schema mismatch: field 0 of DState is declared as \"map\"", ex.Message);
		}
	}
}
