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

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc.Resources;
using System.Web.Routing;

namespace System.Web.Mvc.Html
{
	public static class SelectExtensions
	{
		public static string DropDownList(this HtmlHelper htmlHelper, string name, string optionLabel)
		{
			return SelectInternal(htmlHelper, optionLabel, name, null /* selectList */, false /* allowMultiple */, null
				/* htmlAttributes */);
		}

		public static string DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList,
		                                  string optionLabel)
		{
			return DropDownList(htmlHelper, name, selectList, optionLabel, null);
		}

		public static string DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList,
		                                  string optionLabel, object htmlAttributes)
		{
			return DropDownList(htmlHelper, name, selectList, optionLabel, new RouteValueDictionary(htmlAttributes));
		}

		public static string DropDownList(this HtmlHelper htmlHelper, string name)
		{
			return SelectInternal(htmlHelper, null /* optionLabel */, name, null /* selectList */, false /* allowMultiple */,
			                      null /* htmlAttributes */);
		}

		public static string DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList)
		{
			return DropDownList(htmlHelper, name, selectList, (object)null /* htmlAttributes */);
		}

		public static string DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList,
		                                  object htmlAttributes)
		{
			return DropDownList(htmlHelper, name, selectList, new RouteValueDictionary(htmlAttributes));
		}

		public static string DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList,
		                                  IDictionary<string, object> htmlAttributes)
		{
			return SelectInternal(htmlHelper, null /* optionLabel */, name, selectList, false /* allowMultiple */, htmlAttributes);
		}

		public static string DropDownList(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList,
		                                  string optionLabel, IDictionary<string, object> htmlAttributes)
		{
			return SelectInternal(htmlHelper, optionLabel, name, selectList, false /* allowMultiple */, htmlAttributes);
		}

		public static string ListBox(this HtmlHelper htmlHelper, string name)
		{
			return SelectInternal(htmlHelper, null /* optionLabel */, name, null /* selectList */, true /* allowMultiple */, null
				/* htmlAttributes */);
		}

		public static string ListBox(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList)
		{
			return ListBox(htmlHelper, name, selectList, null);
		}

		public static string ListBox(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList,
		                             object htmlAttributes)
		{
			return ListBox(htmlHelper, name, selectList, new RouteValueDictionary(htmlAttributes));
		}

		public static string ListBox(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList,
		                             IDictionary<string, object> htmlAttributes)
		{
			return SelectInternal(htmlHelper, null /* optionLabel */, name, selectList, true /* allowMultiple */, htmlAttributes);
		}

		static IEnumerable<SelectListItem> GetSelectData(this HtmlHelper htmlHelper, string name)
		{
			object o = null;
			if (htmlHelper.ViewData != null)
			{
				o = htmlHelper.ViewData.Eval(name);
			}
			if (o == null)
			{
				throw new InvalidOperationException(
					String.Format(
						CultureInfo.CurrentUICulture,
						MvcResources.HtmlHelper_MissingSelectData,
						name,
						"IEnumerable<SelectListItem>"));
			}
			var selectList = o as IEnumerable<SelectListItem>;
			if (selectList == null)
			{
				throw new InvalidOperationException(
					String.Format(
						CultureInfo.CurrentUICulture,
						MvcResources.HtmlHelper_WrongSelectDataType,
						name,
						o.GetType().FullName,
						"IEnumerable<SelectListItem>"));
			}
			return selectList;
		}

		static string ListItemToOption(SelectListItem item)
		{
			var builder = new TagBuilder("option")
			{
				InnerHtml = HttpUtility.HtmlEncode(item.Text)
			};
			if (item.Value != null)
			{
				builder.Attributes["value"] = item.Value;
			}
			if (item.Selected)
			{
				builder.Attributes["selected"] = "selected";
			}
			return builder.ToString(TagRenderMode.Normal);
		}

		static string SelectInternal(this HtmlHelper htmlHelper, string optionLabel, string name,
		                             IEnumerable<SelectListItem> selectList, bool allowMultiple,
		                             IDictionary<string, object> htmlAttributes)
		{
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentException(MvcResources.Common_NullOrEmpty, "name");
			}

			var usedViewData = false;

			// If we got a null selectList, try to use ViewData to get the list of items.
			if (selectList == null)
			{
				selectList = htmlHelper.GetSelectData(name);
				usedViewData = true;
			}

			var defaultValue = (allowMultiple)
			                   	? htmlHelper.GetModelStateValue(name, typeof(string[]))
			                   	: htmlHelper.GetModelStateValue(name, typeof(string));

			// If we haven't already used ViewData to get the entire list of items then we need to
			// use the ViewData-supplied value before using the parameter-supplied value.
			if (!usedViewData)
			{
				if (defaultValue == null)
				{
					defaultValue = htmlHelper.ViewData.Eval(name);
				}
			}

			if (defaultValue != null)
			{
				var defaultValues = (allowMultiple) ? defaultValue as IEnumerable : new[] {defaultValue};
				var values = from object value in defaultValues
				             select Convert.ToString(value, CultureInfo.CurrentCulture);
				var selectedValues = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
				var newSelectList = new List<SelectListItem>();

				foreach (var item in selectList)
				{
					item.Selected = (item.Value != null) ? selectedValues.Contains(item.Value) : selectedValues.Contains(item.Text);
					newSelectList.Add(item);
				}
				selectList = newSelectList;
			}

			// Convert each ListItem to an <option> tag
			var listItemBuilder = new StringBuilder();

			// Make optionLabel the first item that gets rendered.
			if (optionLabel != null)
			{
				listItemBuilder.AppendLine(
					ListItemToOption(new SelectListItem {Text = optionLabel, Value = String.Empty, Selected = false}));
			}

			foreach (var item in selectList)
			{
				listItemBuilder.AppendLine(ListItemToOption(item));
			}

			var tagBuilder = new TagBuilder("select")
			{
				InnerHtml = listItemBuilder.ToString()
			};
			tagBuilder.MergeAttributes(htmlAttributes);
			tagBuilder.MergeAttribute("name", name);
			tagBuilder.GenerateId(name);
			if (allowMultiple)
			{
				tagBuilder.MergeAttribute("multiple", "multiple");
			}

			// If there are any errors for a named field, we add the css attribute.
			ModelState modelState;
			if (htmlHelper.ViewData.ModelState.TryGetValue(name, out modelState))
			{
				if (modelState.Errors.Count > 0)
				{
					tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
				}
			}

			return tagBuilder.ToString(TagRenderMode.Normal);
		}
	}
}