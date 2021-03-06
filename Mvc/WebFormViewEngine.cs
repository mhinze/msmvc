﻿/* ****************************************************************************
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

using System.Net;

namespace System.Web.Mvc
{
	public class WebFormViewEngine : VirtualPathProviderViewEngine
	{
		IBuildManager _buildManager;

		public WebFormViewEngine()
		{
			MasterLocationFormats = new[]
			{
				"~/Views/{1}/{0}.master",
				"~/Views/Shared/{0}.master"
			};

			ViewLocationFormats = new[]
			{
				"~/Views/{1}/{0}.aspx",
				"~/Views/{1}/{0}.ascx",
				"~/Views/Shared/{0}.aspx",
				"~/Views/Shared/{0}.ascx"
			};

			PartialViewLocationFormats = ViewLocationFormats;
		}

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

		protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
		{
			return new WebFormView(partialPath, null);
		}

		protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
		{
			return new WebFormView(viewPath, masterPath);
		}

		protected override bool FileExists(ControllerContext controllerContext, string virtualPath)
		{
			try
			{
				var viewInstance = BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(object));

				return viewInstance != null;
			}
			catch (HttpException he)
			{
				if (he.GetHttpCode() == (int)HttpStatusCode.NotFound)
				{
					// If BuildManager returns a 404 (Not Found) that means the file did not exist
					return false;
				}

				// All other error codes imply other errors such as compilation or parsing errors
				throw;
			}
			catch
			{
				return false;
			}
		}
	}
}