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
	public class ExceptionContext : ControllerContext
	{
		ActionResult _result;

		// parameterless constructor used for mocking
		public ExceptionContext() {}

		public ExceptionContext(ControllerContext controllerContext, Exception exception)
			: base(controllerContext)
		{
			if (exception == null)
			{
				throw new ArgumentNullException("exception");
			}

			Exception = exception;
		}

		public virtual Exception Exception { get; set; }

		public bool ExceptionHandled { get; set; }

		public ActionResult Result
		{
			get { return _result ?? EmptyResult.Instance; }
			set { _result = value; }
		}
	}
}