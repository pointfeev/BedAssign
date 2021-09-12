using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BedAssign
{
    public static class GizmoUtils
    {
        public static IEnumerable<Gizmo> AddModGizmos(this Building_Bed bed, IEnumerable<Gizmo> gizmos)
        {
            if (!bed.CanBeUsedEver()) return gizmos;

            List<Gizmo> Gizmos = gizmos.ToList();

            Gizmos.Add(new Gizmo_UnusableBed(bed));

            if (!bed.CanBeUsed()) return Gizmos.AsEnumerable();

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
                    Gizmos.Add(new Gizmo_ForceAssignment(bed, entry.Key));
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
                        Gizmos.Add(new Gizmo_ForceAssignment(bed, pawn));
                    }
                }
            }

            return Gizmos.AsEnumerable();
        }
    }
}
