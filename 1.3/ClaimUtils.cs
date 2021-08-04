using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BedAssign
{
    internal class ClaimUtils
    {
        public static Building_Bed GetForcedPawnBedIfPossible(Pawn pawn)
        {
            if (pawn == null) { return null; }
            Building_Bed pawnForcedBed = BedAssignData.ForcedPawnBed.TryGetValue(pawn);
            if (pawnForcedBed != null && pawn.Map == pawnForcedBed.Map)
            {
                //Log.Message("[BedAssign] GetForcedPawnBedIfPossible: returned " + pawnForcedBed.LabelShort + " for " + pawn.LabelShort);
                return pawnForcedBed;
            }
            return null;
        }

        public static Pawn GetMostLikedLovePartnerIfPossible(Pawn pawn)
        {
            if (pawn == null) { return null; }
            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, false);
            if (partner != null && pawn.Map == partner.Map)
            {
                //Log.Message("[BedAssign] GetMostLikedLovePartnerIfPossible: returned " + partner.LabelShort + " for " + pawn.LabelShort);
                return partner;
            }
            return null;
        }

        public static void MakeSpaceInBed(Pawn pawn, Building_Bed bed, Pawn lover)
        {
            if (pawn == null || bed == null) { return; }
            if (!BedUtility.WillingToShareBed(pawn, lover)) { Log.Message($"[BedAssign] MakeSpaceInBed failed: {pawn.LabelShort} and their lover, {lover.LabelShort}, aren't willing to share a bed together"); return; }
            List<Pawn> owners = bed.OwnersForReading;
            if (owners.Count > 0)
            {
                for (int i = owners.Count - 1; i >= 0; i--)
                {
                    Pawn sleeper = owners[i];
                    if (!LovePartnerRelationUtility.LovePartnerRelationExists(pawn, sleeper) || !BedUtility.WillingToShareBed(pawn, sleeper))
                    {
                        if (!LovePartnerRelationUtility.LovePartnerRelationExists(lover, sleeper) || GetMostLikedLovePartnerIfPossible(lover) != sleeper)
                        {
                            if (UnclaimBedIfPossible(sleeper))
                            {
                                Log.Message("[BedAssign] MakeSpaceInBed: kicked " + sleeper.LabelShort + " out of " + bed.LabelShort + " to make room for " + pawn.LabelShort);
                            }
                        }
                    }
                }
            }
        }

        public static bool ClaimBedIfPossible(Pawn pawn, Building_Bed bed, Pawn pawnLoverToMakeSpaceWith = null)
        {
            if (pawn == null || bed == null) { Log.Message("[BedAssign] ClaimBedIfPossible failed: null parameter"); return false; }
            if (pawn.Map != bed.Map) { Log.Message("[BedAssign] ClaimBedIfPossible failed: " + bed.LabelShort + " not on same map as " + pawn.LabelShort); return false; }

            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == bed) { /*Log.Message("[BedAssign] ClaimBedIfPossible failed: " + pawn.LabelShort + " already claims " + bed.LabelShort);*/ return false; }

            Building_Bed pawnForcedBed = GetForcedPawnBedIfPossible(pawn);
            bool forced = pawnForcedBed == null || pawnForcedBed == bed;
            if (!forced) { Log.Message("[BedAssign] ClaimBedIfPossible failed: " + bed.LabelShort + " is not " + pawn.LabelShort + "'s forced bed"); return false; }

            if (bed.CompAssignableToPawn.IdeoligionForbids(pawn)) { Log.Message($"[BedAssign] ClaimBedIfPossible failed: {pawn.LabelShort}'s ideology forbids him from being assigned to {bed.LabelShort}"); return false; }

            try { MakeSpaceInBed(pawn, bed, pawnLoverToMakeSpaceWith); } catch { }
            if (bed.OwnersForReading.Count < bed.TotalSleepingSlots)
            {
                Log.Message("[BedAssign] ClaimBedIfPossible succeeded: " + pawn.LabelShort + " claimed " + bed.LabelShort);
                pawn.ownership.ClaimBedIfNonMedical(bed);
                return true;
            }
            Log.Message("[BedAssign] ClaimBedIfPossible failed: unable to make room for " + pawn.LabelShort + " in " + bed.LabelShort);
            return false;
        }

        public static bool UnclaimBedIfPossible(Pawn pawn)
        {
            if (pawn == null) { Log.Message("[BedAssign] UnclaimBedIfPossible failed: null parameter"); return false; }

            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == null) { /*Log.Message("[BedAssign] UnclaimBedIfPossible failed: " + pawn.LabelShort + " has no bed");*/ return false; }

            Building_Bed pawnForcedBed = BedAssignData.ForcedPawnBed.TryGetValue(pawn);
            if (pawnForcedBed == null || pawnForcedBed != pawnBed || pawnForcedBed.Map != pawn.Map)
            {
                Log.Message("[BedAssign] UnclaimBedIfPossible succeeded: " + pawn.LabelShort + " unclaimed " + pawnBed.LabelShort);
                pawn.ownership.UnclaimBed();
                return true;
            }
            Log.Message("[BedAssign] UnclaimBedIfPossible failed: " + pawn.LabelShort + " can't unclaim forced bed");
            return false;
        }
    }
}
