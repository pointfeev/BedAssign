using RimWorld;

using System.Collections.Generic;

using Verse;
using Verse.AI;

namespace BedAssign
{
    public static class ClaimUtils
    {
        public static List<Pawn> GetForcedPawns(this Building_Bed bed)
        {
            List<Pawn> forcedPawns = new List<Pawn>() { };
            if (!bed.CanBeUsed())
                return forcedPawns;

            foreach (KeyValuePair<Pawn, Building_Bed> forcedPair in BedAssignData.ForcedBeds)
            {
                Pawn pawn = forcedPair.Key;
                Building_Bed pawnForcedBed = forcedPair.Value;
                if (pawnForcedBed == bed && pawn.Map == pawnForcedBed.Map)
                    forcedPawns.Add(pawn);
            }
            //BedAssign.Message("GetForcedPawns: returned " + forcedPawns.Count + " pawns for " + bed.LabelShort);
            return forcedPawns;
        }

        public static Building_Bed GetForcedBed(this Pawn pawn)
        {
            if (!pawn.CanBeUsed())
                return null;

            Building_Bed pawnForcedBed = BedAssignData.ForcedBeds.TryGetValue(pawn);
            if (pawnForcedBed != null && pawn.Map == pawnForcedBed.Map && pawnForcedBed.CanBeUsed())
            {
                //BedAssign.Message("GetForcedBed: returned " + pawnForcedBed.LabelShort + " for " + pawn.LabelShort);
                return pawnForcedBed;
            }
            return null;
        }

        public static Pawn GetMostLikedLovePartner(this Pawn pawn)
        {
            if (!pawn.CanBeUsed())
                return null;

            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, false);
            if (partner != null && pawn.Map == partner.Map)
            {
                //BedAssign.Message("GetMostLikedLovePartner: returned " + partner.LabelShort + " for " + pawn.LabelShort);
                return partner;
            }
            return null;
        }

        public static void TryMakeSpaceFor(this Building_Bed bed, Pawn pawn)
        {
            if (!pawn.CanBeUsed() || !bed.CanBeUsed())
                return;

            List<Pawn> otherOwners = bed.OwnersForReading.FindAll(p => p != pawn);
            if (otherOwners.Any())
                for (int i = otherOwners.Count - 1; i >= 0; i--)
                {
                    Pawn sleeper = otherOwners[i];
                    if ((!sleeper.CanBeUsed() || !LovePartnerRelationUtility.LovePartnerRelationExists(pawn, sleeper) || !BedUtility.WillingToShareBed(pawn, sleeper))
                        && sleeper.TryUnclaimBed())
                    {
                        //BedAssign.Message("MakeSpaceFor: kicked " + sleeper.LabelShort + " out of " + bed.LabelShort + " to make space for " + pawn.LabelShort);
                    }
                }
        }

        public static bool TryClaimBed(this Pawn pawn, Building_Bed bed, bool canMakeSpaceFor = true)
        {
            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == bed)
            {
                //BedAssign.Message("TryClaimBed succeeded: " + pawn.LabelShort + " already claims " + bed.LabelShort);
                return true;
            }

            if (!pawn.CanBeUsed() || !bed.CanBeUsed())
                return false;

            if (pawn.Map != bed.Map)
            {
                //BedAssign.Message("TryClaimBed failed: " + bed.LabelShort + " not on same map as " + pawn.LabelShort);
                return false;
            }

            Building_Bed pawnForcedBed = pawn.GetForcedBed();
            bool forced = pawnForcedBed is null || pawnForcedBed == bed;
            if (!forced)
            {
                //BedAssign.Message("TryClaimBed failed: " + bed.LabelShort + " is not " + pawn.LabelShort + "'s forced bed");
                return false;
            }

            if (bed.Medical || !RestUtility.CanUseBedEver(pawn, bed.def))
            {
                //BedAssign.Message("TryClaimBed failed: " + pawn.LabelShort + " can never use " + bed.LabelShort);
                return false;
            }

            if (bed.CompAssignableToPawn.IdeoligionForbids(pawn))
            {
                //BedAssign.Message($"TryClaimBed failed: {pawn.LabelShort}'s ideology forbids them from being assigned to {bed.LabelShort}");
                return false;
            }

            if (bed.GetForcedPawns().Any(sleeper => sleeper != pawn && sleeper.CanBeUsed() && (!LovePartnerRelationUtility.LovePartnerRelationExists(pawn, sleeper) || !BedUtility.WillingToShareBed(pawn, sleeper))))
            {
                //BedAssign.Message("TryClaimBed failed: " + bed.LabelShort + " has forced pawns that are unable to sleep with " + pawn.LabelShort);
                return false;
            }

            if (!ForbidUtility.InAllowedArea(bed.InteractionCell, pawn))
            {
                //BedAssign.Message("TryClaimBed failed: " + bed.LabelShort + " is outside " + pawn.LabelShort + "'s allowed area");
                return false;
            }

            if (!ReachabilityUtility.CanReach(pawn, bed, PathEndMode.InteractionCell, Danger.None))
            {
                //BedAssign.Message("TryClaimBed failed: " + pawn.LabelShort + " can not reach " + bed.LabelShort);
                return false;
            }

            if (canMakeSpaceFor)
                bed.TryMakeSpaceFor(pawn);

            if (bed.AnyUnownedSleepingSlot && pawn.ownership.ClaimBedIfNonMedical(bed))
            {
                //BedAssign.Message("TryClaimBed succeeded: " + pawn.LabelShort + " claimed " + bed.LabelShort);
                return true;
            }
            //BedAssign.Message("TryClaimBed failed: unable to make room for " + pawn.LabelShort + " in " + bed.LabelShort);
            return false;
        }

        public static bool TryUnclaimBed(this Pawn pawn)
        {
            if (!pawn.CanBeUsed())
                return false;

            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == null)
            {
                //BedAssign.Message("TryUnclaimBed failed: " + pawn.LabelShort + " has no bed");
                return false;
            }

            Building_Bed pawnForcedBed = BedAssignData.ForcedBeds.TryGetValue(pawn);
            if (pawnForcedBed == null || pawnForcedBed != pawnBed || pawnForcedBed.Map != pawn.Map)
            {
                //BedAssign.Message("TryUnclaimBed succeeded: " + pawn.LabelShort + " unclaimed " + pawnBed.LabelShort);
                pawn.mindState.lastDisturbanceTick = Find.TickManager.TicksGame;
                RestUtility.WakeUp(pawn);
                _ = pawn.ownership.UnclaimBed();
                return true;
            }
            //BedAssign.Message("TryUnclaimBed failed: " + pawn.LabelShort + " can't unclaim forced bed");
            return false;
        }
    }
}