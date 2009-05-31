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
	public class ViewPage<TModel> : ViewPage where TModel : class
	{
		ViewDataDictionary<TModel> _viewData;

		public new AjaxHelper<TModel> Ajax { get; set; }

		public new HtmlHelper<TModel> Html { get; set; }

		public new TModel Model
		{
			get { return ViewData.Model; }
		}

		public new ViewDataDictionary<TModel> ViewData
		{
			get
			{
				if (_viewData == null)
				{
					SetViewData(new ViewDataDictionary<TModel>());
				}
				return _viewData;
			}
			set { SetViewData(value); }
		}

		public override void InitHelpers()
		{
			base.InitHelpers();

			Ajax = new AjaxHelper<TModel>(ViewContext, this);
			Html = new HtmlHelper<TModel>(ViewContext, this);
		}

		protected override void SetViewData(ViewDataDictionary viewData)
		{
			_viewData = new ViewDataDictionary<TModel>(viewData);

			base.SetViewData(_viewData);
		}
	}
}