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

using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class ValidateAntiForgeryTokenAttribute : FilterAttribute, IAuthorizationFilter
	{
		string _salt;
		AntiForgeryDataSerializer _serializer;

		public string Salt
		{
			get { return _salt ?? String.Empty; }
			set { _salt = value; }
		}

		internal AntiForgeryDataSerializer Serializer
		{
			get
			{
				if (_serializer == null)
				{
					_serializer = new AntiForgeryDataSerializer();
				}
				return _serializer;
			}
			set { _serializer = value; }
		}

		bool ValidateFormToken(AntiForgeryData token)
		{
			return (String.Equals(Salt, token.Salt, StringComparison.Ordinal));
		}

		static HttpAntiForgeryException CreateValidationException()
		{
			return new HttpAntiForgeryException(MvcResources.AntiForgeryToken_ValidationFailed);
		}

		public void OnAuthorization(AuthorizationContext filterContext)
		{
			if (filterContext == null)
			{
				throw new ArgumentNullException("filterContext");
			}

			var fieldName = AntiForgeryData.GetAntiForgeryTokenName(null);
			var cookieName = AntiForgeryData.GetAntiForgeryTokenName(filterContext.HttpContext.Request.ApplicationPath);

			var cookie = filterContext.HttpContext.Request.Cookies[cookieName];
			if (cookie == null || String.IsNullOrEmpty(cookie.Value))
			{
				// error: cookie token is missing
				throw CreateValidationException();
			}
			var cookieToken = Serializer.Deserialize(cookie.Value);

			var formValue = filterContext.HttpContext.Request.Form[fieldName];
			if (String.IsNullOrEmpty(formValue))
			{
				// error: form token is missing
				throw CreateValidationException();
			}
			var formToken = Serializer.Deserialize(formValue);

			if (!String.Equals(cookieToken.Value, formToken.Value, StringComparison.Ordinal))
			{
				// error: form token does not match cookie token
				throw CreateValidationException();
			}

			if (!ValidateFormToken(formToken))
			{
				// error: custom validation failed
				throw CreateValidationException();
			}
		}
	}
}