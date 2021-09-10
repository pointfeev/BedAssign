using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace BedAssign
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("pointfeev.bedassign");

            harmony.Patch(
                original: AccessTools.Method(typeof(JobGiver_GetRest), "TryGiveJob"),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), "JobPrefix")
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Building_Bed), "GetGizmos"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), "GizmoPostfix")
            );
        }

        public static bool JobPrefix(Pawn pawn)
        {
            try
            {
                BedAssign.CheckBeds(pawn);
            }
            catch (Exception e)
            {
                Log.Error("[BedAssign] CheckBeds experienced an exception: " + e.Message + "\n" + e.StackTrace);
            }
            return true;
        }

        public static void GizmoPostfix(ref IEnumerable<Gizmo> __result, Building_Bed __instance)
        {
            try
            {
                __result = BedGizmoUtils.CreateBedGizmos(__instance, __result);
            }
            catch (Exception e)
            {
                Log.Error("[BedAssign] CreateBedGizmos experienced an exception: " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
