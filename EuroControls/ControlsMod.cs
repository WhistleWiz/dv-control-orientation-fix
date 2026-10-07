using DV.CabControls.Spec;
using DV.ThingTypes;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace EuroControls
{
	public static class ControlsMod
	{
		public const string Guid = "wiz.eurocontrols";

		public static UnityModManager.ModEntry Instance { get; private set; } = null!;

		private static bool Load(UnityModManager.ModEntry modEntry)
		{
			Instance = modEntry;

			ProcessLoco("LocoDE2");
			ProcessLoco("LocoDH4");
			ProcessLoco("LocoDM3");
			ProcessLoco("LocoDM1U");

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

		private static void ProcessLoco(string id)
		{

			if (DV.Globals.G.Types.TryGetLivery(id, out var loco))
			{
				Error($"Loco with ID {id} not found!");
				return;
			}

			foreach (var prefab in GetPrefabs(loco))
			{
				foreach (var control in GetRelevantControls(prefab))
				{
					FlipControl(control);
					Log($"Flipped {control.name} of loco {id}");
				}
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

		private static IEnumerable<Lever> GetRelevantControls(GameObject go)
		{
			foreach (var item in go.GetComponentsInChildren<Lever>())
			{
				switch (item.name)
				{
					case "C_Throttle":
					case "C_BrakeTrain":
					case "C_BrakeIndependent":
					case "C_BrakeDynamic":
						yield return item;
						continue;
					default:
						continue;
				}
			}
		}

		private static void FlipControl(Lever lever)
		{
			var min = lever.jointLimitMin;
			var max = lever.jointLimitMax;

			lever.jointLimitMin = -max;
			lever.jointLimitMax = -min;

			lever.jointAxis = -lever.jointAxis;
		}
	}
}
