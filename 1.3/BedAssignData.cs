using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BedAssign
{
    public class BedAssignData : GameComponent
    {
        public BedAssignData(Game _)
        {
        }

        private static Dictionary<Pawn, Building_Bed> forcedBeds;

        public static Dictionary<Pawn, Building_Bed> ForcedBeds
        {
            get
            {
                if (forcedBeds is null)
                {
                    forcedBeds = new Dictionary<Pawn, Building_Bed>();
                }
                forcedBeds.RemoveAll((KeyValuePair<Pawn, Building_Bed> entry) => !entry.Key.CanBeUsed() || !entry.Value.CanBeUsed());
                return forcedBeds;
            }
        }

        private static List<Building_Bed> unusableBeds;

        public static List<Building_Bed> UnusableBeds
        {
            get
            {
                if (unusableBeds is null)
                {
                    unusableBeds = new List<Building_Bed>();
                }
                unusableBeds.RemoveAll(bed => !bed.CanBeUsedEver());
                return unusableBeds;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref forcedBeds, "ForcedBeds", LookMode.Reference, LookMode.Reference);
            Scribe_Collections.Look(ref unusableBeds, "UnusableBeds", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _ = ForcedBeds;
                _ = UnusableBeds;
            }
        }
    }
}