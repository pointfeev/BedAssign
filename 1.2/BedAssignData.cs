using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BedAssign
{
    public class BedAssignData : GameComponent
    {
        public BedAssignData(Game _) { }

        private static Dictionary<Pawn, Building_Bed> forcedPawnBed;
        public static Dictionary<Pawn, Building_Bed> ForcedPawnBed
        {
            get
            {
                if (forcedPawnBed is null)
                {
                    forcedPawnBed = new Dictionary<Pawn, Building_Bed>();
                }
                forcedPawnBed.RemoveAll((KeyValuePair<Pawn, Building_Bed> entry) => entry.Key == null || entry.Value == null);
                return forcedPawnBed;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref forcedPawnBed, "ForcedPawnBed", LookMode.Reference, LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _ = ForcedPawnBed;
            }
        }
    }
}
