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
using System.Linq.Expressions;
using System.Reflection;

namespace System.Web.Mvc
{
	// The methods in this class don't perform error checking; that is the responsibility of the
	// caller.
	internal sealed class ActionMethodDispatcher
	{
		delegate object ActionExecutor(ControllerBase controller, object[] parameters);

		delegate void VoidActionExecutor(ControllerBase controller, object[] parameters);

		readonly ActionExecutor _executor;

		public ActionMethodDispatcher(MethodInfo methodInfo)
		{
			_executor = GetExecutor(methodInfo);
			MethodInfo = methodInfo;
		}

		public MethodInfo MethodInfo { get; private set; }

		public object Execute(ControllerBase controller, object[] parameters)
		{
			return _executor(controller, parameters);
		}

		static ActionExecutor GetExecutor(MethodInfo methodInfo)
		{
			// Parameters to executor
			var controllerParameter = Expression.Parameter(typeof(ControllerBase), "controller");
			var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

			// Build parameter list
			var parameters = new List<Expression>();
			var paramInfos = methodInfo.GetParameters();
			for (var i = 0; i < paramInfos.Length; i++)
			{
				var paramInfo = paramInfos[i];
				var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
				var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

				// valueCast is "(Ti) parameters[i]"
				parameters.Add(valueCast);
			}

			// Call method
			var instanceCast = (!methodInfo.IsStatic) ? Expression.Convert(controllerParameter, methodInfo.ReflectedType) : null;
			var methodCall = Expression.Call(instanceCast, methodInfo, parameters);

			// methodCall is "((TController) controller) method((T0) parameters[0], (T1) parameters[1], ...)"
			// Create function
			if (methodCall.Type == typeof(void))
			{
				var lambda = Expression.Lambda<VoidActionExecutor>(methodCall, controllerParameter, parametersParameter);
				var voidExecutor = lambda.Compile();
				return WrapVoidAction(voidExecutor);
			}
			else
			{
				// must coerce methodCall to match ActionExecutor signature
				var castMethodCall = Expression.Convert(methodCall, typeof(object));
				var lambda = Expression.Lambda<ActionExecutor>(castMethodCall, controllerParameter, parametersParameter);
				return lambda.Compile();
			}
		}

		static ActionExecutor WrapVoidAction(VoidActionExecutor executor)
		{
			return delegate(ControllerBase controller, object[] parameters)
			       {
			       	executor(controller, parameters);
			       	return null;
			       };
		}
	}
}