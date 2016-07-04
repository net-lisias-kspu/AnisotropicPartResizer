//   ResizerConfig.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AT_Utils
{
	public class TechTreeResizeInfo : PartModule
	{
		[KSPField] public string TechGroupID = "";
		[KSPField] public float minSize = -1;
		[KSPField] public float maxSize = -1;
		[KSPField] public float minAspect = -1;
		[KSPField] public float maxAspect = -1;
	}

	[KSPScenario(ScenarioCreationOptions.AddToAllGames, 
	                 new []
	{
		GameScenes.SPACECENTER,
		GameScenes.FLIGHT,
		GameScenes.EDITOR
	})]
	public class ResizerConfig : ScenarioModule
	{
		static ListDict<string, AvailablePart> TechTreeParts = new ListDict<string, AvailablePart>();
		public static Dictionary<string, ResizerLimits> Limits = new Dictionary<string, ResizerLimits>();
		public static bool IsCareer { get; private set; }

		static void process_part(AvailablePart p)
		{
			TechTreeParts.Clear();
			Utils.Log("Checking '{}'; has {} modules", p.name, p.partPrefab.Modules.Count);//debug
			foreach(var m in p.partPrefab.Modules)
			{
				Utils.Log("Module {}", m.GetType().Name);//debug
				var tt = m as TechTreeResizeInfo;
				if(tt != null)
				{
					if(!string.IsNullOrEmpty(tt.TechGroupID))
					{   //using only the first TechTreeInfo
						Utils.Log("Found valid Info {}", tt.TechGroupID);//debug
						TechTreeParts.Add(tt.TechGroupID, p);
						break;
					}
					else
						Utils.Log("{}.{} does not provide GroupID field. Ignoring it.", 
						          p.name, typeof(TechTreeResizeInfo).Name);
				}
			}
			Utils.Log("====================================================");//debug
		}

		static void load_parts()
		{ PartLoader.LoadedPartsList.ForEach(process_part); }

		static void update_limits()
		{
			foreach(var parts in TechTreeParts)
				Limits[parts.Key] = new ResizerLimits(parts.Value);
		}

		public override void OnAwake()
		{
			base.OnAwake();
			load_parts();
			Utils.Log("ResizerConfig.OnAwake: Parts {}, Limits {}", TechTreeParts.Count, Limits.Count);//debug
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);
			IsCareer = HighLogic.CurrentGame != null && 
				HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
			Limits.Clear();
			if(!IsCareer) return;
			update_limits();
			Utils.Log("ResizerConfig.OnLoad: Parts {}, Limits {}", TechTreeParts.Count, Limits.Count);//debug
		}

		public static ResizerLimits GetLimits(string TechGroupID)
		{
			ResizerLimits limits;
			return Limits.TryGetValue(TechGroupID, out limits)? limits : null;
		}
	}


	public class ResizerLimits
	{
		public class UpdatableFloat
		{
			public readonly Func<float, float, bool> Compare;
			public float Value { get; private set; } = -1;

			public UpdatableFloat(Func<float, float, bool> comparer)
			{ Compare = comparer; }

			public void Update(float value)
			{
				if(value < 0) return;
				if(Value < 0 || Compare(Value, value))
					Value = value;
			}

			public static implicit operator float(UpdatableFloat f) { return f.Value; }
		}
		public class UpdatableMin : UpdatableFloat
		{ public UpdatableMin() : base((a, b) => a > b) {} }
		public class UpdatableMax : UpdatableFloat
		{ public UpdatableMax() : base((a, b) => a < b) {} }

		public UpdatableMin minSize   = new UpdatableMin();
		public UpdatableMax maxSize   = new UpdatableMax();
		public UpdatableMin minAspect = new UpdatableMin();
		public UpdatableMax maxAspect = new UpdatableMax();

		void update_limits(AvailablePart part)
		{
			if(!Utils.PartIsPurchased(part.name)) return;
			var info = part.partPrefab.GetModule<TechTreeResizeInfo>();
			if(info == null) return;
			minAspect.Update(info.minAspect);
			maxAspect.Update(info.maxAspect);
			minSize.Update(info.minSize);
			maxSize.Update(info.maxSize);
		}

		public ResizerLimits(List<AvailablePart> parts)
		{ parts.ForEach(update_limits); }
	}
}

