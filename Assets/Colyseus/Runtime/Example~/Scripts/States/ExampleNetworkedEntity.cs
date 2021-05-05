using Colyseus.Schema;
using Colyseus;
using System.Collections.Generic;

[System.Serializable]
public partial class ExampleNetworkedEntity : ColyseusNetworkedEntity
{
	//public string updateHash;

	[Type(0, "string")]
	public string id = default(string);

	[Type(1, "string")]
	public string ownerId = default(string);

	[Type(2, "string")]
	public string creationId = default(string);

	[Type(3, "number")]
	public double xPos = default(double);

	[Type(4, "number")]
	public double yPos = default(double);

	[Type(5, "number")]
	public double zPos = default(double);

	[Type(6, "number")]
	public float xRot = default(float);

	[Type(7, "number")]
	public float yRot = default(float);

	[Type(8, "number")]
	public float zRot = default(float);

	[Type(9, "number")]
	public float wRot = default(float);

	[Type(10, "number")]
	public float xScale = default(float);

	[Type(11, "number")]
	public float yScale = default(float);

	[Type(12, "number")]
	public float zScale = default(float);

	[Type(13, "number")]
	public double timestamp = default(double);

	[Type(14, "number")]
	public double xVel = default(double);

	[Type(15, "number")]
	public double yVel = default(double);

	[Type(16, "number")]
	public double zVel = default(double);
	
	[Type(17, "map", typeof(MapSchema<string>),"string")]
	public MapSchema<string> attributes = new MapSchema<string>();
}

[System.Serializable]
class EntityCreationMessage
{
	public string creationId;
	public Dictionary<string, object> attributes;
}

