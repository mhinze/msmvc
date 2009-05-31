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

using System.Web.UI;

namespace System.Web.Mvc
{
	[FileLevelControlBuilder(typeof(ViewPageControlBuilder))]
	public class ViewPage : Page, IViewDataContainer
	{
		string _masterLocation;
		ViewDataDictionary _viewData;

		public AjaxHelper Ajax { get; set; }

		public HtmlHelper Html { get; set; }

		public string MasterLocation
		{
			get { return _masterLocation ?? String.Empty; }
			set { _masterLocation = value; }
		}

		public object Model
		{
			get { return ViewData.Model; }
		}

		public TempDataDictionary TempData
		{
			get { return ViewContext.TempData; }
		}

		public UrlHelper Url { get; set; }

		public ViewContext ViewContext { get; set; }

		public ViewDataDictionary ViewData
		{
			get
			{
				if (_viewData == null)
				{
					SetViewData(new ViewDataDictionary());
				}
				return _viewData;
			}
			set { SetViewData(value); }
		}

		public HtmlTextWriter Writer { get; private set; }

		public virtual void InitHelpers()
		{
			Ajax = new AjaxHelper(ViewContext, this);
			Html = new HtmlHelper(ViewContext, this);
			Url = new UrlHelper(ViewContext.RequestContext);
		}

		protected override void OnPreInit(EventArgs e)
		{
			base.OnPreInit(e);

			if (!String.IsNullOrEmpty(MasterLocation))
			{
				MasterPageFile = MasterLocation;
			}
		}

		protected override void Render(HtmlTextWriter writer)
		{
			Writer = writer;
			try
			{
				base.Render(writer);
			}
			finally
			{
				Writer = null;
			}
		}

		public virtual void RenderView(ViewContext viewContext)
		{
			ViewContext = viewContext;
			InitHelpers();
			// Tracing requires Page IDs to be unique.
			ID = Guid.NewGuid().ToString();
			ProcessRequest(HttpContext.Current);
		}

		protected virtual void SetViewData(ViewDataDictionary viewData)
		{
			_viewData = viewData;
		}
	}
}