/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. All rights reserved.
 *
 * This software is subject to the Microsoft Public License (Ms-PL). 
 * A copy of the license can be found in the license.htm file included 
 * in this distribution.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	// TODO: Unit test ModelState interaction with VDD

	public class ViewDataDictionary : IDictionary<string, object>
	{
		readonly Dictionary<string, object> _innerDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		object _model;
		readonly ModelStateDictionary _modelState = new ModelStateDictionary();

		public ViewDataDictionary()
			: this((object)null) {}

		public ViewDataDictionary(object model)
		{
			Model = model;
		}

		public ViewDataDictionary(ViewDataDictionary dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}

			foreach (var entry in dictionary)
			{
				_innerDictionary.Add(entry.Key, entry.Value);
			}
			foreach (var entry in dictionary.ModelState)
			{
				ModelState.Add(entry.Key, entry.Value);
			}
			Model = dictionary.Model;
		}

		public int Count
		{
			get { return _innerDictionary.Count; }
		}

		public bool IsReadOnly
		{
			get { return ((IDictionary<string, object>)_innerDictionary).IsReadOnly; }
		}

		public ICollection<string> Keys
		{
			get { return _innerDictionary.Keys; }
		}

		public object Model
		{
			get { return _model; }
			set { SetModel(value); }
		}

		public ModelStateDictionary ModelState
		{
			get { return _modelState; }
		}

		public object this[string key]
		{
			get
			{
				object value;
				_innerDictionary.TryGetValue(key, out value);
				return value;
			}
			set { _innerDictionary[key] = value; }
		}

		public ICollection<object> Values
		{
			get { return _innerDictionary.Values; }
		}

		public void Add(KeyValuePair<string, object> item)
		{
			((IDictionary<string, object>)_innerDictionary).Add(item);
		}

		public void Add(string key, object value)
		{
			_innerDictionary.Add(key, value);
		}

		public void Clear()
		{
			_innerDictionary.Clear();
		}

		public bool Contains(KeyValuePair<string, object> item)
		{
			return ((IDictionary<string, object>)_innerDictionary).Contains(item);
		}

		public bool ContainsKey(string key)
		{
			return _innerDictionary.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		{
			((IDictionary<string, object>)_innerDictionary).CopyTo(array, arrayIndex);
		}

		public object Eval(string expression)
		{
			if (String.IsNullOrEmpty(expression))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "expression");
			}

			return ViewDataEvaluator.Eval(this, expression);
		}

		public string Eval(string expression, string format)
		{
			var value = Eval(expression);

			if (value == null)
			{
				return String.Empty;
			}

			if (String.IsNullOrEmpty(format))
			{
				return Convert.ToString(value, CultureInfo.CurrentCulture);
			}

			return String.Format(CultureInfo.CurrentCulture, format, value);
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			return _innerDictionary.GetEnumerator();
		}

		public bool Remove(KeyValuePair<string, object> item)
		{
			return ((IDictionary<string, object>)_innerDictionary).Remove(item);
		}

		public bool Remove(string key)
		{
			return _innerDictionary.Remove(key);
		}

		// This method will execute before the derived type's instance constructor executes. Derived types must
		// be aware of this and should plan accordingly. For example, the logic in SetModel() should be simple
		// enough so as not to depend on the "this" pointer referencing a fully constructed object.
		protected virtual void SetModel(object value)
		{
			_model = value;
		}

		public bool TryGetValue(string key, out object value)
		{
			return _innerDictionary.TryGetValue(key, out value);
		}

		internal static class ViewDataEvaluator
		{
			public static object Eval(ViewDataDictionary vdd, string expression)
			{
				//Given an expression "foo.bar.baz" we look up the following (pseudocode):
				//  this["foo.bar.baz.quux"]
				//  this["foo.bar.baz"]["quux"]
				//  this["foo.bar"]["baz.quux]
				//  this["foo.bar"]["baz"]["quux"]
				//  this["foo"]["bar.baz.quux"]
				//  this["foo"]["bar.baz"]["quux"]
				//  this["foo"]["bar"]["baz.quux"]
				//  this["foo"]["bar"]["baz"]["quux"]

				var evaluated = EvalComplexExpression(vdd, expression);
				return evaluated;
			}

			static object EvalComplexExpression(object indexableObject, string expression)
			{
				foreach (var expressionPair in GetRightToLeftExpressions(expression))
				{
					var subExpression = expressionPair.Left;
					var postExpression = expressionPair.Right;

					var subtarget = GetPropertyValue(indexableObject, subExpression);
					if (subtarget != null)
					{
						if (String.IsNullOrEmpty(postExpression))
							return subtarget;

						var potential = EvalComplexExpression(subtarget, postExpression);
						if (potential != null)
						{
							return potential;
						}
					}
				}
				return null;
			}

			static IEnumerable<ExpressionPair> GetRightToLeftExpressions(string expression)
			{
				// Produces an enumeration of all the combinations of complex property names
				// given a complex expression. See the list above for an example of the result
				// of the enumeration.

				yield return new ExpressionPair(expression, String.Empty);

				var lastDot = expression.LastIndexOf('.');

				var subExpression = expression;
				var postExpression = string.Empty;

				while (lastDot > -1)
				{
					subExpression = expression.Substring(0, lastDot);
					postExpression = expression.Substring(lastDot + 1);
					yield return new ExpressionPair(subExpression, postExpression);

					lastDot = subExpression.LastIndexOf('.');
				}
			}

			static object GetIndexedPropertyValue(object indexableObject, string key)
			{
				var indexableType = indexableObject.GetType();

				var vdd = indexableObject as ViewDataDictionary;
				if (vdd != null)
				{
					return vdd[key];
				}

				var containsKeyMethod = indexableType.GetMethod("ContainsKey", BindingFlags.Public | BindingFlags.Instance, null,
				                                                new[] {typeof(string)}, null);
				if (containsKeyMethod != null)
				{
					if (!(bool)containsKeyMethod.Invoke(indexableObject, new object[] {key}))
					{
						return null;
					}
				}

				var info = indexableType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance, null, null,
				                                     new[] {typeof(string)}, null);
				if (info != null)
				{
					return info.GetValue(indexableObject, new object[] {key});
				}

				var objectInfo = indexableType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance, null, null,
				                                           new[] {typeof(object)}, null);
				if (objectInfo != null)
				{
					return objectInfo.GetValue(indexableObject, new object[] {key});
				}
				return null;
			}

			static object GetPropertyValue(object container, string propertyName)
			{
				// This method handles one "segment" of a complex property expression

				// First, we try to evaluate the property based on its indexer
				var value = GetIndexedPropertyValue(container, propertyName);
				if (value != null)
				{
					return value;
				}

				// If the indexer didn't return anything useful, continue...

				// If the container is a ViewDataDictionary then treat its Model property
				// as the container instead of the ViewDataDictionary itself.
				var vdd = container as ViewDataDictionary;
				if (vdd != null)
				{
					container = vdd.Model;
				}

				// Second, we try to use PropertyDescriptors and treat the expression as a property name
				var descriptor = TypeDescriptor.GetProperties(container).Find(propertyName, true);
				if (descriptor == null)
				{
					return null;
				}

				return descriptor.GetValue(container);
			}

			struct ExpressionPair
			{
				public readonly string Left;
				public readonly string Right;

				public ExpressionPair(string left, string right)
				{
					Left = left;
					Right = right;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_innerDictionary).GetEnumerator();
		}
	}
}