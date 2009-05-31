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
using System.Globalization;
using System.Text;
using System.Web.Mvc.Resources;
using System.Web.Routing;

namespace System.Web.Mvc
{
	public class DefaultControllerFactory : IControllerFactory
	{
		IBuildManager _buildManager;
		ControllerBuilder _controllerBuilder;
		ControllerTypeCache _instanceControllerTypeCache;
		static readonly ControllerTypeCache _staticControllerTypeCache = new ControllerTypeCache();

		internal IBuildManager BuildManager
		{
			get
			{
				if (_buildManager == null)
				{
					_buildManager = new BuildManagerWrapper();
				}
				return _buildManager;
			}
			set { _buildManager = value; }
		}

		internal ControllerBuilder ControllerBuilder
		{
			get { return _controllerBuilder ?? ControllerBuilder.Current; }
			set { _controllerBuilder = value; }
		}

		internal ControllerTypeCache ControllerTypeCache
		{
			get { return _instanceControllerTypeCache ?? _staticControllerTypeCache; }
			set { _instanceControllerTypeCache = value; }
		}

		public RequestContext RequestContext { get; set; }

		public virtual IController CreateController(RequestContext requestContext, string controllerName)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}
			if (String.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "controllerName");
			}
			RequestContext = requestContext;
			var controllerType = GetControllerType(controllerName);
			var controller = GetControllerInstance(controllerType);
			return controller;
		}

		protected internal virtual IController GetControllerInstance(Type controllerType)
		{
			if (controllerType == null)
			{
				throw new HttpException(404,
				                        String.Format(
				                        	CultureInfo.CurrentUICulture,
				                        	MvcResources.DefaultControllerFactory_NoControllerFound,
				                        	RequestContext.HttpContext.Request.Path));
			}
			if (!typeof(IController).IsAssignableFrom(controllerType))
			{
				throw new ArgumentException(
					String.Format(
						CultureInfo.CurrentUICulture,
						MvcResources.DefaultControllerFactory_TypeDoesNotSubclassControllerBase,
						controllerType),
					"controllerType");
			}
			try
			{
				return (IController)Activator.CreateInstance(controllerType);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(
					String.Format(
						CultureInfo.CurrentUICulture,
						MvcResources.DefaultControllerFactory_ErrorCreatingController,
						controllerType),
					ex);
			}
		}

		protected internal virtual Type GetControllerType(string controllerName)
		{
			if (String.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "controllerName");
			}

			// first search in the current route's namespace collection
			object routeNamespacesObj;
			Type match;
			if (RequestContext != null && RequestContext.RouteData.DataTokens.TryGetValue("Namespaces", out routeNamespacesObj))
			{
				var routeNamespaces = routeNamespacesObj as IEnumerable<string>;
				if (routeNamespaces != null)
				{
					var nsHash = new HashSet<string>(routeNamespaces, StringComparer.OrdinalIgnoreCase);
					match = GetControllerTypeWithinNamespaces(controllerName, nsHash);
					if (match != null)
					{
						return match;
					}
				}
			}

			// then search in the application's default namespace collection
			var nsDefaults = new HashSet<string>(ControllerBuilder.DefaultNamespaces, StringComparer.OrdinalIgnoreCase);
			match = GetControllerTypeWithinNamespaces(controllerName, nsDefaults);
			if (match != null)
			{
				return match;
			}

			// if all else fails, search every namespace
			return GetControllerTypeWithinNamespaces(controllerName, null /* namespaces */);
		}

		Type GetControllerTypeWithinNamespaces(string controllerName, HashSet<string> namespaces)
		{
			// Once the master list of controllers has been created we can quickly index into it
			ControllerTypeCache.EnsureInitialized(BuildManager);

			var matchingTypes = ControllerTypeCache.GetControllerTypes(controllerName, namespaces);
			switch (matchingTypes.Count)
			{
				case 0:
					// no matching types
					return null;

				case 1:
					// single matching type
					return matchingTypes[0];

				default:
					// multiple matching types
					// we need to generate an exception containing all the controller types
					var sb = new StringBuilder();
					foreach (var matchedType in matchingTypes)
					{
						sb.AppendLine();
						sb.Append(matchedType.FullName);
					}
					throw new InvalidOperationException(
						String.Format(
							CultureInfo.CurrentUICulture,
							MvcResources.DefaultControllerFactory_ControllerNameAmbiguous,
							controllerName, sb));
			}
		}

		public virtual void ReleaseController(IController controller)
		{
			var disposable = controller as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
	}
}