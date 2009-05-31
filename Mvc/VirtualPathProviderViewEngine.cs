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
using System.Linq;
using System.Web.Hosting;
using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	public abstract class VirtualPathProviderViewEngine : IViewEngine
	{
		// format is ":ViewCacheEntry:{cacheType}:{prefix}:{name}:{controllerName}:"
		const string _cacheKeyFormat = ":ViewCacheEntry:{0}:{1}:{2}:{3}:";
		const string _cacheKeyPrefix_Master = "Master";
		const string _cacheKeyPrefix_Partial = "Partial";
		const string _cacheKeyPrefix_View = "View";
		static readonly string[] _emptyLocations = new string[0];

		VirtualPathProvider _vpp;

		public string[] MasterLocationFormats { get; set; }

		public string[] PartialViewLocationFormats { get; set; }

		public IViewLocationCache ViewLocationCache { get; set; }

		public string[] ViewLocationFormats { get; set; }

		protected VirtualPathProvider VirtualPathProvider
		{
			get
			{
				if (_vpp == null)
				{
					_vpp = HostingEnvironment.VirtualPathProvider;
				}
				return _vpp;
			}
			set { _vpp = value; }
		}

		protected VirtualPathProviderViewEngine()
		{
			if (HttpContext.Current == null || HttpContext.Current.IsDebuggingEnabled)
			{
				ViewLocationCache = DefaultViewLocationCache.Null;
			}
			else
			{
				ViewLocationCache = new DefaultViewLocationCache();
			}
		}

		string CreateCacheKey(string prefix, string name, string controllerName)
		{
			return String.Format(CultureInfo.InvariantCulture, _cacheKeyFormat,
			                     GetType().AssemblyQualifiedName, prefix, name, controllerName);
		}

		protected abstract IView CreatePartialView(ControllerContext controllerContext, string partialPath);

		protected abstract IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath);

		protected virtual bool FileExists(ControllerContext controllerContext, string virtualPath)
		{
			return VirtualPathProvider.FileExists(virtualPath);
		}

		public virtual ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName,
		                                                bool useCache)
		{
			if (controllerContext == null)
			{
				throw new ArgumentNullException("controllerContext");
			}
			if (String.IsNullOrEmpty(partialViewName))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "partialViewName");
			}

			string[] searched;
			var controllerName = controllerContext.RouteData.GetRequiredString("controller");
			var partialPath = GetPath(controllerContext, PartialViewLocationFormats, "PartialViewLocationFormats",
			                          partialViewName, controllerName, _cacheKeyPrefix_Partial, useCache, out searched);

			if (String.IsNullOrEmpty(partialPath))
			{
				return new ViewEngineResult(searched);
			}

			return new ViewEngineResult(CreatePartialView(controllerContext, partialPath), this);
		}

		public virtual ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName,
		                                         bool useCache)
		{
			if (controllerContext == null)
			{
				throw new ArgumentNullException("controllerContext");
			}
			if (String.IsNullOrEmpty(viewName))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "viewName");
			}

			string[] viewLocationsSearched;
			string[] masterLocationsSearched;

			var controllerName = controllerContext.RouteData.GetRequiredString("controller");
			var viewPath = GetPath(controllerContext, ViewLocationFormats, "ViewLocationFormats", viewName, controllerName,
			                       _cacheKeyPrefix_View, useCache, out viewLocationsSearched);
			var masterPath = GetPath(controllerContext, MasterLocationFormats, "MasterLocationFormats", masterName,
			                         controllerName, _cacheKeyPrefix_Master, useCache, out masterLocationsSearched);

			if (String.IsNullOrEmpty(viewPath) || (String.IsNullOrEmpty(masterPath) && !String.IsNullOrEmpty(masterName)))
			{
				return new ViewEngineResult(viewLocationsSearched.Union(masterLocationsSearched));
			}

			return new ViewEngineResult(CreateView(controllerContext, viewPath, masterPath), this);
		}

		string GetPath(ControllerContext controllerContext, string[] locations, string locationsPropertyName, string name,
		               string controllerName, string cacheKeyPrefix, bool useCache, out string[] searchedLocations)
		{
			searchedLocations = _emptyLocations;

			if (String.IsNullOrEmpty(name))
			{
				return String.Empty;
			}

			if (locations == null || locations.Length == 0)
			{
				throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture,
				                                                  MvcResources.Common_PropertyCannotBeNullOrEmpty,
				                                                  locationsPropertyName));
			}

			var nameRepresentsPath = IsSpecificPath(name);
			var cacheKey = CreateCacheKey(cacheKeyPrefix, name, (nameRepresentsPath) ? String.Empty : controllerName);

			if (useCache)
			{
				var result = ViewLocationCache.GetViewLocation(controllerContext.HttpContext, cacheKey);
				if (result != null)
				{
					return result;
				}
			}

			return (nameRepresentsPath)
			       	?
			       		GetPathFromSpecificName(controllerContext, name, cacheKey, ref searchedLocations)
			       	:
			       		GetPathFromGeneralName(controllerContext, locations, name, controllerName, cacheKey, ref searchedLocations);
		}

		string GetPathFromGeneralName(ControllerContext controllerContext, string[] locations, string name,
		                              string controllerName, string cacheKey, ref string[] searchedLocations)
		{
			var result = String.Empty;
			searchedLocations = new string[locations.Length];

			for (var i = 0; i < locations.Length; i++)
			{
				var virtualPath = String.Format(CultureInfo.InvariantCulture, locations[i], name, controllerName);

				if (FileExists(controllerContext, virtualPath))
				{
					searchedLocations = _emptyLocations;
					result = virtualPath;
					ViewLocationCache.InsertViewLocation(controllerContext.HttpContext, cacheKey, result);
					break;
				}

				searchedLocations[i] = virtualPath;
			}

			return result;
		}

		string GetPathFromSpecificName(ControllerContext controllerContext, string name, string cacheKey,
		                               ref string[] searchedLocations)
		{
			var result = name;

			if (!FileExists(controllerContext, name))
			{
				result = String.Empty;
				searchedLocations = new[] {name};
			}

			ViewLocationCache.InsertViewLocation(controllerContext.HttpContext, cacheKey, result);
			return result;
		}

		static bool IsSpecificPath(string name)
		{
			var c = name[0];
			return (c == '~' || c == '/');
		}

		public virtual void ReleaseView(ControllerContext controllerContext, IView view)
		{
			var disposable = view as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
	}
}