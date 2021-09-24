using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BedAssign
{
    public static class ClaimUtils
    {
        public static Building_Bed GetForcedBed(this Pawn pawn)
        {
            if (!pawn.CanBeUsed()) { return null; }
            Building_Bed pawnForcedBed = BedAssignData.ForcedBeds.TryGetValue(pawn);
            if (pawnForcedBed != null && pawn.Map == pawnForcedBed.Map && pawnForcedBed.CanBeUsed())
            {
                //Log.Message("[BedAssign] GetForcedBed: returned " + pawnForcedBed.LabelShort + " for " + pawn.LabelShort);
                return pawnForcedBed;
            }
            return null;
        }

        public static Pawn GetMostLikedLovePartner(this Pawn pawn)
        {
            if (!pawn.CanBeUsed()) { return null; }
            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, false);
            if (partner != null && pawn.Map == partner.Map)
            {
                //Log.Message("[BedAssign] GetMostLikedLovePartner: returned " + partner.LabelShort + " for " + pawn.LabelShort);
                return partner;
            }
            return null;
        }

        public static void TryMakeSpaceFor(this Building_Bed bed, Pawn pawn)
        {
            if (!pawn.CanBeUsed() || !bed.CanBeUsed()) { return; }
            List<Pawn> otherOwners = bed.OwnersForReading.FindAll(p => p != pawn);
            if (otherOwners.Any())
            {
                for (int i = otherOwners.Count - 1; i >= 0; i--)
                {
                    Pawn sleeper = otherOwners[i];
                    if (!LovePartnerRelationUtility.LovePartnerRelationExists(pawn, sleeper))
                    {
                        Log.Message("[BedAssign] MakeSpaceFor: kicked " + sleeper.LabelShort + " out of " + bed.LabelShort + " to make space for " + pawn.LabelShort);
                    }
                }
            }
        }

        public static bool TryClaimBed(this Pawn pawn, Building_Bed bed)
        {
            if (!pawn.CanBeUsed() || !bed.CanBeUsed()) { return false; }
            if (pawn.Map != bed.Map) { Log.Message("[BedAssign] TryClaimBed failed: " + bed.LabelShort + " not on same map as " + pawn.LabelShort); return false; }

            if (!RestUtility.CanUseBedEver(pawn, bed.def)) { Log.Message("[BedAssign] TryClaimBed failed: " + pawn.LabelShort + " can never use " + bed.LabelShort); return false; }

            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == bed) { /*Log.Message("[BedAssign] TryClaimBed failed: " + pawn.LabelShort + " already claims " + bed.LabelShort);*/ return false; }

            Building_Bed pawnForcedBed = pawn.GetForcedBed();
            bool forced = pawnForcedBed is null || pawnForcedBed == bed;
            if (!forced) { Log.Message("[BedAssign] TryClaimBed failed: " + bed.LabelShort + " is not " + pawn.LabelShort + "'s forced bed"); return false; }

            bed.TryMakeSpaceFor(pawn);
            if (bed.AnyUnownedSleepingSlot)
            {
                pawn.ownership.ClaimBedIfNonMedical(bed);
                Log.Message("[BedAssign] TryClaimBed succeeded: " + pawn.LabelShort + " claimed " + bed.LabelShort);
                return true;
            }
            Log.Message("[BedAssign] TryClaimBed failed: unable to make room for " + pawn.LabelShort + " in " + bed.LabelShort);
            return false;
        }

        public static bool TryUnclaimBed(this Pawn pawn)
        {
            if (!pawn.CanBeUsed()) { return false; }

            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == null) { /*Log.Message("[BedAssign] TryUnclaimBed failed: " + pawn.LabelShort + " has no bed");*/ return false; }

            Building_Bed pawnForcedBed = BedAssignData.ForcedBeds.TryGetValue(pawn);
            if (pawnForcedBed == null || pawnForcedBed != pawnBed || pawnForcedBed.Map != pawn.Map)
            {
                Log.Message("[BedAssign] TryUnclaimBed succeeded: " + pawn.LabelShort + " unclaimed " + pawnBed.LabelShort);
                pawn.mindState.lastDisturbanceTick = Find.TickManager.TicksGame;
                RestUtility.WakeUp(pawn);
                pawn.ownership.UnclaimBed();
                return true;
            }
            Log.Message("[BedAssign] TryUnclaimBed failed: " + pawn.LabelShort + " can't unclaim forced bed");
            return false;
        }
    }
}