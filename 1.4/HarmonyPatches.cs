using System;
using System.Collections.Generic;
using BedAssign.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BedAssign
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("pointfeev.bedassign");
            _ = harmony.Patch(AccessTools.Method(typeof(JobGiver_GetRest), "TryGiveJob"),
                              new HarmonyMethod(typeof(HarmonyPatches), nameof(JobPrefix)));
            _ = harmony.Patch(AccessTools.Method(typeof(Building_Bed), nameof(Building_Bed.GetGizmos)),
                              postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(GizmoPostfix)));
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
                __instance.AddModGizmos(ref __result);
            }
            catch (Exception e)
            {
                BedAssign.Error("CreateBedGizmos experienced an exception: " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}