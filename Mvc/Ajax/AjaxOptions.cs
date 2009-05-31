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

using System.Globalization;
using System.Text;
using System.Web.Mvc.Resources;

namespace System.Web.Mvc.Ajax
{
	public class AjaxOptions
	{
		string _confirm;
		string _httpMethod;
		InsertionMode _insertionMode = InsertionMode.Replace;
		string _loadingElementId;
		string _onBegin;
		string _onComplete;
		string _onFailure;
		string _onSuccess;
		string _updateTargetId;
		string _url;

		public string Confirm
		{
			get { return _confirm ?? String.Empty; }
			set { _confirm = value; }
		}

		public string HttpMethod
		{
			get { return _httpMethod ?? String.Empty; }
			set { _httpMethod = value; }
		}

		public InsertionMode InsertionMode
		{
			get { return _insertionMode; }
			set
			{
				switch (value)
				{
					case InsertionMode.Replace:
					case InsertionMode.InsertAfter:
					case InsertionMode.InsertBefore:
						_insertionMode = value;
						return;

					default:
						throw new ArgumentOutOfRangeException("value", String.Format(CultureInfo.InvariantCulture,
						                                                             MvcResources.Common_InvalidEnumValue, value,
						                                                             typeof(InsertionMode).FullName));
				}
			}
		}

		public string LoadingElementId
		{
			get { return _loadingElementId ?? String.Empty; }
			set { _loadingElementId = value; }
		}

		public string OnBegin
		{
			get { return _onBegin ?? String.Empty; }
			set { _onBegin = value; }
		}

		public string OnComplete
		{
			get { return _onComplete ?? String.Empty; }
			set { _onComplete = value; }
		}

		public string OnFailure
		{
			get { return _onFailure ?? String.Empty; }
			set { _onFailure = value; }
		}

		public string OnSuccess
		{
			get { return _onSuccess ?? String.Empty; }
			set { _onSuccess = value; }
		}

		public string UpdateTargetId
		{
			get { return _updateTargetId ?? String.Empty; }
			set { _updateTargetId = value; }
		}

		public string Url
		{
			get { return _url ?? String.Empty; }
			set { _url = value; }
		}

		internal string ToJavascriptString()
		{
			// creates a string of the form { key1: value1, key2 : value2, ... }
			var optionsBuilder = new StringBuilder("{");
			optionsBuilder.Append(String.Format(CultureInfo.InvariantCulture, " insertionMode: {0},",
			                                    AjaxExtensions.InsertionModeToString(InsertionMode)));
			optionsBuilder.Append(PropertyStringIfSpecified("confirm", Confirm));
			optionsBuilder.Append(PropertyStringIfSpecified("httpMethod", HttpMethod));
			optionsBuilder.Append(PropertyStringIfSpecified("loadingElementId", LoadingElementId));
			optionsBuilder.Append(PropertyStringIfSpecified("updateTargetId", UpdateTargetId));
			optionsBuilder.Append(PropertyStringIfSpecified("url", Url));
			optionsBuilder.Append(EventStringIfSpecified("onBegin", OnBegin));
			optionsBuilder.Append(EventStringIfSpecified("onComplete", OnComplete));
			optionsBuilder.Append(EventStringIfSpecified("onFailure", OnFailure));
			optionsBuilder.Append(EventStringIfSpecified("onSuccess", OnSuccess));
			optionsBuilder.Length--;
			optionsBuilder.Append(" }");
			return optionsBuilder.ToString();
		}

		static string EventStringIfSpecified(string propertyName, string handler)
		{
			if (!String.IsNullOrEmpty(handler))
			{
				return String.Format(CultureInfo.InvariantCulture, " {0}: Function.createDelegate(this, {1}),", propertyName,
				                     handler);
			}
			return String.Empty;
		}

		static string PropertyStringIfSpecified(string propertyName, string propertyValue)
		{
			if (!String.IsNullOrEmpty(propertyValue))
			{
				var escapedPropertyValue = propertyValue.Replace("'", @"\'");
				return String.Format(CultureInfo.InvariantCulture, " {0}: '{1}',", propertyName, escapedPropertyValue);
			}
			return String.Empty;
		}
	}
}