// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.0-alpha.40
// 

using Colyseus.Schema;

namespace SchemaTest.InstanceSharingTypes {
	public partial class Player : Schema {
		[Type(0, "ref", typeof(Position))]
		public Position position = null;
	}
}
