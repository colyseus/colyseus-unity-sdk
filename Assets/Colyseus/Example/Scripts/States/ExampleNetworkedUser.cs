using Colyseus.Schema;
using Colyseus;

[System.Serializable]
public partial class ExampleNetworkedUser : ColyseusNetworkedUser
{
    [Type(0, "string")]
	public string id = default(string);

	[Type(1, "string")]
	public string sessionId = default(string);

	[Type(2, "boolean")]
	public bool connected = default(bool);

	[Type(3, "number")]
	public double timestamp = default(double);

	[Type(4, "map", typeof(MapSchema<string>), "string")]
	public MapSchema<string> attributes = new MapSchema<string>();
}

