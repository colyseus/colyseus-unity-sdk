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
using System.Collections.Generic;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization
{
	internal static class ReflectionExtentions
	{
		private static readonly Dictionary<Type, MethodInfo> GetNameMethods = new Dictionary<Type, MethodInfo>();
		private static readonly object[] EmptyArgs = new object[0];

		public static bool IsInstantiationOf(this Type type, Type openGenericType)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (openGenericType == null)
				throw new ArgumentNullException("openGenericType");

			if (openGenericType.IsGenericType && !openGenericType.IsGenericTypeDefinition)
				throw new ArgumentException(string.Format("Type should be open generic type '{0}'.", openGenericType));

			var genericType = type;
			if (type.IsGenericType)
			{
				if (type.IsGenericType && !type.IsGenericTypeDefinition)
					genericType = type.GetGenericTypeDefinition();

				if (genericType == openGenericType || genericType.IsSubclassOf(openGenericType))
					return true;
			}
			// clean
			genericType = null;

			// check interfaces
			foreach (var interfc in type.GetInterfaces())
			{
				genericType = interfc;

				if (!interfc.IsGenericType)
					continue;

				if (!interfc.IsGenericTypeDefinition)
					genericType = interfc.GetGenericTypeDefinition();

				if (genericType == openGenericType || genericType.IsSubclassOf(openGenericType))
					return true;
			}

			if (type.BaseType != null && type.BaseType != typeof (object))
				return IsInstantiationOf(type.BaseType, openGenericType);

			return false;
		}

		public static bool HasMultipleInstantiations(this Type type, Type openGenericType)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (openGenericType == null)
				throw new ArgumentNullException("openGenericType");

			if (openGenericType.IsGenericType && !openGenericType.IsGenericTypeDefinition)
				throw new ArgumentException(string.Format("Type should be open generic type '{0}'.", openGenericType));

			// can't has multiple implementations of class
			if (!openGenericType.IsInterface)
				return false;

			var found = 0;

			var genericType = type;
			if (type.IsGenericType)
			{
				if (type.IsGenericType && !type.IsGenericTypeDefinition)
					genericType = type.GetGenericTypeDefinition();

				if (genericType == openGenericType || genericType.IsSubclassOf(openGenericType))
					found++;
			}
			// clean
			genericType = null;

			// check interfaces
			foreach (var interfc in type.GetInterfaces())
			{
				genericType = interfc;

				if (!interfc.IsGenericType)
					continue;

				if (!interfc.IsGenericTypeDefinition)
					genericType = interfc.GetGenericTypeDefinition();

				if (genericType == openGenericType || genericType.IsSubclassOf(openGenericType))
					found++;
			}


			return found > 1;
		}

		public static Type[] GetInstantiationArguments(this Type type, Type openGenericType)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (openGenericType == null)
				throw new ArgumentNullException("openGenericType");

			if (openGenericType.IsGenericType && !openGenericType.IsGenericTypeDefinition)
				throw new ArgumentException(string.Format("Type should be open generic type '{0}'.", openGenericType));

			var genericType = type;
			if (type.IsGenericType)
			{
				if (type.IsGenericType && !type.IsGenericTypeDefinition)
					genericType = type.GetGenericTypeDefinition();

				if (genericType == openGenericType || genericType.IsSubclassOf(openGenericType))
					return type.GetGenericArguments();
			}

			// clean
			genericType = null;

			// check interfaces
			foreach (var _interface in type.GetInterfaces())
			{
				genericType = _interface;

				if (!_interface.IsGenericType)
					continue;

				if (!_interface.IsGenericTypeDefinition)
					genericType = _interface.GetGenericTypeDefinition();


				if (genericType == openGenericType || genericType.IsSubclassOf(openGenericType))
					return _interface.GetGenericArguments();
			}


			if (type.BaseType != null && type.BaseType != typeof (object))
				return GetInstantiationArguments(type.BaseType, openGenericType);

			return null;
		}

		public static string GetDataMemberName(object dataMemberAttribute)
		{
			if (dataMemberAttribute == null) throw new ArgumentNullException("dataMemberAttribute");

			var type = dataMemberAttribute.GetType();
			var getName = default(MethodInfo);

			lock (GetNameMethods)
			{
				if (!GetNameMethods.TryGetValue(type, out getName))
				{
					getName = type.GetMethod("get_Name", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);

					if (getName == null || getName.ReturnType != typeof (string) || getName.GetParameters().Length != 0)
						getName = null;

					if (getName == null)
					{
						var getNameProperty = type.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public, null, typeof (string),
							Type.EmptyTypes, null);
						if (getNameProperty != null)
							getName = getNameProperty.GetGetMethod(nonPublic: false);
					}

					GetNameMethods.Add(type, getName);
				}
			}

			if (getName != null)
				return (string) getName.Invoke(dataMemberAttribute, EmptyArgs);
			else
				return null;
		}
	}
}
