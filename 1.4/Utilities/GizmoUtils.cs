using System.Collections.Generic;
using System.Linq;
using BedAssign.Gizmos;
using RimWorld;
using Verse;

namespace BedAssign.Utilities;

public static class GizmoUtils
{
    private static readonly Dictionary<int, Gizmo_UnusableBed> UnusableBedGizmos = new();

    private static readonly Dictionary<int, Gizmo_ForceAssignment> ForceAssignmentGizmos = new();

    private static Gizmo_UnusableBed GetUnusableBedGizmo(this Building_Bed bed)
    {
        if (UnusableBedGizmos.TryGetValue(bed.thingIDNumber, out Gizmo_UnusableBed gizmo))
            return gizmo;
        gizmo = new(bed);
        UnusableBedGizmos.SetOrAdd(bed.thingIDNumber, gizmo);
        return gizmo;
    }

    private static Gizmo_ForceAssignment GetForceAssignmentGizmo(this Pawn pawn, Building_Bed bed)
    {
        if (ForceAssignmentGizmos.TryGetValue(pawn.thingIDNumber, out Gizmo_ForceAssignment gizmo))
        {
            gizmo.Bed = bed;
            return gizmo;
        }
        gizmo = new(pawn, bed);
        ForceAssignmentGizmos.SetOrAdd(pawn.thingIDNumber, gizmo);
        return gizmo;
    }

    public static void AddModGizmos(this Building_Bed bed, ref IEnumerable<Gizmo> gizmos)
    {
        if (!bed.CanBeUsedEver())
            return;
        gizmos = gizmos.Append(bed.GetUnusableBedGizmo());
        if (!bed.CanBeUsed())
            return;
        List<Pawn> forcedPawns = new();
        foreach (KeyValuePair<Pawn, Building_Bed> entry in BedAssignData.ForcedBeds)
        {
            if (!entry.Key.CanBeUsed())
            {
                _ = BedAssignData.ForcedBeds.Remove(entry.Key);
                continue;
            }
            if (entry.Value != bed)
                continue;
            gizmos = gizmos.Append(entry.Key.GetForceAssignmentGizmo(bed));
            forcedPawns.Add(entry.Key);
        }
        if (forcedPawns.Count >= bed.CompAssignableToPawn.MaxAssignedPawnsCount)
            return;
        gizmos = gizmos.Concat(bed.CompAssignableToPawn.AssignedPawns.Where(pawn => pawn.CanBeUsed() && !forcedPawns.Contains(pawn))
           .Select(pawn => pawn.GetForceAssignmentGizmo(bed)));
    }
}