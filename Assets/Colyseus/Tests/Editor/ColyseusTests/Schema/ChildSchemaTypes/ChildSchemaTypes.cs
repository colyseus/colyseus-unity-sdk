// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.23
// 

using Colyseus.Schema;

namespace SchemaTest.ChildSchemaTypes {
	public partial class ChildSchemaTypes : Schema {
		[Type(0, "ref", typeof(IAmAChild))]
		public IAmAChild child = new IAmAChild();

		[Type(1, "ref", typeof(IAmAChild))]
		public IAmAChild secondChild = new IAmAChild();
	}
}
