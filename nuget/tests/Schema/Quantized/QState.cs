// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 5.0.11
// 

using Colyseus.Schema;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace SchemaTest.Quantized {
	public partial class QState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public QState() { }
		[Type(0, "quantized", QuantizeMin = 0, QuantizeMax = 6.283185307179586, QuantizeBits = 16, QuantizeWrap = true)]
		public double yaw = default(double);

		[Type(1, "quantized", QuantizeMin = -1.5, QuantizeMax = 1.5, QuantizeBits = 8, QuantizeWrap = false)]
		public double pitch = default(double);

		[Type(2, "quantized", QuantizeMin = 0, QuantizeMax = 1, QuantizeBits = 32, QuantizeWrap = false)]
		public double precise = default(double);

		[Type(3, "array", typeof(ArraySchema<float>), "number")]
		public ArraySchema<float> nums = null;

		[Type(4, "map", typeof(MapSchema<string>), "string")]
		public MapSchema<string> tags = null;

		[Type(5, "ref", typeof(QChild))]
		public QChild child = null;

		[Type(6, "array", typeof(ArraySchema<QChild>))]
		public ArraySchema<QChild> items = null;

		[Type(7, "string")]
		public string label = default(string);
	}
}
