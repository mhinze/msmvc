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
using System.Globalization;
using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	public class ControllerBuilder
	{
		Func<IControllerFactory> _factoryThunk;
		static readonly ControllerBuilder _instance = new ControllerBuilder();
		readonly HashSet<string> _namespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public ControllerBuilder()
		{
			SetControllerFactory(new DefaultControllerFactory
			{
				ControllerBuilder = this
			});
		}

		public static ControllerBuilder Current
		{
			get { return _instance; }
		}

		public HashSet<string> DefaultNamespaces
		{
			get { return _namespaces; }
		}

		public IControllerFactory GetControllerFactory()
		{
			var controllerFactoryInstance = _factoryThunk();
			return controllerFactoryInstance;
		}

		public void SetControllerFactory(IControllerFactory controllerFactory)
		{
			if (controllerFactory == null)
			{
				throw new ArgumentNullException("controllerFactory");
			}

			_factoryThunk = () => controllerFactory;
		}

		public void SetControllerFactory(Type controllerFactoryType)
		{
			if (controllerFactoryType == null)
			{
				throw new ArgumentNullException("controllerFactoryType");
			}
			if (!typeof(IControllerFactory).IsAssignableFrom(controllerFactoryType))
			{
				throw new ArgumentException(
					String.Format(
						CultureInfo.CurrentUICulture,
						MvcResources.ControllerBuilder_MissingIControllerFactory,
						controllerFactoryType),
					"controllerFactoryType");
			}

			_factoryThunk = (() =>
			                 {
			                 	try
			                 	{
			                 		return (IControllerFactory)Activator.CreateInstance(controllerFactoryType);
			                 	}
			                 	catch (Exception ex)
			                 	{
			                 		throw new InvalidOperationException(
			                 			String.Format(
			                 				CultureInfo.CurrentUICulture,
			                 				MvcResources.ControllerBuilder_ErrorCreatingControllerFactory,
			                 				controllerFactoryType),
			                 			ex);
			                 	}
			                 });
		}
	}
}