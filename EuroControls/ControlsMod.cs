using DV.CabControls.Spec;
using DV.HUD;
using DV.KeyboardInput;
using DV.ThingTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

using static DV.HUD.InteriorControlsManager;

namespace EuroControls
{
	public static class ControlsMod
	{
		private static readonly string[] s_defaultControls1 = new[]
		{
			"C_Throttle",
			"C_BrakeTrain",
			"C_BrakeIndependent",
			"C_BrakeDynamic"
		};
		private static readonly string[] s_defaultControls2 = new[]
		{
			"C_TrainBrake",
			"C_IndependentBrake"
		};
		private static readonly string[] s_defaultControls3 = new[]
		{
			"C_Regulator",
			"C_TrainBrake",
			"C_IndependentBrake"
		};
		private static readonly List<ControlType> s_defaultReverse = new List<ControlType>
		{
			ControlType.TrainBrake,
			ControlType.IndBrake,
			ControlType.DynamicBrake,
			ControlType.CoalDump,
			ControlType.CylCock,
			ControlType.Damper
	};

		private static UnityModManager.ModEntry Instance = null!;

		private static bool Load(UnityModManager.ModEntry modEntry)
		{
			Instance = modEntry;

			ProcessLoco("LocoDE2", s_defaultControls1, s_defaultReverse);
			ProcessLoco("LocoDH4", s_defaultControls1, s_defaultReverse);
			ProcessLoco("LocoDM3", s_defaultControls1, s_defaultReverse);
			ProcessLoco("LocoDM1U", s_defaultControls2, s_defaultReverse);
			ProcessLoco("LocoS060", s_defaultControls2, s_defaultReverse);
			ProcessLoco("LocoS282A", s_defaultControls3, s_defaultReverse);
			S282RegulatorFix();

			return true;
		}

		private static void Log(string message)
		{
			Instance.Logger.Log(message);
		}

		private static void Warning(string message)
		{
			Instance.Logger.Warning(message);
		}

		private static void Error(string message)
		{
			Instance.Logger.Error(message);
		}

		private static void ProcessLoco(string id, string[] controls, List<ControlType> reverseList)
		{
			if (!DV.Globals.G.Types.TryGetLivery(id, out var loco))
			{
				Error($"Could not find livery {id}");
				return;
			}

			var hashPrefabs = new HashSet<string>();
			var hashControls = new HashSet<string>();

			foreach (var prefab in GetPrefabs(loco))
			{
				hashPrefabs.Add(prefab.name);

				if (prefab.TryGetComponent(out InteriorControlsManager icm))
				{
					// Make a copy.
					icm.reverseDirectionList = reverseList.ToList();
				}

				foreach (var lever in prefab.GetComponentsInChildren<Lever>())
				{
					if (controls.Contains(lever.name))
					{
						FlipControl(lever);
						hashControls.Add(lever.name);
					}
				}
			}

			Log($"Processing {id}:\n" +
				$"    Prefabs: {string.Join(", ", hashPrefabs)}\n" +
				$"    Controls: {string.Join(", ", hashControls)}");

			if (hashControls.Count == 0)
			{
				Warning("Processing did not change any control, something may have changed");
			}
		}

		private static IEnumerable<GameObject> GetPrefabs(TrainCarLivery livery)
		{
			if (livery.interiorPrefab != null)
			{
				yield return livery.interiorPrefab;
			}

			if (livery.explodedInteriorPrefab != null)
			{
				yield return livery.explodedInteriorPrefab;
			}
		}

		private static void FlipControl(Lever lever)
		{
			var min = lever.jointLimitMin;
			var max = lever.jointLimitMax;
			lever.jointLimitMin = -max;
			lever.jointLimitMax = -min;

			lever.jointAxis = -lever.jointAxis;
			lever.scrollWheelHoverScroll = -lever.scrollWheelHoverScroll;

			// All of these controls use this keyboard input so it should
			// cover everything.
			// Absolute version is not changed, only incremental.
			if (lever.TryGetComponent(out MouseScrollKeyboardInput input))
			{
				input.scrollAction.flip = !input.scrollAction.flip;
			}
		}

		// What are standards without exceptions?
		private static void S282RegulatorFix()
		{
			if (!DV.Globals.G.Types.TryGetLivery("LocoS282A", out var loco))
			{
				Error("Could not find livery LocoS282A for fix");
				return;
			}

			foreach (var prefab in GetPrefabs(loco))
			{
				var t = prefab.transform.Find("Center/Regulator/C_Regulator");

				if (t == null)
				{
					Error("Failed to get regulator for S282!");
					continue;
				}

				t.gameObject.AddComponent<HJAFThingyFlipper>();
			}
		}

		// So we can invert the lever without inverting the animation.
		private class HJAFThingyFlipper : MonoBehaviour
		{
			private System.Collections.IEnumerator Start()
			{
				yield return null;
				yield return null;
				yield return null;
				yield return null;

				if (TryGetComponent(out HingeJointAngleFix hjaf))
				{
					hjaf.invertPercentage = !hjaf.invertPercentage;
				}
				else
				{
					Warning("HJAFThingyFlipper did not find the HJAF!");
				}

				Destroy(this);
			}
		}
	}
}
