using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Colyseus.Schema
{
	/// <summary>
	///     Helper class to extract property chains from nested member expressions
	/// </summary>
	internal static class ExpressionHelper
	{
		/// <summary>
		///     Extracts the property chain from a nested member expression.
		///     For example, `state => state.staticEntities.entities` returns ["staticEntities", "entities"]
		/// </summary>
		public static List<string> GetPropertyChain(Expression expression)
		{
			var chain = new List<string>();
			var current = expression;

			while (current is MemberExpression memberExpr)
			{
				chain.Insert(0, memberExpr.Member.Name);
				current = memberExpr.Expression;
			}

			return chain;
		}

		/// <summary>
		///     Gets the root instance from an expression by walking up to the parameter.
		/// </summary>
		public static Expression GetRootExpression(Expression expression)
		{
			var current = expression;
			while (current is MemberExpression memberExpr)
			{
				current = memberExpr.Expression;
			}
			return current;
		}
	}

	/// <summary>
	///     Delegate for handling events given a <paramref name="key" /> and a <paramref name="value" />
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="currentValue"></param>
	/// <param name="previousValue"></param>
	public delegate void PropertyChangeEventHandler<T>(T currentValue, T previousValue);

	/// <summary>
	///     Delegate function when any property on the schema structure has changed.
	/// </summary>
	public delegate void OnInstanceChangeEventHandler();

	/// <summary>
	///     Delegate function for handling <see cref="Schema" /> removal
	/// </summary>
	public delegate void OnRemoveEventHandler();

	/// <summary>
	///     Delegate for handling events given a <paramref name="key" /> and a <paramref name="value" />
	/// </summary>
	/// <param name="value">The affected value</param>
	/// <param name="key">The key we're affecting</param>
	/// <typeparam name="K">The type of <see cref="object" /> we're attempting to access</typeparam>
	/// <typeparam name="T">The <see cref="Schema" /> type</typeparam>
	public delegate void KeyValueEventHandler<K, T>(K key, T value);

	public class StateCallbackStrategy<TState>
		where TState : Schema
	{
		protected Decoder<TState> Decoder;

		protected HashSet<int> UniqueRefIds = new HashSet<int>();

		protected bool isTriggering = false;

		public StateCallbackStrategy(Decoder<TState> decoder)
		{
			Decoder = decoder;
			Decoder.TriggerChanges = TriggerChanges;
		}

		protected Action AddCallback(int refId, object operationOrProperty, Delegate handler)
		{
			if (!Decoder.Refs.callbacks.TryGetValue(refId, out var handlers))
			{
				handlers = new Dictionary<object, List<Delegate>>();
				Decoder.Refs.callbacks[refId] = handlers;
			}
			if (!handlers.ContainsKey(operationOrProperty))
			{
				handlers[operationOrProperty] = new List<Delegate>();
			}
			handlers[operationOrProperty].Add(handler);
			return () => handlers[operationOrProperty].Remove(handler);
		}

		/// <summary>
		///     Navigates through a nested property chain and invokes a callback when the final property is reached.
		///     Automatically listens to intermediate properties and re-attaches handlers when they change.
		/// </summary>
		/// <param name="instance">The root instance to start navigation from</param>
		/// <param name="propertyChain">List of property names to navigate through</param>
		/// <param name="currentIndex">Current position in the property chain</param>
		/// <param name="onFinalProperty">Callback invoked with the final Schema instance and property name</param>
		/// <param name="immediate">Whether to trigger immediately if available</param>
		/// <returns>Action to remove all attached listeners</returns>
		protected Action NavigateNestedProperties(Schema instance, List<string> propertyChain, int currentIndex, Func<Schema, string, Action> onFinalProperty, bool immediate = true)
		{
			if (currentIndex >= propertyChain.Count)
			{
				return () => { };
			}

			var propertyName = propertyChain[currentIndex];
			var isLastProperty = currentIndex == propertyChain.Count - 1;

			if (isLastProperty)
			{
				// We've reached the final property, invoke the callback
				return onFinalProperty(instance, propertyName);
			}

			// We're at an intermediate property, need to navigate deeper
			Action removeHandler = () => { };
			Action removeListener = () => removeHandler();

			var currentValue = instance[propertyName];

			if (currentValue == null)
			{
				// Intermediate property not available yet, listen for it
				removeHandler = AddCallback(instance.__refId, propertyName, new PropertyChangeEventHandler<Schema>((newValue, prevValue) =>
				{
					// Remove previous deep listener if any
					removeHandler();

					if (newValue != null)
					{
						// Navigate to the next level
						removeHandler = NavigateNestedProperties(newValue, propertyChain, currentIndex + 1, onFinalProperty, immediate);
					}
				}));
			}
			else if (currentValue is Schema schemaValue)
			{
				// Intermediate property is available, navigate deeper
				removeHandler = NavigateNestedProperties(schemaValue, propertyChain, currentIndex + 1, onFinalProperty, immediate);

				// Also listen for changes to re-attach handlers
				var removeChangeListener = AddCallback(instance.__refId, propertyName, new PropertyChangeEventHandler<Schema>((newValue, prevValue) =>
				{
					// Remove previous deep listener
					removeHandler();

					if (newValue != null)
					{
						// Navigate to the next level with the new value
						removeHandler = NavigateNestedProperties(newValue, propertyChain, currentIndex + 1, onFinalProperty, immediate);
					}
				}));

				var originalRemoveHandler = removeHandler;
				removeHandler = () =>
				{
					originalRemoveHandler();
					removeChangeListener();
				};
			}

			return removeListener;
		}

		protected Action AddCallbackOrWaitCollectionAvailable<TInstance, TReturn>(TInstance instance, Expression<Func<TInstance, TReturn>> propertyExpression, OPERATION operation, Delegate handler, bool immediate = true)
			where TInstance : Schema
			where TReturn : IRef
		{
			var propertyChain = ExpressionHelper.GetPropertyChain(propertyExpression.Body);

			// If only one property in chain, use the original simple logic
			if (propertyChain.Count == 1)
			{
				return AddCallbackOrWaitCollectionAvailableSimple(instance, propertyChain[0], operation, handler, immediate);
			}

			// For nested properties, use the navigation helper
			Action removeHandler = () => { };
			Action removeAll = () => removeHandler();

			removeHandler = NavigateNestedProperties(instance, propertyChain, 0, (finalInstance, finalPropertyName) =>
			{
				return AddCallbackOrWaitCollectionAvailableSimple(finalInstance, finalPropertyName, operation, handler, immediate);
			}, immediate);

			return removeAll;
		}

		/// <summary>
		///     Original simple implementation for single-level property access
		/// </summary>
		protected Action AddCallbackOrWaitCollectionAvailableSimple(Schema instance, string propertyName, OPERATION operation, Delegate handler, bool immediate = true)
		{
			Action removeHandler = () => { };
			Action removeOnAdd = () => removeHandler();

			// Collection not available yet. Listen for its availability before attaching the handler.
			if (instance[propertyName] == null)
			{
				removeHandler = AddCallback(instance.__refId, propertyName, new PropertyChangeEventHandler<IRef>((IRef collection, IRef _) =>
				{
					removeHandler = AddCallback(collection.__refId, operation, handler);
				}));
				return removeOnAdd;
			}
			else
			{
				//
				// Call immediately if collection is already available, if it's an ADD operation.
				//
				immediate = immediate && isTriggering == false;

				if (operation == OPERATION.ADD && immediate)
				{
					((ISchemaCollection)instance[propertyName]).ForEach((key, value) =>
					{
						handler.DynamicInvoke(key, value);
					});
				}

				return AddCallback(((IRef)instance[propertyName]).__refId, operation, handler);
			}
		}

		public Action Listen<TReturn>(Expression<Func<TState, TReturn>> propertyExpression, PropertyChangeEventHandler<TReturn> handler, bool immediate = true)
		{
			return Listen(Decoder.State, propertyExpression, handler, immediate);
		}

		public Action Listen<TInstance, TReturn>(TInstance instance, Expression<Func<TInstance, TReturn>> propertyExpression, PropertyChangeEventHandler<TReturn> handler, bool immediate = true)
			where TInstance : Schema
		{
			var propertyChain = ExpressionHelper.GetPropertyChain(propertyExpression.Body);

			// If only one property in chain, use the original simple logic
			if (propertyChain.Count == 1)
			{
				return ListenSimple(instance, propertyChain[0], handler, immediate);
			}

			// For nested properties, use the navigation helper
			Action removeHandler = () => { };
			Action removeAll = () => removeHandler();

			removeHandler = NavigateNestedProperties(instance, propertyChain, 0, (finalInstance, finalPropertyName) =>
			{
				return ListenSimple(finalInstance, finalPropertyName, handler, immediate);
			}, immediate);

			return removeAll;
		}

		/// <summary>
		///     Original simple implementation for single-level property listening
		/// </summary>
		protected Action ListenSimple<TReturn>(Schema instance, string propertyName, PropertyChangeEventHandler<TReturn> handler, bool immediate = true)
		{
			immediate = immediate && isTriggering == false;

			//
			// Call handler immediately if property is already available.
			//
			if (immediate && instance[propertyName] != null && !instance[propertyName].Equals(default(TReturn)))
			{
				handler((TReturn)instance[propertyName], default(TReturn));
			}

			return AddCallback(instance.__refId, propertyName, handler);
		}

		public Action OnChange<T>(T instance, OnInstanceChangeEventHandler handler)
			where T : Schema
		{
			return AddCallback(instance.__refId, OPERATION.REPLACE, handler);
		}

		public Action OnAdd<TReturn>(Expression<Func<TState, ArraySchema<TReturn>>> propertyExpression, KeyValueEventHandler<int, TReturn> handler, bool immediate = true)
		{
			return OnAdd(Decoder.State, propertyExpression, handler, immediate);
		}

		public Action OnAdd<TInstance, TReturn>(TInstance instance, Expression<Func<TInstance, ArraySchema<TReturn>>> propertyExpression, KeyValueEventHandler<int, TReturn> handler, bool immediate = true)
			where TInstance : Schema
		{
			return AddCallbackOrWaitCollectionAvailable(instance, propertyExpression, OPERATION.ADD, handler, immediate);
		}

		public Action OnAdd<TReturn>(Expression<Func<TState, MapSchema<TReturn>>> propertyExpression, KeyValueEventHandler<string, TReturn> handler, bool immediate = true)
		{
			return OnAdd(Decoder.State, propertyExpression, handler, immediate);
		}

		public Action OnAdd<TInstance, TReturn>(TInstance instance, Expression<Func<TInstance, MapSchema<TReturn>>> propertyExpression, KeyValueEventHandler<string, TReturn> handler, bool immediate = true)
			where TInstance : Schema
		{
			return AddCallbackOrWaitCollectionAvailable(instance, propertyExpression, OPERATION.ADD, handler, immediate);
		}

		public Action OnChange<TReturn>(Expression<Func<TState, ArraySchema<TReturn>>> propertyExpression, KeyValueEventHandler<int, TReturn> handler)
		{
			return OnChange(Decoder.State, propertyExpression, handler);
		}

		public Action OnChange<TInstance, TReturn>(TInstance instance, Expression<Func<TInstance, ArraySchema<TReturn>>> propertyExpression, KeyValueEventHandler<int, TReturn> handler)
			where TInstance : Schema
		{
			return AddCallbackOrWaitCollectionAvailable(instance, propertyExpression, OPERATION.REPLACE, handler);
		}

		public Action OnChange<TReturn>(Expression<Func<TState, MapSchema<TReturn>>> propertyExpression, KeyValueEventHandler<string, TReturn> handler)
		{
			return OnChange(Decoder.State, propertyExpression, handler);
		}

		public Action OnChange<TInstance, TReturn>(TInstance instance, Expression<Func<TInstance, MapSchema<TReturn>>> propertyExpression, KeyValueEventHandler<string, TReturn> handler)
			where TInstance : Schema
		{
			return AddCallbackOrWaitCollectionAvailable(instance, propertyExpression, OPERATION.REPLACE, handler);
		}

		public Action OnRemove<TReturn>(Expression<Func<TState, ArraySchema<TReturn>>> propertyExpression, KeyValueEventHandler<int, TReturn> handler)
		{
			return OnRemove(Decoder.State, propertyExpression, handler);
		}

		public Action OnRemove<TInstance, TReturn>(TInstance instance, Expression<Func<TInstance, ArraySchema<TReturn>>> propertyExpression, KeyValueEventHandler<int, TReturn> handler)
			where TInstance : Schema
		{
			return AddCallbackOrWaitCollectionAvailable(instance, propertyExpression, OPERATION.DELETE, handler);
		}

		public Action OnRemove<TReturn>(Expression<Func<TState, MapSchema<TReturn>>> propertyExpression, KeyValueEventHandler<string, TReturn> handler)
		{
			return OnRemove(Decoder.State, propertyExpression, handler);
		}

		public Action OnRemove<TInstance, TReturn>(TInstance instance, Expression<Func<TInstance, MapSchema<TReturn>>> propertyExpression, KeyValueEventHandler<string, TReturn> handler)
			where TInstance : Schema
		{
			return AddCallbackOrWaitCollectionAvailable(instance, propertyExpression, OPERATION.DELETE, handler);
		}

		// ...

		/// <summary>
		/// 	Binds a schema property to a target object.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public Action BindTo<T>(Schema from, T to, bool immediate = true)
		{
			var action = (Action)(() =>
			{
				var toType = typeof(T);
				foreach (var field in from.fieldsByIndex)
				{
					var fromValue = from.GetType().GetProperty(field.Value)?.GetValue(from);
					var toProperty = toType.GetProperty(field.Value);

					if (toProperty != null && fromValue != null)
					{
						if (toProperty.PropertyType.IsAssignableFrom(fromValue.GetType()))
						{
							toProperty.SetValue(to, fromValue);
						}
						else
						{
							// Handle type mismatch, maybe convert or log
							UnityEngine.Debug.Log($"BindTo: Type mismatch for property {field.Value}: Cannot assign {fromValue.GetType().Name} to {toProperty.PropertyType.Name}");
						}
					}
				}
			});
			if (immediate) {
				action();
			}
			return AddCallback(from.__refId, to, action);
		}

		protected void TriggerChanges(ref List<DataChange> allChanges)
		{
			UniqueRefIds.Clear();

			foreach (DataChange change in allChanges)
			{
				var refId = change.RefId;
				var _ref = Decoder.Refs.Get(refId);

				// Dictionary<object, List<Delegate>> callbacks = Decoder.Refs.callbacks[refId];
				Decoder.Refs.callbacks.TryGetValue(refId, out var callbacks);

				if (callbacks == null)
				{
					continue;
				}

				//
				// trigger onRemove on child structure.
				//
				if ((change.Op & (byte)OPERATION.DELETE) == (byte)OPERATION.DELETE && change.PreviousValue is Schema)
				{
					Decoder.Refs.callbacks.TryGetValue(((Schema)change.PreviousValue).__refId, out var deleteCallbacks);
					if (deleteCallbacks != null
					    && deleteCallbacks.ContainsKey(OPERATION.DELETE)
					    && deleteCallbacks[OPERATION.DELETE] != null)
					{
						foreach (var callback in deleteCallbacks[OPERATION.DELETE])
						{
							callback.DynamicInvoke();
						}
					}
				}

				if (_ref is Schema)
				{
					//
					// Handle Schema instance
					//

					if (!UniqueRefIds.Contains(refId))
					{
						// trigger onChange
						callbacks.TryGetValue(OPERATION.REPLACE, out var replaceCallbacks);
						if (replaceCallbacks != null)
						{
							foreach (var callback in replaceCallbacks)
							{
								try
								{
									callback.DynamicInvoke();
								}
								catch (Exception e)
								{
									UnityEngine.Debug.LogError(e.Message);
								}
							}
						}
					}

					callbacks.TryGetValue(change.Field, out var fieldCallbacks);
					if (fieldCallbacks != null)
					{
						foreach (var callback in fieldCallbacks)
						{
							try
							{
								isTriggering = true;
								callback.DynamicInvoke(change.Value, change.PreviousValue);
							}
							catch (Exception e)
							{
								UnityEngine.Debug.LogError(e.Message);
							} finally
							{
								isTriggering = false;
							}
						}
					}
				}
				else
				{
					//
					// Handle collection of items
					//
					ISchemaCollection container = (ISchemaCollection)_ref;

					if ((change.Op & (byte)OPERATION.DELETE) == (byte)OPERATION.DELETE)
					{
						if (change.PreviousValue != container.GetTypeDefaultValue())
						{
							// trigger onRemove
							callbacks.TryGetValue(OPERATION.DELETE, out var deleteCallbacks);
							if (deleteCallbacks != null)
							{
								foreach (var callback in deleteCallbacks)
								{
									callback.DynamicInvoke(change.DynamicIndex ?? change.Field, change.PreviousValue);
								}
							}
						}

						// Handle DELETE_AND_ADD operation
						if ((change.Op & (byte)OPERATION.ADD) == (byte)OPERATION.ADD)
						{
							// trigger onRemove
							callbacks.TryGetValue(OPERATION.ADD, out var addCallbacks);
							if (addCallbacks != null)
							{
								isTriggering = true;
								foreach (var callback in addCallbacks)
								{
									callback.DynamicInvoke(change.DynamicIndex, change.Value);
								}
								isTriggering = false;
							}
						}

					}
					else if (
						(change.Op & (byte)OPERATION.ADD) == (byte)OPERATION.ADD &&
						change.PreviousValue != change.Value
					)
					{
						// trigger onAdd
						callbacks.TryGetValue(OPERATION.ADD, out var addCallbacks);
						if (addCallbacks != null)
						{
							isTriggering = true;
							foreach (var callback in addCallbacks)
							{
								callback.DynamicInvoke(change.DynamicIndex ?? change.Field, change.Value);
							}
							isTriggering = false;
						}
					}

					// trigger onChange
					if (change.Value != change.PreviousValue)
					{
						callbacks.TryGetValue(OPERATION.REPLACE, out var replaceCallbacks);
						if (replaceCallbacks != null)
						{
							foreach (var callback in replaceCallbacks)
							{
								callback.DynamicInvoke(change.DynamicIndex ?? change.Field, change.Value);
							}
						}
					}
				}

				UniqueRefIds.Add(refId);
			}
		}
	}

	public class Callbacks // <T> where T : Schema
	{
		public static StateCallbackStrategy<T> Get<T>(Room<T> room)
			where T : Schema
		{
			var decoder = (room.Serializer as SchemaSerializer<T>).Decoder;
			return new StateCallbackStrategy<T>(decoder);
		}

		public static StateCallbackStrategy<T> Get<T>(Decoder<T> decoder)
			where T : Schema
		{
			return new StateCallbackStrategy<T>(decoder);
		}

		internal static void RemoveChildRefs(ISchemaCollection collection, List<DataChange> changes, ReferenceTracker refs)
		{
			if (refs == null) {
				return;
			}

			collection.ForEach((key, value) => {
				changes.Add(new DataChange
				{
					RefId = collection.__refId,
					Op = (byte)OPERATION.DELETE,
					//Field = item.Key,
					DynamicIndex = key,
					Value = null,
					PreviousValue = value
				});

				if (collection.HasSchemaChild)
				{
					refs.Remove((value as IRef).__refId);
				}
			});
		}
	}
}
