﻿/* ****************************************************************************
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
using System.Runtime.Serialization;

namespace System.Web.Mvc
{
	[Serializable]
	public class TempDataDictionary : IDictionary<string, object>, ISerializable
	{
		internal const string _tempDataSerializationKey = "__tempData";

		internal Dictionary<string, object> _data;
		HashSet<string> _initialKeys;
		readonly HashSet<string> _modifiedKeys;

		public TempDataDictionary()
		{
			_initialKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			_modifiedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			_data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}

		protected TempDataDictionary(SerializationInfo info, StreamingContext context)
		{
			_initialKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			_modifiedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			_data = info.GetValue(_tempDataSerializationKey, typeof(Dictionary<string, object>)) as Dictionary<string, object>;
		}

		public int Count
		{
			get { return _data.Count; }
		}

		public Dictionary<string, object>.KeyCollection Keys
		{
			get { return _data.Keys; }
		}

		public void Load(ControllerContext controllerContext, ITempDataProvider tempDataProvider)
		{
			var providerDictionary = tempDataProvider.LoadTempData(controllerContext);
			_data = (providerDictionary != null)
			        	? new Dictionary<string, object>(providerDictionary, StringComparer.OrdinalIgnoreCase)
			        	:
			        		new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			_initialKeys = new HashSet<string>(_data.Keys);
			_modifiedKeys.Clear();
		}

		public void Save(ControllerContext controllerContext, ITempDataProvider tempDataProvider)
		{
			if (_modifiedKeys.Count > 0)
			{
				// Apply change tracking.
				foreach (var x in _initialKeys)
				{
					if (!_modifiedKeys.Contains(x))
					{
						_data.Remove(x);
					}
				}

				// Store the dictionary
				tempDataProvider.SaveTempData(controllerContext, _data);
			}
		}

		public Dictionary<string, object>.ValueCollection Values
		{
			get { return _data.Values; }
		}

		public object this[string key]
		{
			get
			{
				object value;
				if (TryGetValue(key, out value))
				{
					return value;
				}
				return null;
			}
			set
			{
				_data[key] = value;
				_modifiedKeys.Add(key);
			}
		}

		public void Add(string key, object value)
		{
			_data.Add(key, value);
			_modifiedKeys.Add(key);
		}

		public void Clear()
		{
			_data.Clear();
			_modifiedKeys.Clear();
			_initialKeys.Clear();
		}

		public bool ContainsKey(string key)
		{
			return _data.ContainsKey(key);
		}

		public bool ContainsValue(object value)
		{
			return _data.ContainsValue(value);
		}

		public Dictionary<string, object>.Enumerator GetEnumerator()
		{
			return _data.GetEnumerator();
		}

		protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(_tempDataSerializationKey, _data);
		}

		public bool Remove(string key)
		{
			_initialKeys.Remove(key);
			_modifiedKeys.Remove(key);
			return _data.Remove(key);
		}

		public bool TryGetValue(string key, out object value)
		{
			return _data.TryGetValue(key, out value);
		}

		ICollection<string> IDictionary<string, object>.Keys
		{
			get { return ((IDictionary<string, object>)_data).Keys; }
		}

		ICollection<object> IDictionary<string, object>.Values
		{
			get { return ((IDictionary<string, object>)_data).Values; }
		}

		IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<string, object>>)_data).GetEnumerator();
		}

		bool ICollection<KeyValuePair<string, object>>.IsReadOnly
		{
			get { return ((ICollection<KeyValuePair<string, object>>)_data).IsReadOnly; }
		}

		void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int index)
		{
			((ICollection<KeyValuePair<string, object>>)_data).CopyTo(array, index);
		}

		void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> keyValuePair)
		{
			_modifiedKeys.Add(keyValuePair.Key);
			((ICollection<KeyValuePair<string, object>>)_data).Add(keyValuePair);
		}

		bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> keyValuePair)
		{
			return ((ICollection<KeyValuePair<string, object>>)_data).Contains(keyValuePair);
		}

		bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> keyValuePair)
		{
			_modifiedKeys.Remove(keyValuePair.Key);
			return ((ICollection<KeyValuePair<string, object>>)_data).Remove(keyValuePair);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_data).GetEnumerator();
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			GetObjectData(info, context);
		}
	}
}