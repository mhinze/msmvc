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

using System.ComponentModel;
using System.Web.UI;

namespace System.Web.Mvc
{
	[ControlBuilder(typeof(ViewTypeControlBuilder))]
	[NonVisualControl]
	public class ViewType : Control
	{
		string _typeName;

		[DefaultValue("")]
		public string TypeName
		{
			get { return _typeName ?? String.Empty; }
			set { _typeName = value; }
		}
	}
}