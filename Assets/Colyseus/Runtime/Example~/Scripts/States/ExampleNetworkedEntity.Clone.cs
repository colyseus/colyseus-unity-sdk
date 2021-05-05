using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ExampleNetworkedEntity
{
	// Make sure to update Clone fi you add any attributes
	public ExampleNetworkedEntity Clone()
	{
		return new ExampleNetworkedEntity() { id = id, ownerId = ownerId, creationId = creationId, xPos = xPos, yPos = yPos, zPos = zPos, xRot = xRot, yRot = yRot, zRot = zRot, wRot = wRot, xScale = xScale, yScale = yScale, zScale = zScale, timestamp = timestamp, attributes = attributes };
	}
}
