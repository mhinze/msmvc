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

namespace System.Web.Mvc
{
	internal static class TypeHelpers
	{
		public static bool TypeAllowsNullValue(Type type)
		{
			// reference types allow null values
			if (!type.IsValueType)
			{
				return true;
			}

			// nullable value types allow null values
			// code lifted from System.Nullable.GetUnderlyingType()
			if (type.IsGenericType && !type.IsGenericTypeDefinition && (type.GetGenericTypeDefinition() == typeof(Nullable<>)))
			{
				return true;
			}

			// no other types allow null values
			return false;
		}
	}
}