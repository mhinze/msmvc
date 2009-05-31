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

using System.Reflection;

namespace System.Web.Mvc
{
	public static class ModelBinders
	{
		static readonly ModelBinderDictionary _binders = CreateDefaultBinderDictionary();

		public static ModelBinderDictionary Binders
		{
			get { return _binders; }
		}

		internal static IModelBinder GetBinderFromAttributes(ICustomAttributeProvider element,
		                                                     Func<string> errorMessageAccessor)
		{
			// this method is used to get a custom binder based on the attributes of the element passed to it.
			// it will return null if a binder cannot be detected based on the attributes alone.

			var attrs =
				(CustomModelBinderAttribute[])element.GetCustomAttributes(typeof(CustomModelBinderAttribute), true /* inherit */);
			if (attrs == null)
			{
				return null;
			}

			switch (attrs.Length)
			{
				case 0:
					return null;

				case 1:
					var binder = attrs[0].GetBinder();
					return binder;

				default:
					var errorMessage = errorMessageAccessor();
					throw new InvalidOperationException(errorMessage);
			}
		}

		static ModelBinderDictionary CreateDefaultBinderDictionary()
		{
			// We can't add a binder to the HttpPostedFileBase type as an attribute, so we'll just
			// prepopulate the dictionary as a convenience to users.

			var binders = new ModelBinderDictionary
			{
				{typeof(HttpPostedFileBase), new HttpPostedFileBaseModelBinder()}
			};
			return binders;
		}
	}
}