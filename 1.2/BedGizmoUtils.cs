using RimWorld;
using System.Collections.Generic;
using System.Linq;
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

        public static IEnumerable<Gizmo> CreateBedGizmos(Building_Bed bed, IEnumerable<Gizmo> gizmos)
        {
            if (bed.Medical) { return gizmos; }

            List<Gizmo> Gizmos = gizmos.ToList();

            List<Pawn> forcedPawns = new List<Pawn>();
            foreach (KeyValuePair<Pawn, Building_Bed> entry in BedAssignData.ForcedPawnBed.ToList())
            {
                if (!ClaimUtils.CanUsePawn(entry.Key))
                {
                    if (entry.Value.OwnersForReading.Contains(entry.Key))
                    {
                        entry.Value.OwnersForReading.Remove(entry.Key);
                    }
                    BedAssignData.ForcedPawnBed.Remove(entry.Key);
                    continue;
                }
                if (entry.Value == bed)
                {
                    Gizmos.Add(CreateBedForceGizmo(bed, entry.Key));
                    forcedPawns.Add(entry.Key);
                }
            }

            if (forcedPawns.Count < bed.GetSlotCount())
            {
                foreach (Pawn pawn in bed.OwnersForReading)
                {
                    if (!ClaimUtils.CanUsePawn(pawn))
                    {
                        continue;
                    }
                    if (!forcedPawns.Contains(pawn))
                    {
                        Gizmos.Add(CreateBedForceGizmo(bed, pawn));
                    }
                }
            }

            return Gizmos.AsEnumerable();
        }
    }
}
