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
using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	public class ReflectedActionDescriptor : ActionDescriptor
	{
		static readonly ActionMethodDispatcherCache _staticDispatcherCache = new ActionMethodDispatcherCache();
		ActionMethodDispatcherCache _instanceDispatcherCache;

		readonly string _actionName;
		readonly ControllerDescriptor _controllerDescriptor;
		ParameterDescriptor[] _parametersCache;

		public ReflectedActionDescriptor(MethodInfo methodInfo, string actionName, ControllerDescriptor controllerDescriptor)
			: this(methodInfo, actionName, controllerDescriptor, true /* validateMethod */) {}

		internal ReflectedActionDescriptor(MethodInfo methodInfo, string actionName, ControllerDescriptor controllerDescriptor,
		                                   bool validateMethod)
		{
			if (methodInfo == null)
			{
				throw new ArgumentNullException("methodInfo");
			}
			if (String.IsNullOrEmpty(actionName))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "actionName");
			}
			if (controllerDescriptor == null)
			{
				throw new ArgumentNullException("controllerDescriptor");
			}

			if (validateMethod)
			{
				var failedMessage = VerifyActionMethodIsCallable(methodInfo);
				if (failedMessage != null)
				{
					throw new ArgumentException(failedMessage, "methodInfo");
				}
			}

			MethodInfo = methodInfo;
			_actionName = actionName;
			_controllerDescriptor = controllerDescriptor;
		}

		public override string ActionName
		{
			get { return _actionName; }
		}

		public override ControllerDescriptor ControllerDescriptor
		{
			get { return _controllerDescriptor; }
		}

		internal ActionMethodDispatcherCache DispatcherCache
		{
			get
			{
				if (_instanceDispatcherCache == null)
				{
					_instanceDispatcherCache = _staticDispatcherCache;
				}
				return _instanceDispatcherCache;
			}
			set { _instanceDispatcherCache = value; }
		}

		public MethodInfo MethodInfo { get; private set; }

		public override object Execute(ControllerContext controllerContext, IDictionary<string, object> parameters)
		{
			if (controllerContext == null)
			{
				throw new ArgumentNullException("controllerContext");
			}
			if (parameters == null)
			{
				throw new ArgumentNullException("parameters");
			}

			var parameterInfos = MethodInfo.GetParameters();
			var rawParameterValues = from parameterInfo in parameterInfos
			                         select ExtractParameterFromDictionary(parameterInfo, parameters, MethodInfo);
			var parametersArray = rawParameterValues.ToArray();

			var dispatcher = DispatcherCache.GetDispatcher(MethodInfo);
			var actionReturnValue = dispatcher.Execute(controllerContext.Controller, parametersArray);
			return actionReturnValue;
		}

		static object ExtractParameterFromDictionary(ParameterInfo parameterInfo, IDictionary<string, object> parameters,
		                                             MethodInfo methodInfo)
		{
			object value;

			if (!parameters.TryGetValue(parameterInfo.Name, out value))
			{
				// the key should always be present, even if the parameter value is null
				var message = String.Format(CultureInfo.CurrentUICulture,
				                            MvcResources.ReflectedActionDescriptor_ParameterNotInDictionary,
				                            parameterInfo.Name, parameterInfo.ParameterType, methodInfo, methodInfo.DeclaringType);
				throw new ArgumentException(message, "parameters");
			}

			if (value == null && !TypeHelpers.TypeAllowsNullValue(parameterInfo.ParameterType))
			{
				// tried to pass a null value for a non-nullable parameter type
				var message = String.Format(CultureInfo.CurrentUICulture,
				                            MvcResources.ReflectedActionDescriptor_ParameterCannotBeNull,
				                            parameterInfo.Name, parameterInfo.ParameterType, methodInfo, methodInfo.DeclaringType);
				throw new ArgumentException(message, "parameters");
			}

			if (value != null && !parameterInfo.ParameterType.IsInstanceOfType(value))
			{
				// value was supplied but is not of the proper type
				var message = String.Format(CultureInfo.CurrentUICulture,
				                            MvcResources.ReflectedActionDescriptor_ParameterValueHasWrongType,
				                            parameterInfo.Name, methodInfo, methodInfo.DeclaringType, value.GetType(),
				                            parameterInfo.ParameterType);
				throw new ArgumentException(message, "parameters");
			}

			return value;
		}

		public override object[] GetCustomAttributes(bool inherit)
		{
			return MethodInfo.GetCustomAttributes(inherit);
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return MethodInfo.GetCustomAttributes(attributeType, inherit);
		}

		public override FilterInfo GetFilters()
		{
			// Enumerable.OrderBy() is a stable sort, so this method preserves scope ordering.
			var typeFilters =
				(FilterAttribute[])MethodInfo.ReflectedType.GetCustomAttributes(typeof(FilterAttribute), true /* inherit */);
			var methodFilters = (FilterAttribute[])MethodInfo.GetCustomAttributes(typeof(FilterAttribute), true /* inherit */);
			var orderedFilters = typeFilters.Concat(methodFilters).OrderBy(attr => attr.Order).ToList();

			var filterInfo = new FilterInfo();
			MergeFiltersIntoList(orderedFilters, filterInfo.ActionFilters);
			MergeFiltersIntoList(orderedFilters, filterInfo.AuthorizationFilters);
			MergeFiltersIntoList(orderedFilters, filterInfo.ExceptionFilters);
			MergeFiltersIntoList(orderedFilters, filterInfo.ResultFilters);
			return filterInfo;
		}

		public override ParameterDescriptor[] GetParameters()
		{
			var parameters = LazilyFetchParametersCollection();

			// need to clone array so that user modifications aren't accidentally stored
			return (ParameterDescriptor[])parameters.Clone();
		}

		public override ICollection<ActionSelector> GetSelectors()
		{
			var attrs =
				(ActionMethodSelectorAttribute[])
				MethodInfo.GetCustomAttributes(typeof(ActionMethodSelectorAttribute), true /* inherit */);
			var selectors = Array.ConvertAll(attrs,
			                                 attr =>
			                                 (ActionSelector)
			                                 (controllerContext => attr.IsValidForRequest(controllerContext, MethodInfo)));
			return selectors;
		}

		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return MethodInfo.IsDefined(attributeType, inherit);
		}

		ParameterDescriptor[] LazilyFetchParametersCollection()
		{
			return DescriptorUtil.LazilyFetchOrCreateDescriptors<ParameterInfo, ParameterDescriptor>(
				ref _parametersCache /* cacheLocation */,
				MethodInfo.GetParameters /* initializer */,
				parameterInfo => new ReflectedParameterDescriptor(parameterInfo, this) /* converter */);
		}

		static void MergeFiltersIntoList<TFilter>(IList<FilterAttribute> allFilters, IList<TFilter> destFilters)
			where TFilter : class
		{
			foreach (var filter in allFilters)
			{
				var castFilter = filter as TFilter;
				if (castFilter != null)
				{
					destFilters.Add(castFilter);
				}
			}
		}

		internal static ReflectedActionDescriptor TryCreateDescriptor(MethodInfo methodInfo, string name,
		                                                              ControllerDescriptor controllerDescriptor)
		{
			var descriptor = new ReflectedActionDescriptor(methodInfo, name, controllerDescriptor, false /* validateMethod */);
			var failedMessage = VerifyActionMethodIsCallable(methodInfo);
			return (failedMessage == null) ? descriptor : null;
		}

		static string VerifyActionMethodIsCallable(MethodInfo methodInfo)
		{
			// we can't call instance methods where the 'this' parameter is a type other than ControllerBase
			if (!methodInfo.IsStatic && !typeof(ControllerBase).IsAssignableFrom(methodInfo.ReflectedType))
			{
				return String.Format(CultureInfo.CurrentUICulture,
				                     MvcResources.ReflectedActionDescriptor_CannotCallInstanceMethodOnNonControllerType,
				                     methodInfo, methodInfo.ReflectedType.FullName);
			}

			// we can't call methods with open generic type parameters
			if (methodInfo.ContainsGenericParameters)
			{
				return String.Format(CultureInfo.CurrentUICulture,
				                     MvcResources.ReflectedActionDescriptor_CannotCallOpenGenericMethods,
				                     methodInfo, methodInfo.ReflectedType.FullName);
			}

			// we can't call methods with ref/out parameters
			var parameterInfos = methodInfo.GetParameters();
			foreach (var parameterInfo in parameterInfos)
			{
				if (parameterInfo.IsOut || parameterInfo.ParameterType.IsByRef)
				{
					return String.Format(CultureInfo.CurrentUICulture,
					                     MvcResources.ReflectedActionDescriptor_CannotCallMethodsWithOutOrRefParameters,
					                     methodInfo, methodInfo.ReflectedType.FullName, parameterInfo);
				}
			}

			// we can call this method
			return null;
		}
	}
}