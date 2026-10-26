using System;
using NUnit.Framework;
using Colyseus.Schema;

namespace Colyseus.Tests
{
	/// <summary>
	///     Client and server schemas that disagree (issue #247) must fail with a readable error,
	///     not an <see cref="OverflowException" /> from <see cref="Convert.ToInt32(float)" />.
	///     <see cref="MyRoomState" /> is the Unity example's state, which #247 joined a template server with.
	/// </summary>
	[TestFixture]
	public class SchemaMismatchTest
	{
		[Test]
		public void StringWhereMapIsDeclaredThrowsSchemaMismatch()
		{
			// encodeAll() of `class MyRoomState { @type("string") mySynchronizedProperty = "Hello world" }`
			// (create-colyseus-app 0.16 template; @colyseus/schema 3.0 and 5.0 encode it identically)
			byte[] bytes = { 128, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100 };

			var ex = Assert.Throws<InvalidOperationException>(() => new Decoder<MyRoomState>().Decode(bytes));
			StringAssert.StartsWith("Schema mismatch: field 0 of MyRoomState is declared as \"map\"", ex.Message);
		}

		[Test]
		public void StringWhereRefIsDeclaredThrowsSchemaMismatch()
		{
			// encodeAll() of `{ @type({ map: Player }) players; @type("string") host = "Hello world" }`
			byte[] bytes = { 128, 1, 129, 171, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100 };

			var ex = Assert.Throws<InvalidOperationException>(() => new Decoder<MyRoomState>().Decode(bytes));
			StringAssert.StartsWith("Schema mismatch: field 1 of MyRoomState is declared as \"ref\"", ex.Message);
		}
	}
}
