using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony12;
using UnityModManagerNet;

namespace ExpandedStationRangeMod
{
	public class Main
	{
		private static UnityModManager.ModEntry thisModEntry;
		private static float initialDistanceRegular = 0f;
		private static float initialDistanceAnyJobTaken = 0f;

		static void Load(UnityModManager.ModEntry modEntry)
		{
			var harmony = HarmonyInstance.Create(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			thisModEntry = modEntry;
		}

		[HarmonyPatch(typeof(StationJobGenerationRange))]
		[HarmonyPatchAll]
		class StationJobGenerationRange_AllMethods_Patch
		{
			static void Prefix(StationJobGenerationRange __instance, MethodBase __originalMethod)
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

				if (thisModEntry.Active)
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
}
