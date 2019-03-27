// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 

using Colyseus.Schema;

public class Player : Schema {
	[Type("number")]
	public float x = 0;

	[Type("number")]
	public float y = 0;

	[Type("boolean")]
	public bool connected = false;
}

