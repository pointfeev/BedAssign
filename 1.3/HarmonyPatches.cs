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
            Harmony harmony = new Harmony("pointfeev.bedassign");
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(JobGiver_GetRest), "TryGiveJob"),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), "JobPrefix")
            );
            _ = harmony.Patch(
                original: AccessTools.Method(typeof(Building_Bed), "GetGizmos"),
                postfix: new HarmonyMethod(typeof(HarmonyPatches), "GizmoPostfix")
            );
        }

        public static bool JobPrefix(Pawn pawn)
        {
            try
            {
                pawn.LookForBedReassignment();
            }
            catch (Exception e)
            {
                BedAssign.Error("LookForBedReassignment experienced an exception: " + e.Message + "\n" + e.StackTrace);
            }
            return true;
        }

        public static void GizmoPostfix(ref IEnumerable<Gizmo> __result, Building_Bed __instance)
        {
            try
            {
                __result = __instance.AddModGizmos(__result);
            }
            catch (Exception e)
            {
                BedAssign.Error("CreateBedGizmos experienced an exception: " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}