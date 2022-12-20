using System.Collections.Generic;
using System.Linq;
using BedAssign.Gizmos;
using RimWorld;
using Verse;

namespace BedAssign.Utilities
{
    public static class GizmoUtils
    {
        private static readonly Dictionary<int, Gizmo_UnusableBed> UnusableBedGizmos
            = new Dictionary<int, Gizmo_UnusableBed>();

        private static readonly Dictionary<int, Gizmo_ForceAssignment> ForceAssignmentGizmos
            = new Dictionary<int, Gizmo_ForceAssignment>();

        private static Gizmo_UnusableBed Gizmo_UnusableBed(this Building_Bed bed)
        {
            if (UnusableBedGizmos.TryGetValue(bed.thingIDNumber, out Gizmo_UnusableBed gizmo))
                return gizmo;
            gizmo = new Gizmo_UnusableBed(bed);
            UnusableBedGizmos.SetOrAdd(bed.thingIDNumber, gizmo);
            return gizmo;
        }

        private static Gizmo_ForceAssignment Gizmo_ForceAssignment(this Pawn pawn, Building_Bed bed)
        {
            if (ForceAssignmentGizmos.TryGetValue(pawn.thingIDNumber, out Gizmo_ForceAssignment gizmo))
            {
                gizmo.Bed = bed;
                return gizmo;
            }
            gizmo = new Gizmo_ForceAssignment(pawn, bed);
            ForceAssignmentGizmos.SetOrAdd(pawn.thingIDNumber, gizmo);
            return gizmo;
        }

        public static void AddModGizmos(this Building_Bed bed, ref IEnumerable<Gizmo> gizmos)
        {
            if (!bed.CanBeUsedEver())
                return;
            gizmos = gizmos.Append(bed.Gizmo_UnusableBed());
            if (!bed.CanBeUsed())
                return;
            List<Pawn> forcedPawns = new List<Pawn>();
            foreach (KeyValuePair<Pawn, Building_Bed> entry in BedAssignData.ForcedBeds)
            {
                if (!entry.Key.CanBeUsed())
                {
                    _ = BedAssignData.ForcedBeds.Remove(entry.Key);
                    continue;
                }
                if (entry.Value != bed)
                    continue;
                gizmos = gizmos.Append(entry.Key.Gizmo_ForceAssignment(bed));
                forcedPawns.Add(entry.Key);
            }
            if (forcedPawns.Count >= bed.CompAssignableToPawn.MaxAssignedPawnsCount)
                return;
            gizmos = gizmos.Concat(bed.OwnersForReading.Where(pawn => pawn.CanBeUsed() && !forcedPawns.Contains(pawn))
                                      .Select(pawn => pawn.Gizmo_ForceAssignment(bed)));
        }
    }
}