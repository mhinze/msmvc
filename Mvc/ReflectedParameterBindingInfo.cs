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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	internal class ReflectedParameterBindingInfo : ParameterBindingInfo
	{
		ICollection<string> _exclude = new string[0];
		ICollection<string> _include = new string[0];
		readonly ParameterInfo _parameterInfo;
		string _prefix;

		public ReflectedParameterBindingInfo(ParameterInfo parameterInfo)
		{
			_parameterInfo = parameterInfo;
			ReadSettingsFromBindAttribute();
		}

		public override IModelBinder Binder
		{
			get
			{
				var binder = ModelBinders.GetBinderFromAttributes(_parameterInfo,
				                                                  () =>
				                                                  String.Format(CultureInfo.CurrentUICulture,
				                                                                MvcResources.
				                                                                	ReflectedParameterBindingInfo_MultipleConverterAttributes,
				                                                                _parameterInfo.Name, _parameterInfo.Member));

				return binder;
			}
		}

		public override ICollection<string> Exclude
		{
			get { return _exclude; }
		}

		public override ICollection<string> Include
		{
			get { return _include; }
		}

		public override string Prefix
		{
			get { return _prefix; }
		}

		void ReadSettingsFromBindAttribute()
		{
			var attr = (BindAttribute)Attribute.GetCustomAttribute(_parameterInfo, typeof(BindAttribute));
			if (attr == null)
			{
				return;
			}

			_exclude = new ReadOnlyCollection<string>(AuthorizeAttribute.SplitString(attr.Exclude));
			_include = new ReadOnlyCollection<string>(AuthorizeAttribute.SplitString(attr.Include));
			_prefix = attr.Prefix;
		}
	}
}