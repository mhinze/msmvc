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

using System.Web.Routing;

namespace System.Web.Mvc
{
	public static class RouteCollectionExtensions
	{
		public static void IgnoreRoute(this RouteCollection routes, string url)
		{
			IgnoreRoute(routes, url, null /* constraints */);
		}

		public static void IgnoreRoute(this RouteCollection routes, string url, object constraints)
		{
			if (routes == null)
			{
				throw new ArgumentNullException("routes");
			}
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}

			var route = new IgnoreRouteInternal(url)
			{
				Constraints = new RouteValueDictionary(constraints)
			};

			routes.Add(route);
		}

		public static Route MapRoute(this RouteCollection routes, string name, string url)
		{
			return MapRoute(routes, name, url, null /* defaults */, (object)null /* constraints */);
		}

		public static Route MapRoute(this RouteCollection routes, string name, string url, object defaults)
		{
			return MapRoute(routes, name, url, defaults, (object)null /* constraints */);
		}

		public static Route MapRoute(this RouteCollection routes, string name, string url, object defaults, object constraints)
		{
			return MapRoute(routes, name, url, defaults, constraints, null /* namespaces */);
		}

		public static Route MapRoute(this RouteCollection routes, string name, string url, string[] namespaces)
		{
			return MapRoute(routes, name, url, null /* defaults */, null /* constraints */, namespaces);
		}

		public static Route MapRoute(this RouteCollection routes, string name, string url, object defaults,
		                             string[] namespaces)
		{
			return MapRoute(routes, name, url, defaults, null /* constraints */, namespaces);
		}

		public static Route MapRoute(this RouteCollection routes, string name, string url, object defaults, object constraints,
		                             string[] namespaces)
		{
			if (routes == null)
			{
				throw new ArgumentNullException("routes");
			}
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}

			var route = new Route(url, new MvcRouteHandler())
			{
				Defaults = new RouteValueDictionary(defaults),
				Constraints = new RouteValueDictionary(constraints)
			};

			if ((namespaces != null) && (namespaces.Length > 0))
			{
				route.DataTokens = new RouteValueDictionary();
				route.DataTokens["Namespaces"] = namespaces;
			}

			routes.Add(name, route);

			return route;
		}

		sealed class IgnoreRouteInternal : Route
		{
			public IgnoreRouteInternal(string url)
				: base(url, new StopRoutingHandler()) {}

			public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary routeValues)
			{
				// Never match during route generation. This avoids the scenario where an IgnoreRoute with
				// fairly relaxed constraints ends up eagerly matching all generated URLs.
				return null;
			}
		}
	}
}