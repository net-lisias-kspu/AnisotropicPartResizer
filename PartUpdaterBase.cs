//   PartUpdaterBase.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using System;
using System.Collections.Generic;
using UnityEngine;
using AT_Utils;

namespace AT_Utils
{
	public abstract class PartUpdaterBase : PartModule
	{
		protected Part base_part;

		public static Vector3 ScaleVector(Vector3 v, float s, float l)
		{ return Vector3.Scale(v, new Vector3(s, s*l, s)); }

		public virtual void Init() 
		{ base_part = PartLoader.getPartInfoByName(part.partInfo.name).partPrefab; }

		protected abstract void SaveDefaults();
	}

	public abstract class PartUpdater : PartUpdaterBase
	{
		public uint priority = 0; // 0 is highest

		protected override void SaveDefaults() {}
		public virtual void OnRescale(Scale scale) {}

		#region ModuleUpdaters
		public readonly static Dictionary<string, Func<Part, PartUpdater>> UpdatersTypes = new Dictionary<string, Func<Part, PartUpdater>>();

		static Func<Part, PartUpdater> updaterConstructor<UpdaterType>() where UpdaterType : PartUpdater
		{ return part => part.GetModule<UpdaterType>() ?? part.AddModule(typeof(UpdaterType).Name) as UpdaterType; }

		public static void RegisterUpdater<UpdaterType>() 
			where UpdaterType : PartUpdater
		{ 
			string updater_name = typeof(UpdaterType).FullName;
			if(UpdatersTypes.ContainsKey(updater_name)) return;
			Utils.Log("PartUpdater: registering {0}", updater_name);
			UpdatersTypes[updater_name] = updaterConstructor<UpdaterType>();
		}
		#endregion
	}

	public abstract class ModuleUpdater<T> : PartUpdater where T : PartModule
	{
		protected struct ModulePair<M>
		{
			public M base_module;
			public M module;
			public Dictionary<string, object> orig_data;

			public ModulePair(M base_module, M module)
			{
				this.module = module;
				this.base_module = base_module;
				orig_data = new Dictionary<string, object>();
			}
		}

		protected readonly List<ModulePair<T>> modules = new List<ModulePair<T>>();

		public override void Init() 
		{
			base.Init();
			priority = 100; 
			var m = part.Modules.GetEnumerator();
			var b = base_part.Modules.GetEnumerator();
			while(b.MoveNext() && m.MoveNext())
			{
				if(b.Current is T && m.Current is T)
					modules.Add(new ModulePair<T>(b.Current as T, m.Current as T));
			}
			if(modules.Count == 0) 
				throw new MissingComponentException(string.Format("[Hangar] ModuleUpdater: part {0} does not have {1} module", part.name, typeof(T).Name));
			SaveDefaults();
		}

		protected abstract void on_rescale(ModulePair<T> mp, Scale scale);

		public override void OnRescale(Scale scale) 
		{ modules.ForEach(mp => on_rescale(mp, scale)); }
	}
}

