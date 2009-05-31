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
	// represents a result that performs a redirection given some URI
	public class RedirectResult : ActionResult
	{
		public RedirectResult(string url)
		{
			if (String.IsNullOrEmpty(url))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "url");
			}

			Url = url;
		}

		public string Url { get; private set; }

		public override void ExecuteResult(ControllerContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			var destinationUrl = UrlHelper.Content(Url, context.HttpContext);
			context.HttpContext.Response.Redirect(destinationUrl, false /* endResponse */);
		}
	}
}