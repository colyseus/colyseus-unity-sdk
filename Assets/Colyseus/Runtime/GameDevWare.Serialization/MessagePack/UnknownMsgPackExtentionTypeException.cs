/* 
	Copyright (c) 2026 Denis Zykov, GameDevWare.com

	This a part of "Json & MessagePack Serialization" Unity Asset - https://www.assetstore.unity3d.com/#!/content/59918

	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND 
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE 
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY, 
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE 
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.
	
	This source code is distributed via Unity Asset Store, 
	to use it in your project you should accept Terms of Service and EULA 
	https://unity3d.com/ru/legal/as_terms
*/
using System;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.MessagePack
{
	/// <summary>
	/// Exception that is thrown when an unknown or unsupported MessagePack extension type is encountered during deserialization.
	/// </summary>
	[Serializable]
	public sealed class UnknownMsgPackExtentionTypeException : SerializationException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UnknownMsgPackExtentionTypeException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
		public UnknownMsgPackExtentionTypeException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnknownMsgPackExtentionTypeException"/> class with a specified error message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public UnknownMsgPackExtentionTypeException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnknownMsgPackExtentionTypeException"/> class with an error message indicating the invalid extension type.
		/// </summary>
		/// <param name="invalidExtType">The invalid extension type code encountered.</param>
		public UnknownMsgPackExtentionTypeException(sbyte invalidExtType)
			: base(string.Format("Unknown MessagePack extention type '{0}' was readed from stream.", invalidExtType))
		{
		}

		private UnknownMsgPackExtentionTypeException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
