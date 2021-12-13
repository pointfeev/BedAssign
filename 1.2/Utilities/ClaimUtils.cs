using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BedAssign
{
    public static class ClaimUtils
    {
        public static List<Pawn> GetForcedPawns(this Building_Bed bed)
        {
            List<Pawn> forcedPawns = new List<Pawn>() { };
            if (!bed.CanBeUsed()) return forcedPawns;
            foreach (KeyValuePair<Pawn, Building_Bed> forcedPair in BedAssignData.ForcedBeds)
            {
                Pawn pawn = forcedPair.Key;
                Building_Bed pawnForcedBed = forcedPair.Value;
                if (pawnForcedBed == bed && pawn.Map == pawnForcedBed.Map) forcedPawns.Add(pawn);
            }
            //BedAssign.Message("[BedAssign] GetForcedPawns: returned " + forcedPawns.Count + " pawns for " + bed.LabelShort);
            return forcedPawns;
        }

        public static Building_Bed GetForcedBed(this Pawn pawn)
        {
            if (!pawn.CanBeUsed()) return null;
            Building_Bed pawnForcedBed = BedAssignData.ForcedBeds.TryGetValue(pawn);
            if (pawnForcedBed != null && pawn.Map == pawnForcedBed.Map && pawnForcedBed.CanBeUsed())
            {
                //BedAssign.Message("[BedAssign] GetForcedBed: returned " + pawnForcedBed.LabelShort + " for " + pawn.LabelShort);
                return pawnForcedBed;
            }
            return null;
        }

        public static Pawn GetMostLikedLovePartner(this Pawn pawn)
        {
            if (!pawn.CanBeUsed()) return null;
            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, false);
            if (partner != null && pawn.Map == partner.Map)
            {
                //BedAssign.Message("[BedAssign] GetMostLikedLovePartner: returned " + partner.LabelShort + " for " + pawn.LabelShort);
                return partner;
            }
            return null;
        }

        public static void TryMakeSpaceFor(this Building_Bed bed, Pawn pawn)
        {
            if (!pawn.CanBeUsed() || !bed.CanBeUsed()) return;
            List<Pawn> otherOwners = bed.OwnersForReading.FindAll(p => p != pawn);
            if (otherOwners.Any())
            {
                for (int i = otherOwners.Count - 1; i >= 0; i--)
                {
                    Pawn sleeper = otherOwners[i];
                    if (!LovePartnerRelationUtility.LovePartnerRelationExists(pawn, sleeper) && sleeper.TryUnclaimBed())
                    {
                        //BedAssign.Message("[BedAssign] MakeSpaceFor: kicked " + sleeper.LabelShort + " out of " + bed.LabelShort + " to make space for " + pawn.LabelShort);
                    }
                }
            }
        }

        public static bool TryClaimBed(this Pawn pawn, Building_Bed bed, bool canMakeSpaceFor = true)
        {
            if (!pawn.CanBeUsed() || !bed.CanBeUsed()) return false;
            if (pawn.Map != bed.Map)
            {
                //BedAssign.Message("[BedAssign] TryClaimBed failed: " + bed.LabelShort + " not on same map as " + pawn.LabelShort);
                return false;
            }

            if (bed.Medical || !RestUtility.CanUseBedEver(pawn, bed.def))
            {
                //BedAssign.Message("[BedAssign] TryClaimBed failed: " + pawn.LabelShort + " can never use " + bed.LabelShort);
                return false;
            }

            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == bed)
            {
                //BedAssign.Message("[BedAssign] TryClaimBed failed: " + pawn.LabelShort + " already claims " + bed.LabelShort);
                return false;
            }

            Building_Bed pawnForcedBed = pawn.GetForcedBed();
            bool forced = pawnForcedBed is null || pawnForcedBed == bed;
            if (!forced)
            {
                //BedAssign.Message("[BedAssign] TryClaimBed failed: " + bed.LabelShort + " is not " + pawn.LabelShort + "'s forced bed");
                return false;
            }

            if (bed.GetForcedPawns().Any(sleeper => !LovePartnerRelationUtility.LovePartnerRelationExists(pawn, sleeper)))
            {
                //BedAssign.Message("[BedAssign] TryClaimBed failed: " + bed.LabelShort + " has forced pawns that are unable to sleep with " + pawn.LabelShort);
                return false;
            }

            if (canMakeSpaceFor) bed.TryMakeSpaceFor(pawn);
            if (bed.AnyUnownedSleepingSlot && pawn.ownership.ClaimBedIfNonMedical(bed))
            {
                //BedAssign.Message("[BedAssign] TryClaimBed succeeded: " + pawn.LabelShort + " claimed " + bed.LabelShort);
                return true;
            }
            //BedAssign.Message("[BedAssign] TryClaimBed failed: unable to make room for " + pawn.LabelShort + " in " + bed.LabelShort);
            return false;
        }

        public static bool TryUnclaimBed(this Pawn pawn)
        {
            if (!pawn.CanBeUsed()) return false;

            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == null)
            {
                //BedAssign.Message("[BedAssign] TryUnclaimBed failed: " + pawn.LabelShort + " has no bed");
                return false;
            }

            Building_Bed pawnForcedBed = BedAssignData.ForcedBeds.TryGetValue(pawn);
            if (pawnForcedBed == null || pawnForcedBed != pawnBed || pawnForcedBed.Map != pawn.Map)
            {
                //BedAssign.Message("[BedAssign] TryUnclaimBed succeeded: " + pawn.LabelShort + " unclaimed " + pawnBed.LabelShort);
                pawn.mindState.lastDisturbanceTick = Find.TickManager.TicksGame;
                RestUtility.WakeUp(pawn);
                pawn.ownership.UnclaimBed();
                return true;
            }
            //BedAssign.Message("[BedAssign] TryUnclaimBed failed: " + pawn.LabelShort + " can't unclaim forced bed");
            return false;
        }
    }
}