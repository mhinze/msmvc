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

namespace System.Web.Mvc
{
	public class ViewContext : ControllerContext
	{
		// parameterless constructor used for mocking
		public ViewContext() {}

		public ViewContext(ControllerContext controllerContext, IView view, ViewDataDictionary viewData,
		                   TempDataDictionary tempData)
			: base(controllerContext)
		{
			if (controllerContext == null)
			{
				throw new ArgumentNullException("controllerContext");
			}
			if (view == null)
			{
				throw new ArgumentNullException("view");
			}
			if (viewData == null)
			{
				throw new ArgumentNullException("viewData");
			}
			if (tempData == null)
			{
				throw new ArgumentNullException("tempData");
			}

			View = view;
			ViewData = viewData;
			TempData = tempData;
		}

		public virtual IView View { get; set; }

		public virtual ViewDataDictionary ViewData { get; set; }

		public virtual TempDataDictionary TempData { get; set; }
	}
}