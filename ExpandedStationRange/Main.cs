using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace ExpandedStationRange;

public static class Main
{
	const float MINIMUM_SQR_RANGE = 4_000_000f;

	private static List<StationJobGenerationRange>? rangeComponents;
	private static readonly Dictionary<string, float> initialDistanceRegular = new();
	private static readonly Dictionary<string, float> initialDistanceAnyJobTaken = new();
#if RELEASE
#pragma warning disable CS0649 // It's expected that LogDebug will never be assigned in Release builds
#endif
	private static Action<string>? LogDebug;
#pragma warning restore CS0649
	private static Action? patchOnLoadingFinished;

	// Unity Mod Manage Wiki: https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
	private static bool Load(UnityModManager.ModEntry modEntry)
	{
#if DEBUG
		LogDebug = modEntry.Logger.Log;
#endif
		modEntry.OnToggle = OnToggle;
		return true;
	}

	private static bool OnToggle(UnityModManager.ModEntry modEntry, bool isTogglingOn)
	{
		LogDebug?.Invoke($"Toggle {(isTogglingOn ? "on" : "off")} requested for {modEntry.Info.DisplayName}");
		bool result = true;

		if (isTogglingOn)
		{
			patchOnLoadingFinished ??= () => DoPatch(modEntry);
			WorldStreamingInit.LoadingFinished += patchOnLoadingFinished;
			if (WorldStreamingInit.IsLoaded)
			{
				result = DoPatch(modEntry);
			}
		}
		else
		{
			if (patchOnLoadingFinished != null)
			{
				WorldStreamingInit.LoadingFinished -= patchOnLoadingFinished;
			}
			if (WorldStreamingInit.IsLoaded)
			{
				result = DoUnpatch(modEntry);
			}
		}

		return result;
	}

	private static bool DoPatch(UnityModManager.ModEntry modEntry)
	{
		LogDebug?.Invoke($"Doing patch for {modEntry.Info.DisplayName}");

		try
		{
			rangeComponents = UnityEngine.Object.FindObjectsOfType<StationJobGenerationRange>().ToList();
			LogDebug?.Invoke($"Found {rangeComponents.Count} StationJobGenerationRange components");

			foreach (var rangeComponent in rangeComponents)
			{
				string stationName = rangeComponent.gameObject.name;
				// Backup existing values before overwriting
				if (!initialDistanceRegular.ContainsKey(stationName))
				{
					initialDistanceRegular.Add(stationName, rangeComponent.destroyGeneratedJobsSqrDistanceRegular);
				}
				if (!initialDistanceAnyJobTaken.ContainsKey(stationName))
				{
					initialDistanceAnyJobTaken.Add(stationName, rangeComponent.destroyGeneratedJobsSqrDistanceAnyJobTaken);
				}

				// Only overwrite the job taken range if it's less than our minimum
				if (rangeComponent.destroyGeneratedJobsSqrDistanceAnyJobTaken < MINIMUM_SQR_RANGE)
				{
					rangeComponent.destroyGeneratedJobsSqrDistanceAnyJobTaken = MINIMUM_SQR_RANGE;
				}
				rangeComponent.destroyGeneratedJobsSqrDistanceRegular = rangeComponent.destroyGeneratedJobsSqrDistanceAnyJobTaken;
			}
		}
		catch (Exception ex)
		{
			modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
			modEntry.Enabled = false;
			rangeComponents = null;
			return false;
		}

		return true;
	}

	private static bool DoUnpatch(UnityModManager.ModEntry modEntry)
	{
		LogDebug?.Invoke($"Doing unpatch for {modEntry.Info.DisplayName}");

		if (rangeComponents == null)
		{
			// The patch operation must have not succeeded if the reangeComponents field is null.
			// Return as if it's been unpatched since it was never patched to start.
			return true;
		}

		try
		{
			foreach (var rangeComponent in rangeComponents)
			{
				string stationName = rangeComponent.gameObject.name;
				if (!initialDistanceRegular.ContainsKey(stationName))
				{
					throw new Exception($"Couldn't find a cached initial regular distance for ${stationName}");
				}
				if (!initialDistanceAnyJobTaken.ContainsKey(stationName))
				{
					throw new Exception($"Couldn't find a cached initial job taken distance for ${stationName}");
				}

				rangeComponent.destroyGeneratedJobsSqrDistanceAnyJobTaken = initialDistanceAnyJobTaken[stationName];
				rangeComponent.destroyGeneratedJobsSqrDistanceRegular = initialDistanceRegular[stationName];
			}
		}
		catch (Exception ex)
		{
			modEntry.Logger.LogException($"Failed to unload {modEntry.Info.DisplayName}:", ex);
			return false;
		}

		return true;
	}
}
