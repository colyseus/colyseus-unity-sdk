using System;
using GameDevWare.Serialization.MessagePack;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Serializers
{
	public class MsgPackTimestampSerializer : TypeSerializer
	{
		private const string SECONDS_MEMBER_NAME = "$seconds";
		private const string NANO_SECONDS_MEMBER_NAME = "$nanoSeconds";

		/// <inheritdoc />
		public override Type SerializedType { get { return typeof(MessagePackTimestamp); } }

		public override object Deserialize(IJsonReader reader)
		{
			if (reader.RawValue is MessagePackTimestamp)
			{
				return (MessagePackTimestamp)reader.Value.Raw;
			}
			else if (reader.Token == JsonToken.Null)
			{
				return null;
			}

			reader.ReadObjectBegin();
			var seconds = default(long);
			var nanoSeconds = default(uint);
			while (reader.Token != JsonToken.EndOfObject)
			{
				var member = reader.ReadMember();
				switch (member)
				{
					case SECONDS_MEMBER_NAME:
						seconds = reader.ReadInt64();
						break;
					case NANO_SECONDS_MEMBER_NAME:
						seconds = reader.ReadUInt32();
						break;
					default:
						reader.ReadValue(typeof(object)); // skip value
						break;
				}
			}
			reader.ReadObjectEnd(false);

			var value = new MessagePackTimestamp(seconds, nanoSeconds);
			return value;
		}
		public override void Serialize(IJsonWriter writer, object value)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			var messagePackWriter = writer as MsgPackWriter;
			if (messagePackWriter != null)
			{
				var extensionType = default(sbyte);
				var buffer = messagePackWriter.GetWriteBuffer();
				if (messagePackWriter.Context.ExtensionTypeHandler.TryWrite(value, out extensionType, ref buffer))
				{
					messagePackWriter.Write(extensionType, buffer);
					return;
				}
			}

			var timeStamp = (MessagePackTimestamp)value;
			writer.WriteObjectBegin(2);
			writer.WriteMember(SECONDS_MEMBER_NAME);
			writer.Write(timeStamp.Seconds);
			writer.WriteMember(NANO_SECONDS_MEMBER_NAME);
			writer.Write(timeStamp.NanoSeconds);
			writer.WriteObjectEnd();
		}
	}
}
