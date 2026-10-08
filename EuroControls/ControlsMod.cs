using DV.CabControls.Spec;
using DV.HUD;
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
		public const string Guid = "wiz.eurocontrols";

		private static readonly string[] s_defaultControls = new[]
		{
			"C_Throttle",
			"C_BrakeTrain",
			"C_BrakeIndependent",
			"C_BrakeDynamic"
		};
		private static readonly List<ControlType> s_defaultReverse = new List<ControlType>
		{
			ControlType.TrainBrake,
			ControlType.IndBrake,
			ControlType.DynamicBrake
		};

		public static UnityModManager.ModEntry Instance { get; private set; } = null!;

		private static bool Load(UnityModManager.ModEntry modEntry)
		{
			Instance = modEntry;

			ProcessLoco("LocoDE2", s_defaultControls, s_defaultReverse);
			ProcessLoco("LocoDH4", s_defaultControls, s_defaultReverse);
			ProcessLoco("LocoDM3", s_defaultControls, s_defaultReverse);
			ProcessLoco("LocoDM1U", new[] { "C_TrainBrake", }, new List<ControlType> { ControlType.TrainBrake });

			return true;
		}

		public static void Log(string message)
		{
			Instance.Logger.Log(message);
		}

		public static void Warning(string message)
		{
			Instance.Logger.Warning(message);
		}

		public static void Error(string message)
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
					icm.reverseDirectionList = reverseList;
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
				$"Prefabs: {string.Join(", ", hashPrefabs)}\n" +
				$"Controls: {string.Join(", ", hashControls)}");
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
		}
	}
}
