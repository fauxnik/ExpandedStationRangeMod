using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace ExpandedStationRange;

public static class Main
{
#pragma warning disable CS8618 // ModEntry will be set during Load
	private static UnityModManager.ModEntry ModEntry;
#pragma warning restore CS8618
	private static Harmony? harmony;
	private static float initialDistanceRegular = 0f;
	private static float initialDistanceAnyJobTaken = 0f;

	// Unity Mod Manage Wiki: https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
	private static bool Load(UnityModManager.ModEntry modEntry)
	{
		ModEntry = modEntry;
		return DoPatch();
	}

	private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
	{
		modEntry.Enabled = value;

		if (value)
		{
			return DoPatch();
		}

		return DoUnpatch();
	}

	private static bool DoPatch()
	{
		try
		{
			harmony ??= new Harmony(ModEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
		catch (Exception ex)
		{
			ModEntry.Logger.LogException($"Failed to load {ModEntry.Info.DisplayName}:", ex);
			harmony?.UnpatchAll();
			harmony = null;
			return false;
		}

		return true;
	}

	private static bool DoUnpatch()
	{
		if (harmony == null)
		{
			// The patch operation must have not succeeded if the harmony field is null.
			// Return as if it's been unpatched since it was never patched to start.
			return true;
		}

		try
		{
			harmony.UnpatchAll();
		}
		catch (Exception ex)
		{
			ModEntry.Logger.LogException($"Failed to unload {ModEntry.Info.DisplayName}:", ex);
			return false;
		}

		return true;
	}

	[HarmonyPatch(typeof(StationJobGenerationRange))]
	[HarmonyPatchAll]
	class StationJobGenerationRange_AllMethods_Patch
	{
		static void Prefix(StationJobGenerationRange __instance)
		{
			// backup existing values before overwriting
			if (initialDistanceRegular < 1f)
			{
				initialDistanceRegular = __instance.destroyGeneratedJobsSqrDistanceRegular;
			}
			if (initialDistanceAnyJobTaken < 1f)
			{
				initialDistanceAnyJobTaken = __instance.destroyGeneratedJobsSqrDistanceAnyJobTaken;
			}

			if (ModEntry.Active)
			{
				if (__instance.destroyGeneratedJobsSqrDistanceAnyJobTaken < 4000000f)
				{
					__instance.destroyGeneratedJobsSqrDistanceAnyJobTaken = 4000000f;
				}
				__instance.destroyGeneratedJobsSqrDistanceRegular =
					__instance.destroyGeneratedJobsSqrDistanceAnyJobTaken;
			}
			else
			{
				__instance.destroyGeneratedJobsSqrDistanceRegular = initialDistanceRegular;
				__instance.destroyGeneratedJobsSqrDistanceAnyJobTaken = initialDistanceAnyJobTaken;
			}
		}
	}
}
