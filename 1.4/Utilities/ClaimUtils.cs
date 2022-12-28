using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace BedAssign.Utilities
{
    public static class ClaimUtils
    {
        private static IEnumerable<Pawn> GetForcedPawns(this Building_Bed bed)
        {
            if (!bed.CanBeUsed())
                yield break;
            foreach (Pawn pawn in from forcedPair in BedAssignData.ForcedBeds
                                  let pawn = forcedPair.Key
                                  let pawnForcedBed = forcedPair.Value
                                  where pawnForcedBed == bed && pawn.MapHeld == pawnForcedBed.MapHeld
                                  select pawn)
                yield return pawn;
        }

        public static Building_Bed GetForcedBed(this Pawn pawn)
        {
            if (!pawn.CanBeUsed())
                return null;
            Building_Bed pawnForcedBed = BedAssignData.ForcedBeds.TryGetValue(pawn);
            if (pawnForcedBed != null && pawn.MapHeld == pawnForcedBed.MapHeld && pawnForcedBed.CanBeUsed())
                //BedAssign.Message("GetForcedBed: returned " + pawnForcedBed.LabelShort + " for " + pawn.LabelShort);
                return pawnForcedBed;
            return null;
        }

        public static Pawn GetMostLikedLovePartner(this Pawn pawn)
        {
            if (!pawn.CanBeUsed())
                return null;
            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, false);
            if (partner != null && pawn.MapHeld == partner.MapHeld && BedUtility.WillingToShareBed(pawn, partner))
                //BedAssign.Message("GetMostLikedLovePartner: returned " + partner.LabelShort + " for " + pawn.LabelShort);
                return partner;
            return null;
        }

        private static void TryMakeSpaceFor(this Building_Bed bed, Pawn pawn)
        {
            if (!pawn.CanBeUsed() || !bed.CanBeUsed())
                return;
            IEnumerable<Pawn> otherOwners = bed.OwnersForReading.Where(p => p != pawn);
            foreach (Pawn sleeper in otherOwners)
                if ((!sleeper.CanBeUsed() || !LovePartnerRelationUtility.LovePartnerRelationExists(pawn, sleeper)
                                          || !BedUtility.WillingToShareBed(pawn, sleeper))
                 && sleeper.TryUnClaimBed())
                {
                    //BedAssign.Message("MakeSpaceFor: kicked " + sleeper.LabelShort + " out of " + bed.LabelShort + " to make space for " + pawn.LabelShort);
                }
        }

        public static bool TryClaimBed(this Pawn pawn, Building_Bed bed, bool canMakeSpaceFor = true)
        {
            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == bed)
                //BedAssign.Message("TryClaimBed succeeded: " + pawn.LabelShort + " already claims " + bed.LabelShort);
                return true;
            if (!pawn.CanBeUsed() || !bed.CanBeUsed())
                return false;
            if (pawn.MapHeld != bed.MapHeld)
                //BedAssign.Message("TryClaimBed failed: " + bed.LabelShort + " not on same map as " + pawn.LabelShort);
                return false;
            Building_Bed pawnForcedBed = pawn.GetForcedBed();
            bool forced = pawnForcedBed is null || pawnForcedBed == bed;
            if (!forced)
                //BedAssign.Message("TryClaimBed failed: " + bed.LabelShort + " is not " + pawn.LabelShort + "'s forced bed");
                return false;
            if (bed.Medical || !RestUtility.CanUseBedEver(pawn, bed.def))
                //BedAssign.Message("TryClaimBed failed: " + pawn.LabelShort + " can never use " + bed.LabelShort);
                return false;
            if (bed.CompAssignableToPawn.IdeoligionForbids(pawn))
                //BedAssign.Message($"TryClaimBed failed: {pawn.LabelShort}'s ideology forbids them from being assigned to {bed.LabelShort}");
                return false;
            if (bed.GetForcedPawns().Any(sleeper => sleeper != pawn && sleeper.CanBeUsed()
                                                                    && (!LovePartnerRelationUtility
                                                                           .LovePartnerRelationExists(pawn, sleeper)
                                                                     || !BedUtility.WillingToShareBed(pawn, sleeper))))
                //BedAssign.Message("TryClaimBed failed: " + bed.LabelShort + " has forced pawns that are unable to sleep with " + pawn.LabelShort);
                return false;
            if (!bed.InteractionCell.InAllowedArea(pawn))
                //BedAssign.Message("TryClaimBed failed: " + bed.LabelShort + " is outside " + pawn.LabelShort + "'s allowed area");
                return false;
            if (!pawn.CanReach(bed, PathEndMode.InteractionCell, Danger.None))
                //BedAssign.Message("TryClaimBed failed: " + pawn.LabelShort + " can not reach " + bed.LabelShort);
                return false;
            if (canMakeSpaceFor)
                bed.TryMakeSpaceFor(pawn);
            if (bed.AnyUnownedSleepingSlot && pawn.ownership.ClaimBedIfNonMedical(bed))
                //BedAssign.Message("TryClaimBed succeeded: " + pawn.LabelShort + " claimed " + bed.LabelShort);
                return true;
            //BedAssign.Message("TryClaimBed failed: unable to make room for " + pawn.LabelShort + " in " + bed.LabelShort);
            return false;
        }

        public static bool TryUnClaimBed(this Pawn pawn)
        {
            if (!pawn.CanBeUsed())
                return false;
            Building_Bed pawnBed = pawn.ownership.OwnedBed;
            if (pawnBed == null)
                //BedAssign.Message("TryUnClaimBed failed: " + pawn.LabelShort + " has no bed");
                return false;
            Building_Bed pawnForcedBed = BedAssignData.ForcedBeds.TryGetValue(pawn);
            if (pawnForcedBed == null || pawnForcedBed != pawnBed || pawnForcedBed.MapHeld != pawn.MapHeld)
            {
                //BedAssign.Message("TryUnClaimBed succeeded: " + pawn.LabelShort + " unclaimed " + pawnBed.LabelShort);
                pawn.mindState.lastDisturbanceTick = Find.TickManager.TicksGame;
                RestUtility.WakeUp(pawn);
                _ = pawn.ownership.UnclaimBed();
                return true;
            }
            //BedAssign.Message("TryUnClaimBed failed: " + pawn.LabelShort + " can't unclaim forced bed");
            return false;
        }
    }
}