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
using System.Reflection;

namespace System.Web.Mvc
{
	public abstract class ActionDescriptor : ICustomAttributeProvider
	{
		static readonly ActionSelector[] _emptySelectors = new ActionSelector[0];

		public abstract string ActionName { get; }

		public abstract ControllerDescriptor ControllerDescriptor { get; }

		public abstract object Execute(ControllerContext controllerContext, IDictionary<string, object> parameters);

		public virtual object[] GetCustomAttributes(bool inherit)
		{
			return GetCustomAttributes(typeof(object), inherit);
		}

		public virtual object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}

			return (object[])Array.CreateInstance(attributeType, 0);
		}

		public virtual FilterInfo GetFilters()
		{
			return new FilterInfo();
		}

		public abstract ParameterDescriptor[] GetParameters();

		public virtual ICollection<ActionSelector> GetSelectors()
		{
			return _emptySelectors;
		}

		public virtual bool IsDefined(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}

			return false;
		}
	}
}