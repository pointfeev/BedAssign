using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BedAssign
{
    public static class BedAssign
    {
        public static Building_Bed GetForcedPawnBedIfPossible(Pawn pawn)
        {
            if (pawn == null) { return null; }
            Building_Bed pawnForcedBed = BedAssignData.ForcedPawnBed.TryGetValue(pawn);
            if (pawnForcedBed != null && pawn.Map == pawnForcedBed.Map)
            {
                Log.Message("[BedAssign] GetForcedPawnBedIfPossible: returned " + pawnForcedBed.LabelShort + " for " + pawn.LabelShort);
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
                Log.Message("[BedAssign] GetMostLikedLovePartnerIfPossible: returned " + partner.LabelShort + " for " + pawn.LabelShort);
                return partner;
            }
            return null;
        }

        public static void MakeSpaceInBed(Pawn pawn, Building_Bed bed)
        {
            MakeSpaceInBed(pawn, bed, null);
        }

        public static void MakeSpaceInBed(Pawn pawn, Building_Bed bed, Pawn lover)
        {
            if (pawn == null || bed == null) { return; }
            List<Pawn> owners = bed.OwnersForReading;
            if (owners.Count > 0)
            {
                for (int i = owners.Count - 1; i >= 0; i--)
                {
                    Pawn sleeper = owners[i];
                    if (!LovePartnerRelationUtility.LovePartnerRelationExists(pawn, sleeper))
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

        public static bool ClaimBedIfPossible(Pawn pawn, Building_Bed bed)
        {
            return ClaimBedIfPossible(pawn, bed, null);
        }

        public static bool ClaimBedIfPossible(Pawn pawn, Building_Bed bed, Pawn pawnLoverToMakeSpaceWith)
        {
            if (pawn == null || bed == null) { Log.Message("[BedAssign] ClaimBedIfPossible failed: null parameter"); return false; }
            if (pawn.Map != bed.Map) { Log.Message("[BedAssign] ClaimBedIfPossible failed: " + bed.LabelShort + " not on same map as " + pawn.LabelShort); return false; }

            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == bed) { Log.Message("[BedAssign] ClaimBedIfPossible failed: " + pawn.LabelShort + " already claims " + bed.LabelShort); return false; }

            Building_Bed pawnForcedBed = GetForcedPawnBedIfPossible(pawn);
            bool forced = pawnForcedBed == null || pawnForcedBed == bed;
            if (!forced) { Log.Message("[BedAssign] ClaimBedIfPossible failed: " + bed.LabelShort + " is not " + pawn.LabelShort + "'s forced bed"); return false; }

            try { MakeSpaceInBed(pawn, bed, pawnLoverToMakeSpaceWith); } catch { }
            if (bed.OwnersForReading.Count < bed.SleepingSlotsCount)
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
            if (pawnBed == null) { Log.Message("[BedAssign] UnclaimBedIfPossible failed: " + pawn.LabelShort + " has no bed"); return false; }

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

        public static void CheckBeds(Pawn pawn)
        {
            if (pawn == null || pawn.ownership == null) { return; }

            // Unclaim off-map bed to give space to other colonists
            Building_Bed currentBed = pawn.ownership.OwnedBed;
            if (currentBed != null && pawn.Map != currentBed.Map)
            {
                pawn.ownership.UnclaimBed();
                Log.Message("[BedAssign] " + pawn.LabelShort + " unclaimed their bed due to being on a different map.");
            }

            // Attempt to claim forced bed
            Building_Bed forcedBed = GetForcedPawnBedIfPossible(pawn);
            if (forcedBed != null)
            {
                if (ClaimBedIfPossible(pawn, forcedBed))
                {
                    Log.Message("[BedAssign] " + pawn.LabelShort + " claimed their forced bed.");
                    return;
                }
            }

            // Attempt to avoid the "Want to sleep with partner" moodlet
            Pawn pawnLover = GetMostLikedLovePartnerIfPossible(pawn);
            if (pawnLover != null)
            {
                Building_Bed loverBed = pawnLover.ownership.OwnedBed;
                if (loverBed != null)
                {
                    if (ClaimBedIfPossible(pawn, loverBed, pawnLover))
                    {
                        Log.Message("[BedAssign] " + pawn.LabelShort + " claimed their lover's bed.");
                        return;
                    }
                }
            }

            // Attempt to avoid the "Sharing bed" moodlet
            if (LovePartnerRelationUtility.GetMostDislikedNonPartnerBedOwner(pawn) != null)
            {
                if (UnclaimBedIfPossible(pawn))
                {
                    Log.Message("[BedAssign] " + pawn.LabelShort + " unclaimed their bed to avoid 'Sharing bed' moodlet.");
                    return;
                }
            }
        }
    }
}
