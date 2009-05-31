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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	internal sealed class ActionMethodSelector
	{
		public ActionMethodSelector(Type controllerType)
		{
			ControllerType = controllerType;
			PopulateLookupTables();
		}

		public Type ControllerType { get; private set; }

		public MethodInfo[] AliasedMethods { get; private set; }

		public ILookup<string, MethodInfo> NonAliasedMethods { get; private set; }

		AmbiguousMatchException CreateAmbiguousMatchException(List<MethodInfo> ambiguousMethods, string actionName)
		{
			var exceptionMessageBuilder = new StringBuilder();
			foreach (var methodInfo in ambiguousMethods)
			{
				var controllerAction = Convert.ToString(methodInfo, CultureInfo.CurrentUICulture);
				var controllerType = methodInfo.DeclaringType.FullName;
				exceptionMessageBuilder.AppendLine();
				exceptionMessageBuilder.AppendFormat(CultureInfo.CurrentUICulture,
				                                     MvcResources.ActionMethodSelector_AmbiguousMatchType, controllerAction,
				                                     controllerType);
			}
			var message = String.Format(CultureInfo.CurrentUICulture, MvcResources.ActionMethodSelector_AmbiguousMatch,
			                            actionName, ControllerType.Name, exceptionMessageBuilder);
			return new AmbiguousMatchException(message);
		}

		public MethodInfo FindActionMethod(ControllerContext controllerContext, string actionName)
		{
			var methodsMatchingName = GetMatchingAliasedMethods(controllerContext, actionName);
			methodsMatchingName.AddRange(NonAliasedMethods[actionName]);
			var finalMethods = RunSelectionFilters(controllerContext, methodsMatchingName);

			switch (finalMethods.Count)
			{
				case 0:
					return null;

				case 1:
					return finalMethods[0];

				default:
					throw CreateAmbiguousMatchException(finalMethods, actionName);
			}
		}

		internal List<MethodInfo> GetMatchingAliasedMethods(ControllerContext controllerContext, string actionName)
		{
			// find all aliased methods which are opting in to this request
			// to opt in, all attributes defined on the method must return true

			var methods = from methodInfo in AliasedMethods
			              let attrs =
			              	(ActionNameSelectorAttribute[])
			              	methodInfo.GetCustomAttributes(typeof(ActionNameSelectorAttribute), true /* inherit */)
			              where attrs.All(attr => attr.IsValidName(controllerContext, actionName, methodInfo))
			              select methodInfo;
			return methods.ToList();
		}

		static bool IsMethodDecoratedWithAliasingAttribute(MethodInfo methodInfo)
		{
			return methodInfo.IsDefined(typeof(ActionNameSelectorAttribute), true /* inherit */);
		}

		static bool IsValidActionMethod(MethodInfo methodInfo)
		{
			return !(methodInfo.IsSpecialName ||
			         methodInfo.GetBaseDefinition().DeclaringType.IsAssignableFrom(typeof(Controller)));
		}

		void PopulateLookupTables()
		{
			var allMethods = ControllerType.GetMethods(BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public);
			var actionMethods = Array.FindAll(allMethods, IsValidActionMethod);

			AliasedMethods = Array.FindAll(actionMethods, IsMethodDecoratedWithAliasingAttribute);
			NonAliasedMethods = actionMethods.Except(AliasedMethods).ToLookup(method => method.Name,
			                                                                  StringComparer.OrdinalIgnoreCase);
		}

		static List<MethodInfo> RunSelectionFilters(ControllerContext controllerContext, List<MethodInfo> methodInfos)
		{
			// remove all methods which are opting out of this request
			// to opt out, at least one attribute defined on the method must return false

			var matchesWithSelectionAttributes = new List<MethodInfo>();
			var matchesWithoutSelectionAttributes = new List<MethodInfo>();

			foreach (var methodInfo in methodInfos)
			{
				var attrs =
					(ActionMethodSelectorAttribute[])
					methodInfo.GetCustomAttributes(typeof(ActionMethodSelectorAttribute), true /* inherit */);
				if (attrs.Length == 0)
				{
					matchesWithoutSelectionAttributes.Add(methodInfo);
				}
				else
				{
					var info = methodInfo;
					if (attrs.All(attr => attr.IsValidForRequest(controllerContext, info)))
					{
						matchesWithSelectionAttributes.Add(methodInfo);
					}
				}
			}

			// if a matching action method had a selection attribute, consider it more specific than a matching action method
			// without a selection attribute
			return (matchesWithSelectionAttributes.Count > 0)
			       	? matchesWithSelectionAttributes
			       	: matchesWithoutSelectionAttributes;
		}
	}
}