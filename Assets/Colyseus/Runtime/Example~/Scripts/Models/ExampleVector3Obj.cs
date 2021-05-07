using System;
using UnityEngine;

//Wrapper class for serializable Vector3 response we will receive from the server
[Serializable]
public class ExampleVector3Obj
{
    public ExampleVector3Obj()
    {
        x = 0;
        y = 0;
        z = 0;
    }

    public ExampleVector3Obj(Vector3 vector3)
    {
        x = vector3.x;
        y = vector3.y;
        z = vector3.z;
    }

    //The X-axis value
    public double x { get; set; }

    //The Y-axis value
    public double y { get; set; }

    //The Z-axis value
    public double z { get; set; }
}