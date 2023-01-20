using System.Collections.Generic;
using BedAssign.Utilities;
using RimWorld;
using Verse;

namespace BedAssign;

public class BedAssignData : GameComponent
{
    private static Dictionary<Pawn, Building_Bed> forcedBeds;

    private static List<Building_Bed> unusableBeds;
    public BedAssignData(Game _) { }

    public static Dictionary<Pawn, Building_Bed> ForcedBeds
    {
        get
        {
            if (forcedBeds is null)
                forcedBeds = new();
            _ = forcedBeds.RemoveAll(entry => !entry.Key.CanBeUsed() || !entry.Value.CanBeUsedEver());
            return forcedBeds;
        }
    }

    public static List<Building_Bed> UnusableBeds
    {
        get
        {
            if (unusableBeds is null)
                unusableBeds = new();
            _ = unusableBeds.RemoveAll(bed => !bed.CanBeUsedEver());
            return unusableBeds;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref forcedBeds, "ForcedBeds", LookMode.Reference, LookMode.Reference);
        Scribe_Collections.Look(ref unusableBeds, "UnusableBeds", LookMode.Reference);
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
            return;
        _ = ForcedBeds;
        _ = UnusableBeds;
    }
}