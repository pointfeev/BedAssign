using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BedAssign
{
    internal class ClaimUtils
    {
        public static bool CanUsePawn(Pawn pawn)
        {
            if (pawn is null || pawn.ownership is null) { return false; }
            if (pawn.Faction != Faction.OfPlayerSilentFail || !pawn.IsFreeColonist) { return false; }
            if (pawn.def is null || pawn.def.race is null || !pawn.def.race.Humanlike) { return false; }
            return true;
        }

        public static Building_Bed GetForcedPawnBedIfPossible(Pawn pawn)
        {
            if (!CanUsePawn(pawn)) { return null; }
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
            if (!CanUsePawn(pawn)) { return null; }
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
            if (!CanUsePawn(pawn) || bed == null || bed.Medical) { return; }
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

        public static bool ClaimBedIfPossible(Pawn pawn, Building_Bed bed, Pawn pawnLoverToMakeSpaceWith = null)
        {
            if (!CanUsePawn(pawn) || bed == null || bed.Medical) { return false; }
            if (pawn.Map != bed.Map) { Log.Message("[BedAssign] ClaimBedIfPossible failed: " + bed.LabelShort + " not on same map as " + pawn.LabelShort); return false; }

            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == bed) { /*Log.Message("[BedAssign] ClaimBedIfPossible failed: " + pawn.LabelShort + " already claims " + bed.LabelShort);*/ return false; }

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
            if (!CanUsePawn(pawn)) { return false; }

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
