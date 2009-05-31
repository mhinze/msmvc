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

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	[FormCollectionBinder]
	public class FormCollection : NameValueCollection
	{
		public FormCollection() {}

		public FormCollection(NameValueCollection collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}

			Add(collection);
		}

		public IDictionary<string, ValueProviderResult> ToValueProvider()
		{
			var currentCulture = CultureInfo.CurrentCulture;

			var dict = new Dictionary<string, ValueProviderResult>(StringComparer.OrdinalIgnoreCase);
			var keys = AllKeys;
			foreach (var key in keys)
			{
				var rawValue = GetValues(key);
				var attemptedValue = this[key];
				var vpResult = new ValueProviderResult(rawValue, attemptedValue, currentCulture);
				dict[key] = vpResult;
			}

			return dict;
		}

		public virtual ValueProviderResult GetValue(string name)
		{
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "name");
			}

			var rawValue = GetValues(name);
			if (rawValue == null)
			{
				return null;
			}

			var attemptedValue = this[name];
			return new ValueProviderResult(rawValue, attemptedValue, CultureInfo.CurrentCulture);
		}

		sealed class FormCollectionBinderAttribute : CustomModelBinderAttribute
		{
			// since the FormCollectionModelBinder.BindModel() method is thread-safe, we only need to keep
			// a single instance of the binder around
			static readonly FormCollectionModelBinder _binder = new FormCollectionModelBinder();

			public override IModelBinder GetBinder()
			{
				return _binder;
			}

			// this class is used for generating a FormCollection object
			sealed class FormCollectionModelBinder : IModelBinder
			{
				public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
				{
					if (controllerContext == null)
					{
						throw new ArgumentNullException("controllerContext");
					}

					return new FormCollection(controllerContext.HttpContext.Request.Form);
				}
			}
		}
	}
}