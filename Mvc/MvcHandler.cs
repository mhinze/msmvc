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
using System.Reflection;
using System.Web.Mvc.Resources;
using System.Web.Routing;
using System.Web.SessionState;

namespace System.Web.Mvc
{
	public class MvcHandler : IHttpHandler, IRequiresSessionState
	{
		ControllerBuilder _controllerBuilder;
		static readonly string MvcVersion = GetMvcVersionString();

		public static readonly string MvcVersionHeaderName = "X-AspNetMvc-Version";

		public MvcHandler(RequestContext requestContext)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			RequestContext = requestContext;
		}

		protected virtual bool IsReusable
		{
			get { return false; }
		}

		internal ControllerBuilder ControllerBuilder
		{
			get
			{
				if (_controllerBuilder == null)
				{
					_controllerBuilder = ControllerBuilder.Current;
				}
				return _controllerBuilder;
			}
			set { _controllerBuilder = value; }
		}

		public static bool DisableMvcResponseHeader { get; set; }

		public RequestContext RequestContext { get; private set; }

		protected internal virtual void AddVersionHeader(HttpContextBase httpContext)
		{
			if (!DisableMvcResponseHeader)
			{
				httpContext.Response.AppendHeader(MvcVersionHeaderName, MvcVersion);
			}
		}

		static string GetMvcVersionString()
		{
			// DevDiv 216459:
			// This code originally used Assembly.GetName(), but that requires FileIOPermission, which isn't granted in
			// medium trust. However, Assembly.FullName *is* accessible in medium trust.
			return new AssemblyName(typeof(MvcHandler).Assembly.FullName).Version.ToString(2);
		}

		protected virtual void ProcessRequest(HttpContext httpContext)
		{
			HttpContextBase iHttpContext = new HttpContextWrapper(httpContext);
			ProcessRequest(iHttpContext);
		}

		protected internal virtual void ProcessRequest(HttpContextBase httpContext)
		{
			AddVersionHeader(httpContext);

			// Get the controller type
			var controllerName = RequestContext.RouteData.GetRequiredString("controller");

			// Instantiate the controller and call Execute
			var factory = ControllerBuilder.GetControllerFactory();
			var controller = factory.CreateController(RequestContext, controllerName);
			if (controller == null)
			{
				throw new InvalidOperationException(
					String.Format(
						CultureInfo.CurrentUICulture,
						MvcResources.ControllerBuilder_FactoryReturnedNull,
						factory.GetType(),
						controllerName));
			}
			try
			{
				controller.Execute(RequestContext);
			}
			finally
			{
				factory.ReleaseController(controller);
			}
		}

		bool IHttpHandler.IsReusable
		{
			get { return IsReusable; }
		}

		void IHttpHandler.ProcessRequest(HttpContext httpContext)
		{
			ProcessRequest(httpContext);
		}
	}
}