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
	public class AjaxHelper<TModel> : AjaxHelper where TModel : class
	{
		readonly ViewDataDictionary<TModel> _viewData;

		public AjaxHelper(ViewContext viewContext, IViewDataContainer viewDataContainer)
			: this(viewContext, viewDataContainer, RouteTable.Routes) {}

		public AjaxHelper(ViewContext viewContext, IViewDataContainer viewDataContainer, RouteCollection routeCollection)
			: base(viewContext, viewDataContainer, routeCollection)
		{
			_viewData = new ViewDataDictionary<TModel>(viewDataContainer.ViewData);
		}

		public new ViewDataDictionary<TModel> ViewData
		{
			get { return _viewData; }
		}
	}
}