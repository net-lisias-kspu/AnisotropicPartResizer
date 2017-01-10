//   ResizerConfig.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using System;
using System.Collections.Generic;

namespace AT_Utils
{
	public class ResizerGlobals : PluginGlobals<ResizerGlobals>
	{
		[Persistent] public float AbsMinSize = 0.5f;
		[Persistent] public float AbsMaxSize = 10f;
		[Persistent] public float AbsMinAspect = 0.5f;
		[Persistent] public float AbsMaxAspect = 10f;
	}

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
			foreach(var m in p.partPrefab.Modules)
			{
				var tt = m as TechTreeResizeInfo;
				if(tt != null)
				{
					if(!string.IsNullOrEmpty(tt.TechGroupID))
					{   //using only the first TechTreeInfo
						TechTreeParts.Add(tt.TechGroupID, p);
						break;
					}
					else
						Utils.Log("{}.{} does not provide GroupID field. Ignoring it.", 
						          p.name, typeof(TechTreeResizeInfo).Name);
				}
			}
		}

		static void load_parts()
		{ 
			TechTreeParts.Clear();
			PartLoader.LoadedPartsList.ForEach(process_part); 
		}

		static void update_limits()
		{
			foreach(var parts in TechTreeParts)
				Limits[parts.Key] = new ResizerLimits(parts.Value);
		}

		public override void OnAwake()
		{
			base.OnAwake();
			load_parts();
			IsCareer = HighLogic.CurrentGame != null && 
				HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX;
			Limits.Clear();
			if(!IsCareer) return;
			update_limits();
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
				if(Value < 0 || Compare(value, Value))
					Value = value;
			}

			public static implicit operator float(UpdatableFloat f) { return f.Value; }
		}
		public class UpdatableMin : UpdatableFloat
		{ public UpdatableMin() : base((new_value, old_value) => new_value < old_value) {} }
		public class UpdatableMax : UpdatableFloat
		{ public UpdatableMax() : base((new_value, old_value) => new_value > old_value) {} }

		public UpdatableMin minSize   = new UpdatableMin();
		public UpdatableMax maxSize   = new UpdatableMax();
		public UpdatableMin minAspect = new UpdatableMin();
		public UpdatableMax maxAspect = new UpdatableMax();

		void update_limits(AvailablePart part)
		{
			if(!Utils.PartIsPurchased(part.name)) return;
			var info = part.partPrefab.Modules.GetModule<TechTreeResizeInfo>();
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

