//   UpdaterRegistrator.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AnisotropicPartResizer
{
	#region From TweakScale (reworked)
	public abstract class UpdaterRegistrator : MonoBehaviour
	//Can't understand what it is needed for =^_^=
	//Probably a workaround of some sort.
	{
		static bool loadedInScene = false;

		public void Start()
		{
			if(loadedInScene)
			{
				Destroy(gameObject);
				return;
			}
			loadedInScene = true;
			OnStart();
		}
		public abstract void OnStart();

		public void Update()
		{
			loadedInScene = false;
			Destroy(gameObject);

		}
	}

	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	class PartUpdatereRegister : UpdaterRegistrator
	{
		/// <summary>
		/// Gets all types defined in all loaded assemblies.
		/// </summary>
		static IEnumerable<Type> get_all_types()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] types;
				try	{ types = assembly.GetTypes(); }
				catch(Exception) { types = Type.EmptyTypes; }
				foreach(var type in types) yield return type;
			}
		}

		//Register all found PartUpdaters
		override public void OnStart()
		{
			var all_updaters = get_all_types().Where(IsPartUpdater).ToArray();
			foreach (var updater in all_updaters)
			{
				MethodInfo register = typeof(PartUpdater).GetMethod("RegisterUpdater");
				register = register.MakeGenericMethod(new [] { updater });
				register.Invoke(null, null);
			}
		}

		static bool IsPartUpdater(Type t)
		{ return !t.IsGenericType && t.IsSubclassOf(typeof(PartUpdater)); }
	}
	#endregion
}

