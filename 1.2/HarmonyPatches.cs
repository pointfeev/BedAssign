using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

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
            BedAssign.CheckBeds(pawn);
            return true;
        }

        public static void GizmoPostfix(ref IEnumerable<Gizmo> __result, Building_Bed __instance)
        {
            __result = BedGizmoUtils.CreateBedGizmos(__instance, __result.ToList());
        }
    }
}
