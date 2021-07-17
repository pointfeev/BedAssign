using RimWorld;
using Verse;
using System.Collections.Generic;

namespace BedAssign
{
    public class BedAssignData : GameComponent
    {
        public BedAssignData(Game game) { }

        public static Dictionary<Pawn, Building_Bed> ForcedPawnBed = new Dictionary<Pawn, Building_Bed>();

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref ForcedPawnBed, "ForcedPawnBed", LookMode.Reference, LookMode.Reference);
			ForcedPawnBed.RemoveAll((KeyValuePair<Pawn, Building_Bed> entry) => entry.Key == null || entry.Value == null);
			base.ExposeData();
        }
	}
}
