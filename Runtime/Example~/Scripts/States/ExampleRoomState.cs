// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.22
// 

using Colyseus.Schema;

public partial class ExampleRoomState : Schema {
	[Type(0, "map", typeof(MapSchema<ExampleNetworkedEntity>))]
	public MapSchema<ExampleNetworkedEntity> networkedEntities = new MapSchema<ExampleNetworkedEntity>();

	[Type(1, "map", typeof(MapSchema<ExampleNetworkedUser>))]
	public MapSchema<ExampleNetworkedUser> networkedUsers = new MapSchema<ExampleNetworkedUser>();

	[Type(2, "map", typeof(MapSchema<string>), "string")]
	public MapSchema<string> attributes = new MapSchema<string>();
}

