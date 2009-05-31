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
using System.Web.Routing;

namespace System.Web.Mvc
{
	public abstract class ControllerBase : MarshalByRefObject, IController
	{
		TempDataDictionary _tempDataDictionary;
		bool _validateRequest = true;
		IDictionary<string, ValueProviderResult> _valueProvider;
		ViewDataDictionary _viewDataDictionary;

		public ControllerContext ControllerContext { get; set; }

		public TempDataDictionary TempData
		{
			get
			{
				if (_tempDataDictionary == null)
				{
					_tempDataDictionary = new TempDataDictionary();
				}
				return _tempDataDictionary;
			}
			set { _tempDataDictionary = value; }
		}

		public bool ValidateRequest
		{
			get { return _validateRequest; }
			set { _validateRequest = value; }
		}

		public IDictionary<string, ValueProviderResult> ValueProvider
		{
			get
			{
				if (_valueProvider == null)
				{
					_valueProvider = new ValueProviderDictionary(ControllerContext);
				}
				return _valueProvider;
			}
			set { _valueProvider = value; }
		}

		public ViewDataDictionary ViewData
		{
			get
			{
				if (_viewDataDictionary == null)
				{
					_viewDataDictionary = new ViewDataDictionary();
				}
				return _viewDataDictionary;
			}
			set { _viewDataDictionary = value; }
		}

		protected virtual void Execute(RequestContext requestContext)
		{
			if (requestContext == null)
			{
				throw new ArgumentNullException("requestContext");
			}

			Initialize(requestContext);
			ExecuteCore();
		}

		protected abstract void ExecuteCore();

		protected virtual void Initialize(RequestContext requestContext)
		{
			ControllerContext = new ControllerContext(requestContext, this);
		}

		void IController.Execute(RequestContext requestContext)
		{
			Execute(requestContext);
		}
	}
}