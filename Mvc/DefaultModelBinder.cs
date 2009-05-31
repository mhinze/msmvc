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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	public class DefaultModelBinder : IModelBinder
	{
		ModelBinderDictionary _binders;
		static string _resourceClassKey;

		[SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly",
			Justification = "Property is settable so that the dictionary can be provided for unit testing purposes.")]
		protected internal ModelBinderDictionary Binders
		{
			get
			{
				if (_binders == null)
				{
					_binders = ModelBinders.Binders;
				}
				return _binders;
			}
			set { _binders = value; }
		}

		public static string ResourceClassKey
		{
			get { return _resourceClassKey ?? String.Empty; }
			set { _resourceClassKey = value; }
		}

		internal void BindComplexElementalModel(ControllerContext controllerContext, ModelBindingContext bindingContext,
		                                        object model)
		{
			// need to replace the property filter + model object and create an inner binding context
			var bindAttr = (BindAttribute)TypeDescriptor.GetAttributes(bindingContext.ModelType)[typeof(BindAttribute)];
			var newPropertyFilter = (bindAttr != null)
			                        	? propertyName =>
			                        	  bindAttr.IsPropertyAllowed(propertyName) && bindingContext.PropertyFilter(propertyName)
			                        	: bindingContext.PropertyFilter;

			var newBindingContext = new ModelBindingContext
			{
				Model = model,
				ModelName = bindingContext.ModelName,
				ModelState = bindingContext.ModelState,
				ModelType = bindingContext.ModelType,
				PropertyFilter = newPropertyFilter,
				ValueProvider = bindingContext.ValueProvider
			};

			// validation
			if (OnModelUpdating(controllerContext, newBindingContext))
			{
				BindProperties(controllerContext, newBindingContext);
				OnModelUpdated(controllerContext, newBindingContext);
			}
		}

		internal object BindComplexModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
			var model = bindingContext.Model;
			var modelType = bindingContext.ModelType;

			// if we're being asked to create an array, create a list instead, then coerce to an array after the list is created
			if (model == null && modelType.IsArray)
			{
				var elementType = modelType.GetElementType();
				var listType = typeof(List<>).MakeGenericType(elementType);
				var collection = CreateModel(controllerContext, bindingContext, listType);

				var arrayBindingContext = new ModelBindingContext
				{
					Model = collection,
					ModelName = bindingContext.ModelName,
					ModelState = bindingContext.ModelState,
					ModelType = listType,
					PropertyFilter = bindingContext.PropertyFilter,
					ValueProvider = bindingContext.ValueProvider
				};
				var list = (IList)UpdateCollection(controllerContext, arrayBindingContext, elementType);

				if (list == null)
				{
					return null;
				}

				var array = Array.CreateInstance(elementType, list.Count);
				list.CopyTo(array, 0);
				return array;
			}

			if (model == null)
			{
				model = CreateModel(controllerContext, bindingContext, modelType);
			}

			// special-case IDictionary<,> and ICollection<>
			var dictionaryType = ExtractGenericInterface(modelType, typeof(IDictionary<,>));
			if (dictionaryType != null)
			{
				var genericArguments = dictionaryType.GetGenericArguments();
				var keyType = genericArguments[0];
				var valueType = genericArguments[1];

				var dictionaryBindingContext = new ModelBindingContext
				{
					Model = model,
					ModelName = bindingContext.ModelName,
					ModelState = bindingContext.ModelState,
					ModelType = modelType,
					PropertyFilter = bindingContext.PropertyFilter,
					ValueProvider = bindingContext.ValueProvider
				};
				var dictionary = UpdateDictionary(controllerContext, dictionaryBindingContext, keyType, valueType);
				return dictionary;
			}

			var enumerableType = ExtractGenericInterface(modelType, typeof(IEnumerable<>));
			if (enumerableType != null)
			{
				var elementType = enumerableType.GetGenericArguments()[0];

				var collectionType = typeof(ICollection<>).MakeGenericType(elementType);
				if (collectionType.IsInstanceOfType(model))
				{
					var collectionBindingContext = new ModelBindingContext
					{
						Model = model,
						ModelName = bindingContext.ModelName,
						ModelState = bindingContext.ModelState,
						ModelType = modelType,
						PropertyFilter = bindingContext.PropertyFilter,
						ValueProvider = bindingContext.ValueProvider
					};
					var collection = UpdateCollection(controllerContext, collectionBindingContext, elementType);
					return collection;
				}
			}

			// otherwise, just update the properties on the complex type
			BindComplexElementalModel(controllerContext, bindingContext, model);
			return model;
		}

		public virtual object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
			if (bindingContext == null)
			{
				throw new ArgumentNullException("bindingContext");
			}

			var performedFallback = false;

			if (!String.IsNullOrEmpty(bindingContext.ModelName) &&
			    !DictionaryHelpers.DoesAnyKeyHavePrefix(bindingContext.ValueProvider, bindingContext.ModelName))
			{
				// We couldn't find any entry that began with the prefix. If this is the top-level element, fall back
				// to the empty prefix.
				if (bindingContext.FallbackToEmptyPrefix)
				{
					bindingContext = new ModelBindingContext
					{
						Model = bindingContext.Model,
						ModelState = bindingContext.ModelState,
						ModelType = bindingContext.ModelType,
						PropertyFilter = bindingContext.PropertyFilter,
						ValueProvider = bindingContext.ValueProvider
					};
					performedFallback = true;
				}
				else
				{
					return null;
				}
			}

			// Simple model = int, string, etc.; determined by calling TypeConverter.CanConvertFrom(typeof(string))
			// or by seeing if a value in the request exactly matches the name of the model we're binding.
			// Complex type = everything else.
			if (!performedFallback)
			{
				ValueProviderResult vpResult;
				bindingContext.ValueProvider.TryGetValue(bindingContext.ModelName, out vpResult);
				if (vpResult != null)
				{
					return BindSimpleModel(controllerContext, bindingContext, vpResult);
				}
			}
			if (TypeDescriptor.GetConverter(bindingContext.ModelType).CanConvertFrom(typeof(string)))
			{
				return null;
			}

			return BindComplexModel(controllerContext, bindingContext);
		}

		void BindProperties(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
			var properties = GetModelProperties(controllerContext, bindingContext);
			foreach (PropertyDescriptor property in properties)
			{
				BindProperty(controllerContext, bindingContext, property);
			}
		}

		protected virtual void BindProperty(ControllerContext controllerContext, ModelBindingContext bindingContext,
		                                    PropertyDescriptor propertyDescriptor)
		{
			// need to skip properties that aren't part of the request, else we might hit a StackOverflowException
			var fullPropertyKey = CreateSubPropertyName(bindingContext.ModelName, propertyDescriptor.Name);
			if (!DictionaryHelpers.DoesAnyKeyHavePrefix(bindingContext.ValueProvider, fullPropertyKey))
			{
				return;
			}

			// call into the property's model binder
			var propertyBinder = Binders.GetBinder(propertyDescriptor.PropertyType);
			var originalPropertyValue = propertyDescriptor.GetValue(bindingContext.Model);
			var innerBindingContext = new ModelBindingContext
			{
				Model = originalPropertyValue,
				ModelName = fullPropertyKey,
				ModelState = bindingContext.ModelState,
				ModelType = propertyDescriptor.PropertyType,
				ValueProvider = bindingContext.ValueProvider
			};
			var newPropertyValue = propertyBinder.BindModel(controllerContext, innerBindingContext);

			// validation
			if (OnPropertyValidating(controllerContext, bindingContext, propertyDescriptor, newPropertyValue))
			{
				SetProperty(controllerContext, bindingContext, propertyDescriptor, newPropertyValue);
				OnPropertyValidated(controllerContext, bindingContext, propertyDescriptor, newPropertyValue);
			}
		}

		internal object BindSimpleModel(ControllerContext controllerContext, ModelBindingContext bindingContext,
		                                ValueProviderResult valueProviderResult)
		{
			bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

			// if the value provider returns an instance of the requested data type, we can just short-circuit
			// the evaluation and return that instance
			if (bindingContext.ModelType.IsInstanceOfType(valueProviderResult.RawValue))
			{
				return valueProviderResult.RawValue;
			}

			// since a string is an IEnumerable<char>, we want it to skip the two checks immediately following
			if (bindingContext.ModelType != typeof(string))
			{
				// conversion results in 3 cases, as below
				if (bindingContext.ModelType.IsArray)
				{
					// case 1: user asked for an array
					// ValueProviderResult.ConvertTo() understands array types, so pass in the array type directly
					var modelArray = ConvertProviderResult(bindingContext.ModelState, bindingContext.ModelName, valueProviderResult,
					                                       bindingContext.ModelType);
					return modelArray;
				}

				var enumerableType = ExtractGenericInterface(bindingContext.ModelType, typeof(IEnumerable<>));
				if (enumerableType != null)
				{
					// case 2: user asked for a collection rather than an array
					// need to call ConvertTo() on the array type, then copy the array to the collection
					var modelCollection = CreateModel(controllerContext, bindingContext, bindingContext.ModelType);
					var elementType = enumerableType.GetGenericArguments()[0];
					var arrayType = elementType.MakeArrayType();
					var modelArray = ConvertProviderResult(bindingContext.ModelState, bindingContext.ModelName, valueProviderResult,
					                                       arrayType);

					var collectionType = typeof(ICollection<>).MakeGenericType(elementType);
					if (collectionType.IsInstanceOfType(modelCollection))
					{
						CollectionHelpers.ReplaceCollection(elementType, modelCollection, modelArray);
					}
					return modelCollection;
				}
			}

			// case 3: user asked for an individual element
			var model = ConvertProviderResult(bindingContext.ModelState, bindingContext.ModelName, valueProviderResult,
			                                  bindingContext.ModelType);
			return model;
		}

		static bool CanUpdateReadonlyTypedReference(Type type)
		{
			// value types aren't strictly immutable, but because they have copy-by-value semantics
			// we can't update a value type that is marked readonly
			if (type.IsValueType)
			{
				return false;
			}

			// arrays are mutable, but because we can't change their length we shouldn't try
			// to update an array that is referenced readonly
			if (type.IsArray)
			{
				return false;
			}

			// special-case known common immutable types
			if (type == typeof(string))
			{
				return false;
			}

			return true;
		}

		[SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo",
			MessageId = "System.Web.Mvc.ValueProviderResult.ConvertTo(System.Type)",
			Justification = "The target object should make the correct culture determination, not this method.")]
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
			Justification = "We're recording this exception so that we can act on it later.")]
		static object ConvertProviderResult(ModelStateDictionary modelState, string modelStateKey,
		                                    ValueProviderResult valueProviderResult, Type destinationType)
		{
			try
			{
				var convertedValue = valueProviderResult.ConvertTo(destinationType);
				return convertedValue;
			}
			catch (Exception ex)
			{
				modelState.AddModelError(modelStateKey, ex);
				return null;
			}
		}

		protected virtual object CreateModel(ControllerContext controllerContext, ModelBindingContext bindingContext,
		                                     Type modelType)
		{
			var typeToCreate = modelType;

			// we can understand some collection interfaces, e.g. IList<>, IDictionary<,>
			if (modelType.IsGenericType)
			{
				var genericTypeDefinition = modelType.GetGenericTypeDefinition();
				if (genericTypeDefinition == typeof(IDictionary<,>))
				{
					typeToCreate = typeof(Dictionary<,>).MakeGenericType(modelType.GetGenericArguments());
				}
				else if (genericTypeDefinition == typeof(IEnumerable<>) || genericTypeDefinition == typeof(ICollection<>) ||
				         genericTypeDefinition == typeof(IList<>))
				{
					typeToCreate = typeof(List<>).MakeGenericType(modelType.GetGenericArguments());
				}
			}

			// fallback to the type's default constructor
			return Activator.CreateInstance(typeToCreate);
		}

		protected static string CreateSubIndexName(string prefix, int index)
		{
			return String.Format(CultureInfo.InvariantCulture, "{0}[{1}]", prefix, index);
		}

		protected static string CreateSubPropertyName(string prefix, string propertyName)
		{
			return (!String.IsNullOrEmpty(prefix)) ? prefix + "." + propertyName : propertyName;
		}

		static Type ExtractGenericInterface(Type queryType, Type interfaceType)
		{
			Func<Type, bool> matchesInterface = t => t.IsGenericType && t.GetGenericTypeDefinition() == interfaceType;
			return (matchesInterface(queryType)) ? queryType : queryType.GetInterfaces().FirstOrDefault(matchesInterface);
		}

		protected virtual PropertyDescriptorCollection GetModelProperties(ControllerContext controllerContext,
		                                                                  ModelBindingContext bindingContext)
		{
			var allProperties = TypeDescriptor.GetProperties(bindingContext.ModelType);
			var propertyFilter = bindingContext.PropertyFilter;

			var filteredProperties = from PropertyDescriptor property in allProperties
			                         where ShouldUpdateProperty(property, propertyFilter)
			                         select property;

			return new PropertyDescriptorCollection(filteredProperties.ToArray());
		}

		static string GetValueRequiredResource(ControllerContext controllerContext)
		{
			string resourceValue = null;
			if (!String.IsNullOrEmpty(ResourceClassKey) && (controllerContext != null) && (controllerContext.HttpContext != null))
			{
				// If the user specified a ResourceClassKey try to load the resource they specified.
				// If the class key is invalid, an exception will be thrown.
				// If the class key is valid but the resource is not found, it returns null, in which
				// case it will fall back to the MVC default error message.
				resourceValue =
					controllerContext.HttpContext.GetGlobalResourceObject(ResourceClassKey, "PropertyValueRequired",
					                                                      CultureInfo.CurrentUICulture) as string;
			}
			return resourceValue ?? MvcResources.DefaultModelBinder_ValueRequired;
		}

		protected virtual void OnModelUpdated(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
			var errorProvider = bindingContext.Model as IDataErrorInfo;
			if (errorProvider != null)
			{
				var errorText = errorProvider.Error;
				if (!String.IsNullOrEmpty(errorText))
				{
					bindingContext.ModelState.AddModelError(bindingContext.ModelName, errorText);
				}
			}
		}

		protected virtual bool OnModelUpdating(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
			// default implementation does nothing

			return true;
		}

		protected virtual void OnPropertyValidated(ControllerContext controllerContext, ModelBindingContext bindingContext,
		                                           PropertyDescriptor propertyDescriptor, object value)
		{
			var errorProvider = bindingContext.Model as IDataErrorInfo;
			if (errorProvider != null)
			{
				var errorText = errorProvider[propertyDescriptor.Name];
				if (!String.IsNullOrEmpty(errorText))
				{
					var modelStateKey = CreateSubPropertyName(bindingContext.ModelName, propertyDescriptor.Name);
					bindingContext.ModelState.AddModelError(modelStateKey, errorText);
				}
			}
		}

		protected virtual bool OnPropertyValidating(ControllerContext controllerContext, ModelBindingContext bindingContext,
		                                            PropertyDescriptor propertyDescriptor, object value)
		{
			// default implementation just checks to make sure that required text entry fields aren't left blank

			var modelStateKey = CreateSubPropertyName(bindingContext.ModelName, propertyDescriptor.Name);
			return VerifyValueUsability(controllerContext, bindingContext.ModelState, modelStateKey,
			                            propertyDescriptor.PropertyType, value);
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
			Justification = "We're recording this exception so that we can act on it later.")]
		protected virtual void SetProperty(ControllerContext controllerContext, ModelBindingContext bindingContext,
		                                   PropertyDescriptor propertyDescriptor, object value)
		{
			if (propertyDescriptor.IsReadOnly)
			{
				return;
			}

			try
			{
				propertyDescriptor.SetValue(bindingContext.Model, value);
			}
			catch (Exception ex)
			{
				var modelStateKey = CreateSubPropertyName(bindingContext.ModelName, propertyDescriptor.Name);
				bindingContext.ModelState.AddModelError(modelStateKey, ex);
			}
		}

		static bool ShouldUpdateProperty(PropertyDescriptor property, Predicate<string> propertyFilter)
		{
			if (property.IsReadOnly && !CanUpdateReadonlyTypedReference(property.PropertyType))
			{
				return false;
			}

			// if this property is rejected by the filter, move on
			if (!propertyFilter(property.Name))
			{
				return false;
			}

			// otherwise, allow
			return true;
		}

		internal object UpdateCollection(ControllerContext controllerContext, ModelBindingContext bindingContext,
		                                 Type elementType)
		{
			var elementBinder = Binders.GetBinder(elementType);

			// build up a list of items from the request
			var modelList = new List<object>();
			for (var currentIndex = 0;; currentIndex++)
			{
				var subIndexKey = CreateSubIndexName(bindingContext.ModelName, currentIndex);
				if (!DictionaryHelpers.DoesAnyKeyHavePrefix(bindingContext.ValueProvider, subIndexKey))
				{
					// we ran out of elements to pull
					break;
				}

				var innerContext = new ModelBindingContext
				{
					ModelName = subIndexKey,
					ModelState = bindingContext.ModelState,
					ModelType = elementType,
					PropertyFilter = bindingContext.PropertyFilter,
					ValueProvider = bindingContext.ValueProvider
				};
				var thisElement = elementBinder.BindModel(controllerContext, innerContext);

				// we need to merge model errors up
				VerifyValueUsability(controllerContext, bindingContext.ModelState, subIndexKey, elementType, thisElement);
				modelList.Add(thisElement);
			}

			// if there weren't any elements at all in the request, just return
			if (modelList.Count == 0)
			{
				return null;
			}

			// replace the original collection
			var collection = bindingContext.Model;
			CollectionHelpers.ReplaceCollection(elementType, collection, modelList);
			return collection;
		}

		internal object UpdateDictionary(ControllerContext controllerContext, ModelBindingContext bindingContext, Type keyType,
		                                 Type valueType)
		{
			var keyBinder = Binders.GetBinder(keyType);
			var valueBinder = Binders.GetBinder(valueType);

			// build up a list of items from the request
			var modelList = new List<KeyValuePair<object, object>>();
			for (var currentIndex = 0;; currentIndex++)
			{
				var subIndexKey = CreateSubIndexName(bindingContext.ModelName, currentIndex);
				var keyFieldKey = CreateSubPropertyName(subIndexKey, "key");
				var valueFieldKey = CreateSubPropertyName(subIndexKey, "value");

				if (
					!(DictionaryHelpers.DoesAnyKeyHavePrefix(bindingContext.ValueProvider, keyFieldKey) &&
					  DictionaryHelpers.DoesAnyKeyHavePrefix(bindingContext.ValueProvider, valueFieldKey)))
				{
					// we ran out of elements to pull
					break;
				}

				// bind the key
				var keyBindingContext = new ModelBindingContext
				{
					ModelName = keyFieldKey,
					ModelState = bindingContext.ModelState,
					ModelType = keyType,
					ValueProvider = bindingContext.ValueProvider
				};
				var thisKey = keyBinder.BindModel(controllerContext, keyBindingContext);

				// we need to merge model errors up
				VerifyValueUsability(controllerContext, bindingContext.ModelState, keyFieldKey, keyType, thisKey);
				if (!keyType.IsInstanceOfType(thisKey))
				{
					// we can't add an invalid key, so just move on
					continue;
				}

				// bind the value
				var valueBindingContext = new ModelBindingContext
				{
					ModelName = valueFieldKey,
					ModelState = bindingContext.ModelState,
					ModelType = valueType,
					PropertyFilter = bindingContext.PropertyFilter,
					ValueProvider = bindingContext.ValueProvider
				};
				var thisValue = valueBinder.BindModel(controllerContext, valueBindingContext);

				// we need to merge model errors up
				VerifyValueUsability(controllerContext, bindingContext.ModelState, valueFieldKey, valueType, thisValue);
				var kvp = new KeyValuePair<object, object>(thisKey, thisValue);
				modelList.Add(kvp);
			}

			// if there weren't any elements at all in the request, just return
			if (modelList.Count == 0)
			{
				return null;
			}

			// replace the original collection
			var dictionary = bindingContext.Model;
			CollectionHelpers.ReplaceDictionary(keyType, valueType, dictionary, modelList);
			return dictionary;
		}

		static bool VerifyValueUsability(ControllerContext controllerContext, ModelStateDictionary modelState,
		                                 string modelStateKey, Type elementType, object value)
		{
			if (value == null && !TypeHelpers.TypeAllowsNullValue(elementType))
			{
				if (modelState.IsValidField(modelStateKey))
				{
					// a required entry field was left blank
					var message = GetValueRequiredResource(controllerContext);
					modelState.AddModelError(modelStateKey, message);
				}
				// we don't care about "you must enter a value" messages if there was an error
				return false;
			}

			return true;
		}

		// This helper type is used because we're working with strongly-typed collections, but we don't know the Ts
		// ahead of time. By using the generic methods below, we can consolidate the collection-specific code in a
		// single helper type rather than having reflection-based calls spread throughout the DefaultModelBinder type.
		// There is a single point of entry to each of the methods below, so they're fairly simple to maintain.

		static class CollectionHelpers
		{
			static readonly MethodInfo _replaceCollectionMethod = typeof(CollectionHelpers).GetMethod("ReplaceCollectionImpl",
			                                                                                          BindingFlags.Static |
			                                                                                          BindingFlags.NonPublic);

			static readonly MethodInfo _replaceDictionaryMethod = typeof(CollectionHelpers).GetMethod("ReplaceDictionaryImpl",
			                                                                                          BindingFlags.Static |
			                                                                                          BindingFlags.NonPublic);

			public static void ReplaceCollection(Type collectionType, object collection, object newContents)
			{
				var targetMethod = _replaceCollectionMethod.MakeGenericMethod(collectionType);
				targetMethod.Invoke(null, new[] {collection, newContents});
			}

			static void ReplaceCollectionImpl<T>(ICollection<T> collection, IEnumerable newContents)
			{
				collection.Clear();
				if (newContents != null)
				{
					foreach (var item in newContents)
					{
						// if the item was not a T, some conversion failed. the error message will be propagated,
						// but in the meanwhile we need to make a placeholder element in the array.
						var castItem = (item is T) ? (T)item : default(T);
						collection.Add(castItem);
					}
				}
			}

			public static void ReplaceDictionary(Type keyType, Type valueType, object dictionary, object newContents)
			{
				var targetMethod = _replaceDictionaryMethod.MakeGenericMethod(keyType, valueType);
				targetMethod.Invoke(null, new[] {dictionary, newContents});
			}

			static void ReplaceDictionaryImpl<TKey, TValue>(IDictionary<TKey, TValue> dictionary,
			                                                IEnumerable<KeyValuePair<object, object>> newContents)
			{
				dictionary.Clear();
				foreach (var item in newContents)
				{
					// if the item was not a T, some conversion failed. the error message will be propagated,
					// but in the meanwhile we need to make a placeholder element in the dictionary.
					var castKey = (TKey)item.Key; // this cast shouldn't fail
					var castValue = (item.Value is TValue) ? (TValue)item.Value : default(TValue);
					dictionary[castKey] = castValue;
				}
			}
		}
	}
}