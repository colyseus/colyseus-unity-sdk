using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using GameDevWare.Serialization.Serializers;

#pragma warning disable 1591

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	[Serializable]
	public class JsonSerializationException : SerializationException
	{
		public enum ErrorCode
		{
			SerializationException = 1,
			EmptyMemberName,
			DiscriminatorNotFirstMemberOfObject,
			CantCreateInstanceOfType,
			SerializationGraphIsTooBig,
			SerializationGraphIsTooDeep,
			TypeIsNotValid,
			SerializingUnknownType,
			SerializingSpecialSystemType,
			UnexpectedEndOfStream,
			UnexpectedMemberName,
			UnexpectedToken,
			UnknownEscapeSequence,
			UnknownDiscriminatorValue,
			SerializationFramesCorruption,
			StreamIsNotReadable,
			StreamIsNotWriteable,
			UnterminatedStringLiteral,
			UnknownNotation,
			MemberNameIsNotSpecified,
			TypeRequiresCustomSerializer
		}

		public ErrorCode Code { get; set; }
		public int LineNumber { get; set; }
		public int ColumnNumber { get; set; }
		public ulong CharactersWritten { get; set; }

		internal JsonSerializationException(string message, ErrorCode errorCode, IJsonReader reader = null)
			: base(message)
		{
			this.Code = errorCode;
			if (reader != null)
				this.Update(reader);
		}
		internal JsonSerializationException(string message, ErrorCode errorCode, IJsonReader reader, Exception innerException)
			: base(message, innerException)
		{
			this.Code = errorCode;
			if (reader != null)
				this.Update(reader);
		}
		internal JsonSerializationException(string message, Exception innerException)
			: base(message, innerException)
		{
			this.Code = ErrorCode.SerializationException;
		}

		protected JsonSerializationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.Code = (ErrorCode)info.GetInt32("Code");
			this.LineNumber = info.GetInt32("LineNumber");
			this.ColumnNumber = info.GetInt32("ColumnNumber");
			this.CharactersWritten = info.GetUInt64("CharactersWritten");
		}

		private void Update(IJsonReader reader)
		{
			this.LineNumber = reader.Value.LineNumber;
			this.ColumnNumber = reader.Value.ColumnNumber;
			//this.Path = reader.Value.Path;
		}

		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Code", (int)this.Code);
			info.AddValue("LineNumber", this.LineNumber);
			info.AddValue("ColumnNumber", this.ColumnNumber);
			info.AddValue("CharactersWritten", this.CharactersWritten);

			base.GetObjectData(info, context);
		}

		public static Exception MemberNameIsEmpty(IJsonReader reader)
		{
			return new JsonSerializationException
			(
				string.Format("An empty member name was deserialized. Path: '{0}'", reader.Context.GetPath()),
				ErrorCode.EmptyMemberName,
				reader
			);
		}
		public static Exception MemberNameIsNotSet()
		{
			return new JsonSerializationException
			(
				"A member name is not set before writing value to object.",
				ErrorCode.MemberNameIsNotSpecified
			);
		}
		public static Exception DiscriminatorIsNotFirstMember(IJsonReader reader)
		{
			return new JsonSerializationException
			(
				string.Format("Discriminator member '{0}' should be first member of object. Path: '{1}'.", ObjectSerializer.TYPE_MEMBER_NAME, reader.Context.GetPath()),
				ErrorCode.DiscriminatorNotFirstMemberOfObject,
				reader
			);
		}
		public static Exception CantCreateInstanceOfType(Type type)
		{
			return new JsonSerializationException
			(
				string.Format("Unable to deserialize instance of '{0}' because ", type.FullName) +
					(type.GetTypeInfo().IsAbstract ? "it is an abstract type." : "there is no parameterless constructor is defined on type."),
				ErrorCode.CantCreateInstanceOfType
			);
		}
		public static Exception SerializationGraphIsTooBig(IJsonReader reader, ulong maxObjects)
		{
			return new JsonSerializationException
			(
				string.Format("Serialization graph is too big. Maximum serialized objects is {0}. Path: '{1}'", maxObjects, reader.Context.GetPath()),
				ErrorCode.SerializationGraphIsTooBig
			);
		}
		public static Exception SerializationGraphIsTooDeep(IJsonReader reader, ulong maxDepth)
		{
			return new JsonSerializationException
			(
					string.Format("Serialization graph is too deep. Maximum depth is {0}. Path: '{1}'", maxDepth, reader.Context.GetPath()),
				ErrorCode.SerializationGraphIsTooDeep)
			;
		}
		public static Exception TypeIsNotValid(Type type, string problem)
		{
			problem = problem.TrimEnd('.');

			return new JsonSerializationException
			(
				string.Format("Type '{0}' is not valid for serialization: {1}.", type.Name, problem),
				ErrorCode.TypeIsNotValid
			);
		}
		public static Exception SerializingUnknownType(Type type)
		{
			return new JsonSerializationException
			(
				string.Format("Attempt to serialize unknown type '{0}' failed.", type.FullName),
				ErrorCode.SerializingUnknownType
			);
		}
		public static Exception SerializingSpecialSystemType(Type type)
		{
			return new JsonSerializationException
			(
				string.Format("Attempt to serialize special system type '{0}' failed. This type is could be serialized.", type.FullName),
				ErrorCode.SerializingSpecialSystemType
			);
		}
		public static Exception UnexpectedEndOfStream(IJsonReader reader)
		{
			return new JsonSerializationException
			(
				string.Format("Unexpected end of stream while more data is expected. Path: '{0}'.", reader.Context.GetPath()),
				ErrorCode.UnexpectedEndOfStream,
				reader
			);
		}
		public static Exception UnexpectedMemberName(string memberName, string expected, IJsonReader reader)
		{
			return new JsonSerializationException
			(
				string.Format("Unexpected member '{0}' is read while '{1}' is expected. Path: '{2}'.", memberName, expected, reader.Context.GetPath()),
				ErrorCode.UnexpectedMemberName,
				reader
			);
		}
		public static Exception UnexpectedToken(IJsonReader reader, params JsonToken[] expectedTokens)
		{
			var tokensStr = default(string);
			if (expectedTokens.Length == 0)
			{
				tokensStr = "<no tokens>";
			}
			else
			{
#if NET40
				tokensStr = String.Join(", ", expectedTokens);
#else
				var tokens = expectedTokens.ConvertAll(c => c.ToString());
				tokensStr = String.Join(", ", tokens);
#endif
			}

			if (reader.Token == JsonToken.EndOfStream)
			{
				return UnexpectedEndOfStream(reader);
			}
			else if (expectedTokens.Length > 1)
			{
				return new JsonSerializationException
				(
					string.Format("Unexpected token read '{0}' while any of '{1}' are expected. Path: '{2}'.", reader.Token, tokensStr, reader.Context.GetPath()),
					ErrorCode.UnexpectedToken,
					reader
				);

			}
			else
			{
				return new JsonSerializationException
				(
					string.Format("Unexpected token read '{0}' while '{1}' is expected. Path: '{2}'.", reader.Token, tokensStr, reader.Context.GetPath()),
					ErrorCode.UnexpectedToken,
					reader
				);
			}
		}
		public static Exception UnknownEscapeSequence(string escape, IJsonReader reader)
		{
			return new JsonSerializationException
			(
				string.Format("An unknown escape sequence '{0}' is read. Path: '{1}'.", escape, reader.Context.GetPath()),
				ErrorCode.UnknownEscapeSequence,
				reader
			);
		}
		public static Exception SerializationFramesCorruption()
		{
			return new JsonSerializationException
			(
				"Serialization frames are corrupted. Probably invalid Push/Pop sequence in TypeSerializers implementation.",
				ErrorCode.SerializationFramesCorruption
			);
		}
		public static Exception StreamIsNotReadable()
		{
			return new JsonSerializationException
			(
				"Can\'t perform deserialization from stream which doesn't support reading.",
				ErrorCode.StreamIsNotReadable
			);
		}
		public static Exception StreamIsNotWriteable()
		{
			return new JsonSerializationException
			(
				"Can\'t perform serialization to stream which doesn't support writing.",
				ErrorCode.StreamIsNotWriteable
			);
		}
		public static Exception UnterminatedStringLiteral(IJsonReader reader)
		{
			return new JsonSerializationException
			(
				string.Format("An unterminated string literal. Path: '{0}'.", reader.Context.GetPath()),
				ErrorCode.UnterminatedStringLiteral,
				reader
			);
		}
		public static Exception UnknownNotation(IJsonReader reader, string notation)
		{
			return new JsonSerializationException
			(
				string.Format("An unknown notation '{0}'. Path: '{1}'.", notation, reader.Context.GetPath()),
				ErrorCode.UnknownNotation,
				reader
			);
		}
		public static Exception TypeRequiresCustomSerializer(Type type, Type typeSerializer)
		{
			return new JsonSerializationException
			(
				string.Format("Type '{0}' can't be serialized by '{1}' and requires custom {2} registered in 'Json.DefaultSerializers'.", type.FullName, typeSerializer.Name, typeof(TypeSerializer).Name),
				ErrorCode.TypeRequiresCustomSerializer
			);
		}
	}
}
