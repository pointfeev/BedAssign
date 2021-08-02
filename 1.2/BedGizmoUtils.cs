using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BedAssign
{
    public static class BedGizmoUtils
    {
        public static BedForceGizmo CreateBedForceGizmo(Building_Bed bed, Pawn pawn)
        {
            return new BedForceGizmo
            {
                toggleAction = delegate ()
                {
                    if (BedAssignData.ForcedPawnBed.TryGetValue(pawn) == bed)
                    {
                        BedAssignData.ForcedPawnBed.Remove(pawn);
                    }
                    else
                    {
                        BedAssignData.ForcedPawnBed.SetOrAdd(pawn, bed);
                    }
                },
                isActive = () => BedAssignData.ForcedPawnBed.TryGetValue(pawn) == bed,
                defaultDesc = "Force " + pawn.LabelShort + " to always be assigned to this bed when they decide to get rest.",
                order = -100f,
                owner = pawn
            };
        }

        public static List<Gizmo> CreateBedGizmos(Building_Bed bed, List<Gizmo> gizmos)
        {
            List<Pawn> forcedPawns = new List<Pawn>();
            foreach (KeyValuePair<Pawn, Building_Bed> entry in BedAssignData.ForcedPawnBed)
            {
                if (entry.Value == bed)
                {
                    gizmos.Add(CreateBedForceGizmo(bed, entry.Key));
                    forcedPawns.Add(entry.Key);
                }
            }

            if (forcedPawns.Count < bed.SleepingSlotsCount)
            {
                foreach (Pawn pawn in bed.OwnersForReading)
                {
                    if (!forcedPawns.Contains(pawn))
                    {
                        gizmos.Add(CreateBedForceGizmo(bed, pawn));
                    }
                }
            }

            return gizmos;
        }
    }
}
