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
using System.Linq;
using System.Reflection;

namespace System.Web.Mvc
{
	internal sealed class ControllerTypeCache
	{
		Dictionary<string, ILookup<string, Type>> _cache;
		readonly object _lockObj = new object();

		internal int Count
		{
			get
			{
				var count = 0;
				foreach (var lookup in _cache.Values)
				{
					foreach (var grouping in lookup)
					{
						count += grouping.Count();
					}
				}
				return count;
			}
		}

		public void EnsureInitialized(IBuildManager buildManager)
		{
			if (_cache == null)
			{
				lock (_lockObj)
				{
					if (_cache == null)
					{
						var controllerTypes = GetAllControllerTypes(buildManager);
						var groupedByName = controllerTypes.GroupBy(
							t => t.Name.Substring(0, t.Name.Length - "Controller".Length),
							StringComparer.OrdinalIgnoreCase);
						_cache = groupedByName.ToDictionary(
							g => g.Key,
							g => g.ToLookup(t => t.Namespace ?? String.Empty, StringComparer.OrdinalIgnoreCase),
							StringComparer.OrdinalIgnoreCase);
					}
				}
			}
		}

		static List<Type> GetAllControllerTypes(IBuildManager buildManager)
		{
			// Go through all assemblies referenced by the application and search for
			// controllers and controller factories.
			var controllerTypes = new List<Type>();
			var assemblies = buildManager.GetReferencedAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				Type[] typesInAsm;
				try
				{
					typesInAsm = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					typesInAsm = ex.Types;
				}
				controllerTypes.AddRange(typesInAsm.Where(IsControllerType));
			}
			return controllerTypes;
		}

		public IList<Type> GetControllerTypes(string controllerName, HashSet<string> namespaces)
		{
			var matchingTypes = new List<Type>();

			ILookup<string, Type> nsLookup;
			if (_cache.TryGetValue(controllerName, out nsLookup))
			{
				// this friendly name was located in the cache, now cycle through namespaces
				if (namespaces != null)
				{
					foreach (var ns in namespaces)
					{
						matchingTypes.AddRange(nsLookup[ns]);
					}
				}
				else
				{
					// if the namespaces parameter is null, search *every* namespace
					foreach (var nsGroup in nsLookup)
					{
						matchingTypes.AddRange(nsGroup);
					}
				}
			}

			return matchingTypes;
		}

		internal static bool IsControllerType(Type t)
		{
			return
				t != null &&
				t.IsPublic &&
				t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) &&
				!t.IsAbstract &&
				typeof(IController).IsAssignableFrom(t);
		}
	}
}