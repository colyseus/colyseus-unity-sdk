/*
	Copyright (c) 2019 Denis Zykov, GameDevWare.com

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
using System.ComponentModel;
using System.Linq;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Metadata
{
	internal abstract class DataMemberDescription : MemberDescription
	{
		public abstract bool CanGet { get; }
		public abstract bool CanSet { get; }
		public object DefaultValue { get; private set; }
		public abstract Type ValueType { get; }

		protected DataMemberDescription(TypeDescription typeDescription, MemberInfo member)
			: base(typeDescription, member)
		{
			var defaultValue =
				(DefaultValueAttribute) this.GetAttributesOrEmptyList(typeof (DefaultValueAttribute)).FirstOrDefault();
			if (defaultValue != null)
				this.DefaultValue = defaultValue.Value;
		}

		public abstract object GetValue(object target);
		public abstract void SetValue(object target, object value);
	}
}
