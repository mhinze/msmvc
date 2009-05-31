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
using System.Threading;
using System.Web.Mvc.Resources;

namespace System.Web.Mvc
{
	public class ControllerActionInvoker : IActionInvoker
	{
		static readonly ControllerDescriptorCache _staticDescriptorCache = new ControllerDescriptorCache();

		ModelBinderDictionary _binders;
		ControllerDescriptorCache _instanceDescriptorCache;

		protected internal ModelBinderDictionary Binders
		{
			get
			{
				if (_binders == null)
				{
					_binders = ModelBinders.Binders;
				}
				return _binders;
			}
			set { _binders = value; }
		}

		internal ControllerDescriptorCache DescriptorCache
		{
			get
			{
				if (_instanceDescriptorCache == null)
				{
					_instanceDescriptorCache = _staticDescriptorCache;
				}
				return _instanceDescriptorCache;
			}
			set { _instanceDescriptorCache = value; }
		}

		static void AddControllerToFilterList<TFilter>(ControllerBase controller, IList<TFilter> filterList)
			where TFilter : class
		{
			var controllerAsFilter = controller as TFilter;
			if (controllerAsFilter != null)
			{
				filterList.Insert(0, controllerAsFilter);
			}
		}

		protected virtual ActionResult CreateActionResult(ControllerContext controllerContext,
		                                                  ActionDescriptor actionDescriptor, object actionReturnValue)
		{
			if (actionReturnValue == null)
			{
				return new EmptyResult();
			}

			var actionResult = (actionReturnValue as ActionResult) ??
			                   new ContentResult {Content = Convert.ToString(actionReturnValue, CultureInfo.InvariantCulture)};
			return actionResult;
		}

		protected virtual ControllerDescriptor GetControllerDescriptor(ControllerContext controllerContext)
		{
			var controllerType = controllerContext.Controller.GetType();
			var controllerDescriptor = DescriptorCache.GetDescriptor(controllerType);
			return controllerDescriptor;
		}

		protected virtual ActionDescriptor FindAction(ControllerContext controllerContext,
		                                              ControllerDescriptor controllerDescriptor, string actionName)
		{
			var actionDescriptor = controllerDescriptor.FindAction(controllerContext, actionName);
			return actionDescriptor;
		}

		protected virtual FilterInfo GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
		{
			var filters = actionDescriptor.GetFilters();

			// if the current controller implements one of the filter interfaces, it should be added to the list at position 0
			var controller = controllerContext.Controller;
			AddControllerToFilterList(controller, filters.ActionFilters);
			AddControllerToFilterList(controller, filters.ResultFilters);
			AddControllerToFilterList(controller, filters.AuthorizationFilters);
			AddControllerToFilterList(controller, filters.ExceptionFilters);

			return filters;
		}

		IModelBinder GetModelBinder(ParameterDescriptor parameterDescriptor)
		{
			// look on the parameter itself, then look in the global table
			return parameterDescriptor.BindingInfo.Binder ?? Binders.GetBinder(parameterDescriptor.ParameterType);
		}

		protected virtual object GetParameterValue(ControllerContext controllerContext,
		                                           ParameterDescriptor parameterDescriptor)
		{
			// collect all of the necessary binding properties
			var parameterType = parameterDescriptor.ParameterType;
			var binder = GetModelBinder(parameterDescriptor);
			var valueProvider = controllerContext.Controller.ValueProvider;
			var parameterName = parameterDescriptor.BindingInfo.Prefix ?? parameterDescriptor.ParameterName;
			var propertyFilter = GetPropertyFilter(parameterDescriptor);

			// finally, call into the binder
			var bindingContext = new ModelBindingContext
			{
				FallbackToEmptyPrefix = (parameterDescriptor.BindingInfo.Prefix == null),
				// only fall back if prefix not specified
				ModelName = parameterName,
				ModelState = controllerContext.Controller.ViewData.ModelState,
				ModelType = parameterType,
				PropertyFilter = propertyFilter,
				ValueProvider = valueProvider
			};
			var result = binder.BindModel(controllerContext, bindingContext);
			return result;
		}

		protected virtual IDictionary<string, object> GetParameterValues(ControllerContext controllerContext,
		                                                                 ActionDescriptor actionDescriptor)
		{
			var parametersDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			var parameterDescriptors = actionDescriptor.GetParameters();

			foreach (var parameterDescriptor in parameterDescriptors)
			{
				parametersDict[parameterDescriptor.ParameterName] = GetParameterValue(controllerContext, parameterDescriptor);
			}
			return parametersDict;
		}

		static Predicate<string> GetPropertyFilter(ParameterDescriptor parameterDescriptor)
		{
			var bindingInfo = parameterDescriptor.BindingInfo;
			return
				propertyName =>
				BindAttribute.IsPropertyAllowed(propertyName, bindingInfo.Include.ToArray(), bindingInfo.Exclude.ToArray());
		}

		public virtual bool InvokeAction(ControllerContext controllerContext, string actionName)
		{
			if (controllerContext == null)
			{
				throw new ArgumentNullException("controllerContext");
			}
			if (String.IsNullOrEmpty(actionName))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "actionName");
			}

			var controllerDescriptor = GetControllerDescriptor(controllerContext);
			var actionDescriptor = FindAction(controllerContext, controllerDescriptor, actionName);
			if (actionDescriptor != null)
			{
				var filterInfo = GetFilters(controllerContext, actionDescriptor);

				try
				{
					var authContext = InvokeAuthorizationFilters(controllerContext, filterInfo.AuthorizationFilters, actionDescriptor);
					if (authContext.Result != null)
					{
						// the auth filter signaled that we should let it short-circuit the request
						InvokeActionResult(controllerContext, authContext.Result);
					}
					else
					{
						if (controllerContext.Controller.ValidateRequest)
						{
							ValidateRequest(controllerContext.HttpContext.Request);
						}

						var parameters = GetParameterValues(controllerContext, actionDescriptor);
						var postActionContext = InvokeActionMethodWithFilters(controllerContext, filterInfo.ActionFilters,
						                                                      actionDescriptor, parameters);
						InvokeActionResultWithFilters(controllerContext, filterInfo.ResultFilters, postActionContext.Result);
					}
				}
				catch (ThreadAbortException)
				{
					// This type of exception occurs as a result of Response.Redirect(), but we special-case so that
					// the filters don't see this as an error.
					throw;
				}
				catch (Exception ex)
				{
					// something blew up, so execute the exception filters
					var exceptionContext = InvokeExceptionFilters(controllerContext, filterInfo.ExceptionFilters, ex);
					if (!exceptionContext.ExceptionHandled)
					{
						throw;
					}
					InvokeActionResult(controllerContext, exceptionContext.Result);
				}

				return true;
			}

			// notify controller that no method matched
			return false;
		}

		protected virtual ActionResult InvokeActionMethod(ControllerContext controllerContext,
		                                                  ActionDescriptor actionDescriptor,
		                                                  IDictionary<string, object> parameters)
		{
			var returnValue = actionDescriptor.Execute(controllerContext, parameters);
			var result = CreateActionResult(controllerContext, actionDescriptor, returnValue);
			return result;
		}

		internal static ActionExecutedContext InvokeActionMethodFilter(IActionFilter filter, ActionExecutingContext preContext,
		                                                               Func<ActionExecutedContext> continuation)
		{
			filter.OnActionExecuting(preContext);
			if (preContext.Result != null)
			{
				return new ActionExecutedContext(preContext, preContext.ActionDescriptor, true /* canceled */, null /* exception */)
				{
					Result = preContext.Result
				};
			}

			var wasError = false;
			ActionExecutedContext postContext;
			try
			{
				postContext = continuation();
			}
			catch (ThreadAbortException)
			{
				// This type of exception occurs as a result of Response.Redirect(), but we special-case so that
				// the filters don't see this as an error.
				postContext = new ActionExecutedContext(preContext, preContext.ActionDescriptor, false /* canceled */, null
					/* exception */);
				filter.OnActionExecuted(postContext);
				throw;
			}
			catch (Exception ex)
			{
				wasError = true;
				postContext = new ActionExecutedContext(preContext, preContext.ActionDescriptor, false /* canceled */, ex);
				filter.OnActionExecuted(postContext);
				if (!postContext.ExceptionHandled)
				{
					throw;
				}
			}
			if (!wasError)
			{
				filter.OnActionExecuted(postContext);
			}
			return postContext;
		}

		protected virtual ActionExecutedContext InvokeActionMethodWithFilters(ControllerContext controllerContext,
		                                                                      IList<IActionFilter> filters,
		                                                                      ActionDescriptor actionDescriptor,
		                                                                      IDictionary<string, object> parameters)
		{
			var preContext = new ActionExecutingContext(controllerContext, actionDescriptor, parameters);
			Func<ActionExecutedContext> continuation = () =>
			                                           new ActionExecutedContext(controllerContext, actionDescriptor, false
			                                                                     /* canceled */, null /* exception */)
			                                           {
			                                           	Result =
			                                           		InvokeActionMethod(controllerContext, actionDescriptor, parameters)
			                                           };

			// need to reverse the filter list because the continuations are built up backward
			var thunk = filters.Reverse().Aggregate(continuation,
			                                        (next, filter) => () => InvokeActionMethodFilter(filter, preContext, next));
			return thunk();
		}

		protected virtual void InvokeActionResult(ControllerContext controllerContext, ActionResult actionResult)
		{
			actionResult.ExecuteResult(controllerContext);
		}

		internal static ResultExecutedContext InvokeActionResultFilter(IResultFilter filter, ResultExecutingContext preContext,
		                                                               Func<ResultExecutedContext> continuation)
		{
			filter.OnResultExecuting(preContext);
			if (preContext.Cancel)
			{
				return new ResultExecutedContext(preContext, preContext.Result, true /* canceled */, null /* exception */);
			}

			var wasError = false;
			ResultExecutedContext postContext;
			try
			{
				postContext = continuation();
			}
			catch (ThreadAbortException)
			{
				// This type of exception occurs as a result of Response.Redirect(), but we special-case so that
				// the filters don't see this as an error.
				postContext = new ResultExecutedContext(preContext, preContext.Result, false /* canceled */, null /* exception */);
				filter.OnResultExecuted(postContext);
				throw;
			}
			catch (Exception ex)
			{
				wasError = true;
				postContext = new ResultExecutedContext(preContext, preContext.Result, false /* canceled */, ex);
				filter.OnResultExecuted(postContext);
				if (!postContext.ExceptionHandled)
				{
					throw;
				}
			}
			if (!wasError)
			{
				filter.OnResultExecuted(postContext);
			}
			return postContext;
		}

		protected virtual ResultExecutedContext InvokeActionResultWithFilters(ControllerContext controllerContext,
		                                                                      IList<IResultFilter> filters,
		                                                                      ActionResult actionResult)
		{
			var preContext = new ResultExecutingContext(controllerContext, actionResult);
			Func<ResultExecutedContext> continuation = delegate
			                                           {
			                                           	InvokeActionResult(controllerContext, actionResult);
			                                           	return new ResultExecutedContext(controllerContext, actionResult, false
			                                           	                                 /* canceled */, null /* exception */);
			                                           };

			// need to reverse the filter list because the continuations are built up backward
			var thunk = filters.Reverse().Aggregate(continuation,
			                                        (next, filter) => () => InvokeActionResultFilter(filter, preContext, next));
			return thunk();
		}

		protected virtual AuthorizationContext InvokeAuthorizationFilters(ControllerContext controllerContext,
		                                                                  IList<IAuthorizationFilter> filters,
		                                                                  ActionDescriptor actionDescriptor)
		{
			var context = new AuthorizationContext(controllerContext);
			foreach (var filter in filters)
			{
				filter.OnAuthorization(context);
				// short-circuit evaluation
				if (context.Result != null)
				{
					break;
				}
			}

			return context;
		}

		protected virtual ExceptionContext InvokeExceptionFilters(ControllerContext controllerContext,
		                                                          IList<IExceptionFilter> filters, Exception exception)
		{
			var context = new ExceptionContext(controllerContext, exception);
			foreach (var filter in filters)
			{
				filter.OnException(context);
			}

			return context;
		}

		static void ValidateRequest(HttpRequestBase request)
		{
			// DevDiv 214040: Enable Request Validation by default for all controller requests
			// 
			// Note that we grab the Request's RawUrl to force it to be validated. Calling ValidateInput()
			// doesn't actually validate anything. It just sets flags indicating that on the next usage of
			// certain inputs that they should be validated. We special case RawUrl because the URL has already
			// been consumed by routing and thus might contain dangerous data. By forcing the RawUrl to be
			// re-read we're making sure that it gets validated by ASP.NET.

			request.ValidateInput();
#pragma warning disable 168
			var rawUrl = request.RawUrl;
#pragma warning restore 168
		}
	}
}