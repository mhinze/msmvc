﻿/* ****************************************************************************
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
using System.Linq;
using System.Reflection;
using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	public class ReflectedControllerDescriptor : ControllerDescriptor
	{
		ActionDescriptor[] _canonicalActionsCache;
		readonly Type _controllerType;
		readonly ActionMethodSelector _selector;

		public ReflectedControllerDescriptor(Type controllerType)
		{
			if (controllerType == null)
			{
				throw new ArgumentNullException("controllerType");
			}

			_controllerType = controllerType;
			_selector = new ActionMethodSelector(_controllerType);
		}

		public override sealed Type ControllerType
		{
			get { return _controllerType; }
		}

		public override ActionDescriptor FindAction(ControllerContext controllerContext, string actionName)
		{
			if (controllerContext == null)
			{
				throw new ArgumentNullException("controllerContext");
			}
			if (String.IsNullOrEmpty(actionName))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "actionName");
			}

			var matched = _selector.FindActionMethod(controllerContext, actionName);
			if (matched == null)
			{
				return null;
			}

			return new ReflectedActionDescriptor(matched, actionName, this);
		}

		MethodInfo[] GetAllActionMethodsFromSelector()
		{
			var allValidMethods = new List<MethodInfo>();
			allValidMethods.AddRange(_selector.AliasedMethods);
			allValidMethods.AddRange(_selector.NonAliasedMethods.SelectMany(g => g));
			return allValidMethods.ToArray();
		}

		public override ActionDescriptor[] GetCanonicalActions()
		{
			var actions = LazilyFetchCanonicalActionsCollection();

			// need to clone array so that user modifications aren't accidentally stored
			return (ActionDescriptor[])actions.Clone();
		}

		public override object[] GetCustomAttributes(bool inherit)
		{
			return ControllerType.GetCustomAttributes(inherit);
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return ControllerType.GetCustomAttributes(attributeType, inherit);
		}

		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return ControllerType.IsDefined(attributeType, inherit);
		}

		ActionDescriptor[] LazilyFetchCanonicalActionsCollection()
		{
			return DescriptorUtil.LazilyFetchOrCreateDescriptors<MethodInfo, ActionDescriptor>(
				ref _canonicalActionsCache /* cacheLocation */,
				GetAllActionMethodsFromSelector /* initializer */,
				methodInfo => ReflectedActionDescriptor.TryCreateDescriptor(methodInfo, methodInfo.Name, this) /* converter */);
		}
	}
}