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

namespace System.Web.Mvc
{
	public class ActionExecutingContext : ControllerContext
	{
		// parameterless constructor used for mocking
		public ActionExecutingContext() {}

		public ActionExecutingContext(ControllerContext controllerContext, ActionDescriptor actionDescriptor,
		                              IDictionary<string, object> actionParameters)
			: base(controllerContext)
		{
			if (actionDescriptor == null)
			{
				throw new ArgumentNullException("actionDescriptor");
			}
			if (actionParameters == null)
			{
				throw new ArgumentNullException("actionParameters");
			}

			ActionDescriptor = actionDescriptor;
			ActionParameters = actionParameters;
		}

		public virtual ActionDescriptor ActionDescriptor { get; set; }

		public virtual IDictionary<string, object> ActionParameters { get; set; }

		public ActionResult Result { get; set; }
	}
}