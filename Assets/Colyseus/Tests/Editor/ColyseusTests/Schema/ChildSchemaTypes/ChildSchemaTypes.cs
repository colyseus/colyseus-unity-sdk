// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.0-alpha.40
// 

using Colyseus.Schema;

namespace SchemaTest.ChildSchemaTypes {
	public partial class ChildSchemaTypes : Schema {
		[Type(0, "ref", typeof(IAmAChild))]
		public IAmAChild child = null;

		[Type(1, "ref", typeof(IAmAChild))]
		public IAmAChild secondChild = null;
	}
}
