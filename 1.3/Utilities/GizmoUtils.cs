using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BedAssign
{
    public static class GizmoUtils
    {
        private static Dictionary<Building_Bed, Gizmo_UnusableBed> unusableBedGizmos = new Dictionary<Building_Bed, Gizmo_UnusableBed>();

        private static Gizmo_UnusableBed Gizmo_UnusableBed(this Building_Bed bed)
        {
            if (unusableBedGizmos.TryGetValue(bed, out Gizmo_UnusableBed gizmo))
            {
                return gizmo;
            }
            gizmo = new Gizmo_UnusableBed(bed);
            unusableBedGizmos.SetOrAdd(bed, gizmo);
            return gizmo;
        }

        private static Dictionary<Pawn, Gizmo_ForceAssignment> forceAssignmentGizmos = new Dictionary<Pawn, Gizmo_ForceAssignment>();

        private static Gizmo_ForceAssignment Gizmo_ForceAssignment(this Pawn pawn, Building_Bed bed)
        {
            if (forceAssignmentGizmos.TryGetValue(pawn, out Gizmo_ForceAssignment gizmo))
            {
                gizmo.bed = bed;
                return gizmo;
            }
            gizmo = new Gizmo_ForceAssignment(pawn, bed);
            forceAssignmentGizmos.SetOrAdd(pawn, gizmo);
            return gizmo;
        }

        public static IEnumerable<Gizmo> AddModGizmos(this Building_Bed bed, IEnumerable<Gizmo> gizmos)
        {
            if (!bed.CanBeUsedEver())
            {
                return gizmos;
            }

            List<Gizmo> Gizmos = gizmos.ToList();

            Gizmos.Add(bed.Gizmo_UnusableBed());

            if (!bed.CanBeUsed())
            {
                return Gizmos.AsEnumerable();
            }

            List<Pawn> forcedPawns = new List<Pawn>();
            foreach (KeyValuePair<Pawn, Building_Bed> entry in BedAssignData.ForcedBeds.ToList())
            {
                if (!entry.Key.CanBeUsed())
                {
                    if (entry.Value.OwnersForReading.Contains(entry.Key))
                    {
                        entry.Value.OwnersForReading.Remove(entry.Key);
                    }
                    BedAssignData.ForcedBeds.Remove(entry.Key);
                    continue;
                }
                if (entry.Value == bed)
                {
                    Gizmos.Add(entry.Key.Gizmo_ForceAssignment(bed));
                    forcedPawns.Add(entry.Key);
                }
            }

            if (forcedPawns.Count < bed.GetBedSlotCount())
            {
                foreach (Pawn pawn in bed.OwnersForReading)
                {
                    if (!pawn.CanBeUsed())
                    {
                        continue;
                    }
                    if (!forcedPawns.Contains(pawn))
                    {
                        Gizmos.Add(pawn.Gizmo_ForceAssignment(bed));
                    }
                }
            }

            return Gizmos.AsEnumerable();
        }
    }
}