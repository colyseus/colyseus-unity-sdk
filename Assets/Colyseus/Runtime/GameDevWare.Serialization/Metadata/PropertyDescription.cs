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
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Metadata
{
	internal sealed class PropertyDescription : DataMemberDescription
	{
		private readonly PropertyInfo propertyInfo;
		private readonly Func<object, object> getFn;
		private readonly Action<object, object> setFn;
		private readonly MethodInfo getMethod;
		private readonly MethodInfo setMethod;

		public override bool CanGet { get { return this.getMethod != null; } }
		public override bool CanSet { get { return this.setMethod != null; } }
		public override Type ValueType { get { return this.propertyInfo.PropertyType; } }

		public PropertyDescription(TypeDescription typeDescription, PropertyInfo propertyInfo)
			: base(typeDescription, propertyInfo)
		{
			if (propertyInfo == null) throw new ArgumentNullException("propertyInfo");

			this.propertyInfo = propertyInfo;

			this.getMethod = propertyInfo.GetGetMethod(nonPublic: true);
			this.setMethod = propertyInfo.GetSetMethod(nonPublic: true);

			MetadataReflection.TryGetMemberAccessFunc(this.getMethod, this.setMethod, out this.getFn, out this.setFn);
		}

		public override object GetValue(object target)
		{
			if (!this.CanGet) throw new InvalidOperationException("Property is write-only.");

			if (this.getFn != null)
				return this.getFn(target);
			else
				return this.getMethod.Invoke(target, null);
		}
		public override void SetValue(object target, object value)
		{
			if (!this.CanSet) throw new InvalidOperationException("Property is read-only.");

			if (this.setFn != null)
				this.setFn(target, value);
			else
				this.setMethod.Invoke(target, new object[] { value });
		}
	}
}
