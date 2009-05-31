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

using System.Collections;

namespace System.Web.Mvc
{
	public class SelectList : MultiSelectList
	{
		public SelectList(IEnumerable items)
			: this(items, null /* selectedValue */) {}

		public SelectList(IEnumerable items, object selectedValue)
			: this(items, null /* dataValuefield */, null /* dataTextField */, selectedValue) {}

		public SelectList(IEnumerable items, string dataValueField, string dataTextField)
			: this(items, dataValueField, dataTextField, null /* selectedValue */) {}

		public SelectList(IEnumerable items, string dataValueField, string dataTextField, object selectedValue)
			: base(items, dataValueField, dataTextField, ToEnumerable(selectedValue))
		{
			SelectedValue = selectedValue;
		}

		public object SelectedValue { get; private set; }

		static IEnumerable ToEnumerable(object selectedValue)
		{
			return (selectedValue != null) ? new[] {selectedValue} : null;
		}
	}
}