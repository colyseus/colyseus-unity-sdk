#if UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4 || UNITY_4_7 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

// ReSharper disable once CheckNamespace
namespace GameDevWare.Serialization.Metadata
{
	internal static class MetadataReflection
	{
		/// <summary>
		/// Set to true to disable access optimizations (generated read/write access delegates) in AOT runtime.
		/// </summary>
		public static bool AotRuntime;

		private static readonly Dictionary<MemberInfo, Func<object, object>> ReadFunctions;
		private static readonly Dictionary<MemberInfo, Action<object, object>> WriteFunctions;
		private static readonly Dictionary<MemberInfo, Func<object>> ConstructorFunctions;

		static MetadataReflection()
		{
#if ((UNITY_WEBGL || UNITY_IOS || ENABLE_IL2CPP) && !UNITY_EDITOR)
			AotRuntime = true;
#else
			try
			{
				// try compile expression
				Expression.Lambda<Func<bool>>(Expression.Constant(true)).Compile();
#if (NET35 || UNITY) && !NET_STANDARD_2_0
				var voidDynamicMethod = new DynamicMethod("TestVoidMethod", typeof(void), Type.EmptyTypes, restrictedSkipVisibility: true);
				var il = voidDynamicMethod.GetILGenerator();
				il.Emit(OpCodes.Nop);
				voidDynamicMethod.CreateDelegate(typeof(Action));
#endif
			}
			catch
			{
				AotRuntime = true;
			}
#endif
			ReadFunctions = new Dictionary<MemberInfo, Func<object, object>>();
			WriteFunctions = new Dictionary<MemberInfo, Action<object, object>>();
			ConstructorFunctions = new Dictionary<MemberInfo, Func<object>>();
		}

		public static bool TryGetMemberAccessFunc(MethodInfo getMethod, MethodInfo setMethod, out Func<object, object> getFn, out Action<object, object> setFn)
		{
			getFn = null;
			setFn = null;


			if (AotRuntime)
				return false;

			if (getMethod != null && !getMethod.IsStatic && getMethod.GetParameters().Length == 0)
			{
				lock (ReadFunctions)
				{
					if (ReadFunctions.TryGetValue(getMethod, out getFn) == false)
					{
						var instanceParam = Expression.Parameter(typeof(object), "instance");
						var declaringType = getMethod.DeclaringType;
						Debug.Assert(declaringType != null, "getMethod.DeclaringType != null");
						getFn = Expression.Lambda<Func<object, object>>(
							Expression.Convert(
								Expression.Call(
									Expression.Convert(instanceParam, declaringType), getMethod),
									typeof(object)),
								instanceParam
						).Compile();
						ReadFunctions.Add(getMethod, getFn);
					}
				}
			}

			if (setMethod != null && !setMethod.IsStatic && setMethod.GetParameters().Length == 1 && setMethod.DeclaringType != null && setMethod.DeclaringType.IsValueType == false)
			{
				lock (WriteFunctions)
				{
					if (WriteFunctions.TryGetValue(setMethod, out setFn) == false)
					{
						var instanceParam = Expression.Parameter(typeof(object), "instance");
						var valueParam = Expression.Parameter(typeof(object), "value");
						var declaringType = setMethod.DeclaringType;
						var valueType = setMethod.GetParameters()[0].ParameterType;
						Debug.Assert(declaringType != null, "setMethod.DeclaringType != null");

						setFn = ((Expression<Action<object, object>>)Expression.Lambda(typeof(Action<object, object>),
							Expression.Call(
								Expression.Convert(instanceParam, declaringType),
								setMethod,
								Expression.Convert(valueParam, valueType)),
							instanceParam,
							valueParam
						)).Compile();
						WriteFunctions.Add(setMethod, setFn);
					}
				}
			}
			return true;
		}
		public static bool TryGetMemberAccessFunc(FieldInfo fieldInfo, out Func<object, object> getFn, out Action<object, object> setFn)
		{
			getFn = null;
			setFn = null;

			if (AotRuntime || fieldInfo.IsStatic)
				return false;

			lock (ReadFunctions)
			{
				if (ReadFunctions.TryGetValue(fieldInfo, out getFn) == false)
				{
					var instanceParam = Expression.Parameter(typeof(object), "instance");
					var declaringType = fieldInfo.DeclaringType;
					Debug.Assert(declaringType != null, "getMethodDeclaringType != null");
					getFn = Expression.Lambda<Func<object, object>>(
						Expression.Convert(
							Expression.Field(
								Expression.Convert(instanceParam, declaringType),
								fieldInfo),
								typeof(object)),
							instanceParam
					).Compile();
					ReadFunctions.Add(fieldInfo, getFn);
				}
			}

			if (fieldInfo.IsInitOnly == false && fieldInfo.DeclaringType != null && fieldInfo.DeclaringType.IsValueType == false)
			{
				lock (WriteFunctions)
				{
					if (WriteFunctions.TryGetValue(fieldInfo, out setFn) == false)
					{
						var declaringType = fieldInfo.DeclaringType;
						Debug.Assert(declaringType != null, "fieldInfo.DeclaringType != null");
						var fieldType = fieldInfo.FieldType;
#if (NET35 || UNITY) && !NET_STANDARD_2_0
						var setDynamicMethod = new DynamicMethod(declaringType.FullName + "::" + fieldInfo.Name, typeof(void), new Type[] { typeof(object), typeof(object) }, restrictedSkipVisibility: true);
						var il = setDynamicMethod.GetILGenerator();

						il.Emit(OpCodes.Ldarg_0); // instance
						il.Emit(OpCodes.Castclass, declaringType);
						il.Emit(OpCodes.Ldarg_1); // value
						if (fieldType.IsValueType)
							il.Emit(OpCodes.Unbox_Any, fieldType);
						else
							il.Emit(OpCodes.Castclass, fieldType);
						il.Emit(OpCodes.Stfld, fieldInfo); // call instance.Set(value)
						il.Emit(OpCodes.Ret);

						setFn = (Action<object, object>)setDynamicMethod.CreateDelegate(typeof(Action<object, object>));
#else
						var instanceParam = Expression.Parameter(typeof(object), "instance");
						var valueParam = Expression.Parameter(typeof(object), "value");

						setFn = ((Expression<Action<object, object>>)Expression.Lambda(typeof(Action<object, object>),
							Expression.Assign(
								Expression.Field(Expression.Convert(instanceParam, declaringType), fieldInfo), 
								Expression.Convert(valueParam, fieldType)),
							instanceParam,
							valueParam
						)).Compile();
#endif
						WriteFunctions.Add(fieldInfo, setFn);
					}
				}
			}

			return true;
		}
		public static bool TryGetConstructor(Type type, out Func<object> ctrFn, out ConstructorInfo defaultConstructor)
		{
			if (type == null) throw new ArgumentNullException("type");

			ctrFn = null;
			defaultConstructor = null;

			if (AotRuntime || type.IsAbstract || type.IsInterface)
				return false;

			defaultConstructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(ctr => ctr.GetParameters().Length == 0);

			if (defaultConstructor == null)
				return false;

			lock (ConstructorFunctions)
			{
				if (ConstructorFunctions.TryGetValue(type, out ctrFn))
					return true;

				ctrFn = Expression.Lambda<Func<object>>(
					Expression.Convert(
						Expression.New(defaultConstructor),
						typeof(object))
					).Compile();

				ConstructorFunctions.Add(type, ctrFn);
			}

			return true;
		}
	}
}
