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

using System.Reflection;

namespace System.Web.Mvc
{
	internal sealed class ActionMethodDispatcherCache : ReaderWriterCache<MethodInfo, ActionMethodDispatcher>
	{
		public ActionMethodDispatcher GetDispatcher(MethodInfo methodInfo)
		{
			return FetchOrCreateItem(methodInfo, () => new ActionMethodDispatcher(methodInfo));
		}
	}
}