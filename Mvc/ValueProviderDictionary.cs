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
using System.Globalization;

namespace System.Web.Mvc
{
	public class ValueProviderDictionary : IDictionary<string, ValueProviderResult>
	{
		readonly Dictionary<string, ValueProviderResult> _dictionary =
			new Dictionary<string, ValueProviderResult>(StringComparer.OrdinalIgnoreCase);

		public ValueProviderDictionary(ControllerContext controllerContext)
		{
			ControllerContext = controllerContext;
			if (controllerContext != null)
			{
				PopulateDictionary();
			}
		}

		public ControllerContext ControllerContext { get; private set; }

		public int Count
		{
			get { return ((ICollection<KeyValuePair<string, ValueProviderResult>>)Dictionary).Count; }
		}

		internal Dictionary<string, ValueProviderResult> Dictionary
		{
			get { return _dictionary; }
		}

		public bool IsReadOnly
		{
			get { return ((ICollection<KeyValuePair<string, ValueProviderResult>>)Dictionary).IsReadOnly; }
		}

		public ICollection<string> Keys
		{
			get { return Dictionary.Keys; }
		}

		public ValueProviderResult this[string key]
		{
			get
			{
				ValueProviderResult result;
				Dictionary.TryGetValue(key, out result);
				return result;
			}
			set { Dictionary[key] = value; }
		}

		public ICollection<ValueProviderResult> Values
		{
			get { return Dictionary.Values; }
		}

		public void Add(KeyValuePair<string, ValueProviderResult> item)
		{
			((ICollection<KeyValuePair<string, ValueProviderResult>>)Dictionary).Add(item);
		}

		public void Add(string key, ValueProviderResult value)
		{
			Dictionary.Add(key, value);
		}

		void AddToDictionaryIfNotPresent(string key, ValueProviderResult result)
		{
			if (!String.IsNullOrEmpty(key))
			{
				if (!Dictionary.ContainsKey(key))
				{
					Dictionary.Add(key, result);
				}
			}
		}

		public void Clear()
		{
			((ICollection<KeyValuePair<string, ValueProviderResult>>)Dictionary).Clear();
		}

		public bool Contains(KeyValuePair<string, ValueProviderResult> item)
		{
			return ((ICollection<KeyValuePair<string, ValueProviderResult>>)Dictionary).Contains(item);
		}

		public bool ContainsKey(string key)
		{
			return Dictionary.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<string, ValueProviderResult>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<string, ValueProviderResult>>)Dictionary).CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<string, ValueProviderResult>> GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<string, ValueProviderResult>>)Dictionary).GetEnumerator();
		}

		void PopulateDictionary()
		{
			var currentCulture = CultureInfo.CurrentCulture;
			var invariantCulture = CultureInfo.InvariantCulture;

			// We use this order of precedence to populate the dictionary:
			// 1. Request form submission (should be culture-aware)
			// 2. Values from the RouteData (could be from the typed-in URL or from the route's default values)
			// 3. URI query string

			var form = ControllerContext.HttpContext.Request.Form;
			if (form != null)
			{
				var keys = form.AllKeys;
				foreach (var key in keys)
				{
					var rawValue = form.GetValues(key);
					var attemptedValue = form[key];
					var result = new ValueProviderResult(rawValue, attemptedValue, currentCulture);
					AddToDictionaryIfNotPresent(key, result);
				}
			}

			var routeValues = ControllerContext.RouteData.Values;
			if (routeValues != null)
			{
				foreach (var kvp in routeValues)
				{
					var key = kvp.Key;
					var rawValue = kvp.Value;
					var attemptedValue = Convert.ToString(rawValue, invariantCulture);
					var result = new ValueProviderResult(rawValue, attemptedValue, invariantCulture);
					AddToDictionaryIfNotPresent(key, result);
				}
			}

			var queryString = ControllerContext.HttpContext.Request.QueryString;
			if (queryString != null)
			{
				var keys = queryString.AllKeys;
				foreach (var key in keys)
				{
					var rawValue = queryString.GetValues(key);
					var attemptedValue = queryString[key];
					var result = new ValueProviderResult(rawValue, attemptedValue, invariantCulture);
					AddToDictionaryIfNotPresent(key, result);
				}
			}
		}

		public bool Remove(KeyValuePair<string, ValueProviderResult> item)
		{
			return ((ICollection<KeyValuePair<string, ValueProviderResult>>)Dictionary).Remove(item);
		}

		public bool Remove(string key)
		{
			return Dictionary.Remove(key);
		}

		public bool TryGetValue(string key, out ValueProviderResult value)
		{
			return Dictionary.TryGetValue(key, out value);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)Dictionary).GetEnumerator();
		}
	}
}