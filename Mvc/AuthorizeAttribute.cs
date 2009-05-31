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

using System.Linq;

namespace System.Web.Mvc
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
	public class AuthorizeAttribute : FilterAttribute, IAuthorizationFilter
	{
		string _roles;
		string[] _rolesSplit = new string[0];
		string _users;
		string[] _usersSplit = new string[0];

		public string Roles
		{
			get { return _roles ?? String.Empty; }
			set
			{
				_roles = value;
				_rolesSplit = SplitString(value);
			}
		}

		public string Users
		{
			get { return _users ?? String.Empty; }
			set
			{
				_users = value;
				_usersSplit = SplitString(value);
			}
		}

		// This method must be thread-safe since it is called by the thread-safe OnCacheAuthorization() method.
		protected virtual bool AuthorizeCore(HttpContextBase httpContext)
		{
			if (httpContext == null)
			{
				throw new ArgumentNullException("httpContext");
			}

			var user = httpContext.User;
			if (!user.Identity.IsAuthenticated)
			{
				return false;
			}

			if (_usersSplit.Length > 0 && !_usersSplit.Contains(user.Identity.Name, StringComparer.OrdinalIgnoreCase))
			{
				return false;
			}

			if (_rolesSplit.Length > 0 && !_rolesSplit.Any(user.IsInRole))
			{
				return false;
			}

			return true;
		}

		void CacheValidateHandler(HttpContext context, object data, ref HttpValidationStatus validationStatus)
		{
			validationStatus = OnCacheAuthorization(new HttpContextWrapper(context));
		}

		public virtual void OnAuthorization(AuthorizationContext filterContext)
		{
			if (filterContext == null)
			{
				throw new ArgumentNullException("filterContext");
			}

			if (AuthorizeCore(filterContext.HttpContext))
			{
				// ** IMPORTANT **
				// Since we're performing authorization at the action level, the authorization code runs
				// after the output caching module. In the worst case this could allow an authorized user
				// to cause the page to be cached, then an unauthorized user would later be served the
				// cached page. We work around this by telling proxies not to cache the sensitive page,
				// then we hook our custom authorization code into the caching mechanism so that we have
				// the final say on whether a page should be served from the cache.

				var cachePolicy = filterContext.HttpContext.Response.Cache;
				cachePolicy.SetProxyMaxAge(new TimeSpan(0));
				cachePolicy.AddValidationCallback(CacheValidateHandler, null /* data */);
			}
			else
			{
				// auth failed, redirect to login page
				filterContext.Result = new HttpUnauthorizedResult();
			}
		}

		// This method must be thread-safe since it is called by the caching module.
		protected virtual HttpValidationStatus OnCacheAuthorization(HttpContextBase httpContext)
		{
			if (httpContext == null)
			{
				throw new ArgumentNullException("httpContext");
			}

			var isAuthorized = AuthorizeCore(httpContext);
			return (isAuthorized) ? HttpValidationStatus.Valid : HttpValidationStatus.IgnoreThisRequest;
		}

		internal static string[] SplitString(string original)
		{
			if (String.IsNullOrEmpty(original))
			{
				return new string[0];
			}

			var split = from piece in original.Split(',')
			            let trimmed = piece.Trim()
			            where !String.IsNullOrEmpty(trimmed)
			            select trimmed;
			return split.ToArray();
		}
	}
}