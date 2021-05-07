/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

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
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Metadata
{
	internal sealed class FieldDescription : DataMemberDescription
	{
		private readonly FieldInfo fieldInfo;
		private readonly Func<object, object> getFn;
		private readonly Action<object, object> setFn;

		public override bool CanGet { get { return true; } }
		public override bool CanSet { get { return this.fieldInfo.IsInitOnly == false; } }
		public override Type ValueType { get { return this.fieldInfo.FieldType; } }

		public FieldDescription(TypeDescription typeDescription, FieldInfo fieldInfo)
			: base(typeDescription, fieldInfo)
		{
			if (fieldInfo == null) throw new ArgumentNullException("fieldInfo");

			this.fieldInfo = fieldInfo;

			GettersAndSetters.TryGetAssessors(fieldInfo, out this.getFn, out this.setFn);

		}

		public override object GetValue(object target)
		{
			if (!this.CanGet) throw new InvalidOperationException("Field is write-only.");

			if (this.getFn != null)
				return this.getFn(target);
			else
				return fieldInfo.GetValue(target);
		}

		public override void SetValue(object target, object value)
		{
			if (!this.CanSet) throw new InvalidOperationException("Field is read-only.");

			if (this.setFn != null)
				this.setFn(target, value);
			else
				this.fieldInfo.SetValue(target, value);
		}
	}
}
