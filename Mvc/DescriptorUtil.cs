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

using System.Linq;
using System.Threading;

namespace System.Web.Mvc
{
	internal static class DescriptorUtil
	{
		public static TDescriptor[] LazilyFetchOrCreateDescriptors<TReflection, TDescriptor>(ref TDescriptor[] cacheLocation,
		                                                                                     Func<TReflection[]> initializer,
		                                                                                     Func<TReflection, TDescriptor>
		                                                                                     	converter)
		{
			// did we already calculate this once?
			var existingCache = Interlocked.CompareExchange(ref cacheLocation, null, null);
			if (existingCache != null)
			{
				return existingCache;
			}

			var memberInfos = initializer();
			var descriptors = memberInfos.Select(converter).Where(descriptor => descriptor != null).ToArray();
			var updatedCache = Interlocked.CompareExchange(ref cacheLocation, descriptors, null);
			return updatedCache ?? descriptors;
		}
	}
}