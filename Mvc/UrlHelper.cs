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
using System.Web.Mvc.Resources;
using System.Web.Routing;

namespace System.Web.Mvc
{
	public class UrlHelper
	{
		public UrlHelper(RequestContext requestContext)
			: this(requestContext, RouteTable.Routes) {}

		public UrlHelper(RequestContext requestContext, RouteCollection routeCollection)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			if (routeCollection == null)
			{
				throw new ArgumentNullException("routeCollection");
			}
			RequestContext = requestContext;
			RouteCollection = routeCollection;
		}

		public RequestContext RequestContext { get; private set; }

		public RouteCollection RouteCollection { get; private set; }

		public string Action(string actionName)
		{
			return GenerateUrl(null /* routeName */, actionName, null, null /* routeValues */);
		}

		public string Action(string actionName, object routeValues)
		{
			return GenerateUrl(null /* routeName */, actionName, null /* controllerName */, new RouteValueDictionary(routeValues));
		}

		public string Action(string actionName, RouteValueDictionary routeValues)
		{
			return GenerateUrl(null /* routeName */, actionName, null /* controllerName */, routeValues);
		}

		public string Action(string actionName, string controllerName)
		{
			return GenerateUrl(null /* routeName */, actionName, controllerName, null /* routeValues */);
		}

		public string Action(string actionName, string controllerName, object routeValues)
		{
			return GenerateUrl(null /* routeName */, actionName, controllerName, new RouteValueDictionary(routeValues));
		}

		public string Action(string actionName, string controllerName, RouteValueDictionary routeValues)
		{
			return GenerateUrl(null /* routeName */, actionName, controllerName, routeValues);
		}

		public string Action(string actionName, string controllerName, object routeValues, string protocol)
		{
			return GenerateUrl(null /* routeName */, actionName, controllerName, protocol, null /* hostName */, null
			                   /* fragment */, new RouteValueDictionary(routeValues), RouteCollection, RequestContext, true
				/* includeImplicitMvcValues */);
		}

		public string Action(string actionName, string controllerName, RouteValueDictionary routeValues, string protocol,
		                     string hostName)
		{
			return GenerateUrl(null /* routeName */, actionName, controllerName, protocol, hostName, null /* fragment */,
			                   routeValues, RouteCollection, RequestContext, true /* includeImplicitMvcValues */);
		}

		public string Content(string contentPath)
		{
			return Content(contentPath, RequestContext.HttpContext);
		}

		internal static string Content(string contentPath, HttpContextBase httpContext)
		{
			if (String.IsNullOrEmpty(contentPath))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "contentPath");
			}

			if (contentPath[0] == '~')
			{
				return PathHelpers.GenerateClientUrl(httpContext, contentPath);
			}
			else
			{
				return contentPath;
			}
		}

		//REVIEW: Should we have an overload that takes Uri?
		public string Encode(string url)
		{
			return HttpUtility.UrlEncode(url);
		}

		string GenerateUrl(string routeName, string actionName, string controllerName, RouteValueDictionary routeValues)
		{
			return GenerateUrl(routeName, actionName, controllerName, routeValues, RouteCollection, RequestContext, true
				/* includeImplicitMvcValues */);
		}

		internal static string GenerateUrl(string routeName, string actionName, string controllerName, string protocol,
		                                   string hostName, string fragment, RouteValueDictionary routeValues,
		                                   RouteCollection routeCollection, RequestContext requestContext,
		                                   bool includeImplicitMvcValues)
		{
			var url = GenerateUrl(routeName, actionName, controllerName, routeValues, routeCollection, requestContext,
			                      includeImplicitMvcValues);

			if (url != null)
			{
				if (!String.IsNullOrEmpty(fragment))
				{
					url = url + "#" + fragment;
				}

				if (!String.IsNullOrEmpty(protocol) || !String.IsNullOrEmpty(hostName))
				{
					var requestUrl = requestContext.HttpContext.Request.Url;
					protocol = (!String.IsNullOrEmpty(protocol)) ? protocol : Uri.UriSchemeHttp;
					hostName = (!String.IsNullOrEmpty(hostName)) ? hostName : requestUrl.Host;

					var port = String.Empty;
					var requestProtocol = requestUrl.Scheme;

					if (String.Equals(protocol, requestProtocol, StringComparison.OrdinalIgnoreCase))
					{
						port = requestUrl.IsDefaultPort
						       	? String.Empty
						       	: (":" + Convert.ToString(requestUrl.Port, CultureInfo.InvariantCulture));
					}

					url = protocol + Uri.SchemeDelimiter + hostName + port + url;
				}
			}

			return url;
		}

		internal static string GenerateUrl(string routeName, string actionName, string controllerName,
		                                   RouteValueDictionary routeValues, RouteCollection routeCollection,
		                                   RequestContext requestContext, bool includeImplicitMvcValues)
		{
			var mergedRouteValues = RouteValuesHelpers.MergeRouteValues(actionName, controllerName,
			                                                            requestContext.RouteData.Values, routeValues,
			                                                            includeImplicitMvcValues);

			var vpd = routeCollection.GetVirtualPath(requestContext, routeName, mergedRouteValues);
			if (vpd == null)
			{
				return null;
			}

			var modifiedUrl = PathHelpers.GenerateClientUrl(requestContext.HttpContext, vpd.VirtualPath);
			return modifiedUrl;
		}

		public string RouteUrl(object routeValues)
		{
			return RouteUrl(null /* routeName */, routeValues);
		}

		public string RouteUrl(RouteValueDictionary routeValues)
		{
			return RouteUrl(null /* routeName */, routeValues);
		}

		public string RouteUrl(string routeName)
		{
			return RouteUrl(routeName, (object)null /* routeValues */);
		}

		public string RouteUrl(string routeName, object routeValues)
		{
			return RouteUrl(routeName, routeValues, null /* protocol */);
		}

		public string RouteUrl(string routeName, RouteValueDictionary routeValues)
		{
			return RouteUrl(routeName, routeValues, null /* protocol */, null /* hostName */);
		}

		public string RouteUrl(string routeName, object routeValues, string protocol)
		{
			return GenerateUrl(routeName, null /* actionName */, null /* controllerName */, protocol, null /* hostName */, null
			                   /* fragment */, new RouteValueDictionary(routeValues), RouteCollection, RequestContext, false
				/* includeImplicitMvcValues */);
		}

		public string RouteUrl(string routeName, RouteValueDictionary routeValues, string protocol, string hostName)
		{
			return GenerateUrl(routeName, null /* actionName */, null /* controllerName */, protocol, hostName, null
			                   /* fragment */, routeValues, RouteCollection, RequestContext, false /* includeImplicitMvcValues */);
		}
	}
}