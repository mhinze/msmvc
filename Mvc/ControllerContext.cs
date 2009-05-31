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
	// Though many of the properties on ControllerContext and its subclassed types are virtual, there are still sealed
	// properties (like ControllerContext.RequestContext, ActionExecutingContext.Result, etc.). If these properties
	// were virtual, a mocking framework might override them with incorrect behavior (property getters would return
	// null, property setters would be no-ops). By sealing these properties, we are forcing them to have the default
	// "get or store a value" semantics that they were intended to have.

	public class ControllerContext
	{
		HttpContextBase _httpContext;
		RequestContext _requestContext;
		RouteData _routeData;

		// parameterless constructor used for mocking
		public ControllerContext() {}

		// copy constructor - allows for subclassed types to take an existing ControllerContext as a parameter
		// and we'll automatically set the appropriate properties
		protected ControllerContext(ControllerContext controllerContext)
		{
			if (controllerContext == null)
			{
				throw new ArgumentNullException("controllerContext");
			}

			Controller = controllerContext.Controller;
			RequestContext = controllerContext.RequestContext;
		}

		public ControllerContext(HttpContextBase httpContext, RouteData routeData, ControllerBase controller)
			: this(new RequestContext(httpContext, routeData), controller) {}

		public ControllerContext(RequestContext requestContext, ControllerBase controller)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			if (controller == null)
			{
				throw new ArgumentNullException("controller");
			}

			RequestContext = requestContext;
			Controller = controller;
		}

		public virtual ControllerBase Controller { get; set; }

		public virtual HttpContextBase HttpContext
		{
			get
			{
				if (_httpContext == null)
				{
					_httpContext = (_requestContext != null) ? _requestContext.HttpContext : new EmptyHttpContext();
				}
				return _httpContext;
			}
			set { _httpContext = value; }
		}

		public RequestContext RequestContext
		{
			get
			{
				if (_requestContext == null)
				{
					// still need explicit calls to constructors since the property getters are virtual and might return null
					var httpContext = HttpContext ?? new EmptyHttpContext();
					var routeData = RouteData ?? new RouteData();

					_requestContext = new RequestContext(httpContext, routeData);
				}
				return _requestContext;
			}
			set { _requestContext = value; }
		}

		public virtual RouteData RouteData
		{
			get
			{
				if (_routeData == null)
				{
					_routeData = (_requestContext != null) ? _requestContext.RouteData : new RouteData();
				}
				return _routeData;
			}
			set { _routeData = value; }
		}

		sealed class EmptyHttpContext : HttpContextBase {}
	}
}