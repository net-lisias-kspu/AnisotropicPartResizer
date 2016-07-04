//   AnisotropicResizable.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2016 Allis Tauri

using UnityEngine;
using AT_Utils;

namespace AT_Utils
{
	public class AnisotropicResizableBase : PartUpdaterBase, IPartCostModifier
	{
		[KSPField(isPersistant=true, guiActiveEditor=true, guiName="Aspect", guiFormat="S4")]
		[UI_FloatEdit(scene=UI_Scene.Editor, minValue=0.5f, maxValue=10, incrementLarge=1.0f, incrementSmall=0.1f, incrementSlide=0.001f, sigFigs = 4)]
		public float aspect = 1.0f;

		[KSPField(isPersistant=false, guiActiveEditor=true, guiName="Mass")] 
		public string MassDisplay;

		//module config
		[KSPField] public string TechGroupID = "";

		[KSPField] public float minSize = -1;
		[KSPField] public float maxSize = -1;
		[KSPField] public float sizeStepLarge = 1.0f;
		[KSPField] public float sizeStepSmall = 0.1f;

		[KSPField] public float minAspect = -1;
		[KSPField] public float maxAspect = -1;
		[KSPField] public float aspectStepLarge = 0.5f;
		[KSPField] public float aspectStepSmall = 0.1f;

		protected float old_aspect  = -1;
		[KSPField(isPersistant=true)] public float orig_aspect = -1;

		protected Transform model { get { return part.transform.GetChild(0); } }
		public    float delta_cost;
		protected bool  just_loaded = true;

		#region TechTree
		protected void init_limit(ResizerLimits.UpdatableFloat tech_limit, ref float limit, float current_value)
		{
			float val = tech_limit.Value;
			if(tech_limit.Compare(current_value, val)) val = current_value;
			if(limit < 0 || tech_limit.Compare(limit, val)) limit = val;
		}

		protected static void setup_field(BaseField field, float minval, float maxval, float l_increment, float s_increment)
		{
			var fe = field.uiControlEditor as UI_FloatEdit;
			if(fe != null) 
			{ 
				fe.minValue = minval;
				fe.maxValue = maxval;
				fe.incrementLarge = l_increment;
				fe.incrementSmall = s_increment;
			}
		}
		#endregion

		protected const float eps = 1e-5f;
		protected static bool unequal(float f1, float f2)
		{ return Mathf.Abs(f1-f2) > eps; }

		public void UpdateGUI(ShipConstruct ship)
		{ MassDisplay = Utils.formatMass(part.TotalMass()); }

		public override void OnAwake()
		{
			base.OnAwake();
			GameEvents.onEditorShipModified.Add(UpdateGUI);
		}
		void OnDestroy() { GameEvents.onEditorShipModified.Remove(UpdateGUI); }

		protected override void SaveDefaults()
		{
			if(orig_aspect < 0 || HighLogic.LoadedSceneIsEditor)
			{
				var resizer = base_part.GetModule<AnisotropicResizableBase>();
				orig_aspect = resizer != null ? resizer.aspect : aspect;
			}
			old_aspect = aspect;
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);
			Init(); 
			SaveDefaults();
			if(state == StartState.Editor) 
			{
				var limits = ResizerConfig.GetLimits(TechGroupID);
				if(limits != null)
				{
					init_limit(limits.minAspect, ref minAspect, Mathf.Min(aspect, orig_aspect));
					init_limit(limits.maxAspect, ref maxAspect, Mathf.Max(aspect, orig_aspect));
				}
			}
			just_loaded = true;
		}

		public float GetModuleCost(float default_cost, ModifierStagingSituation situation) { return delta_cost; }
		public ModifierChangeWhen GetModuleCostChangeWhen() { return ModifierChangeWhen.CONSTANTLY; }
	}
}

