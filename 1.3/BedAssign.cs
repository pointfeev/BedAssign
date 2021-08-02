using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BedAssign
{
    public static class BedAssign
    {
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
            Building_Bed forcedBed = ClaimUtils.GetForcedPawnBedIfPossible(pawn);
            if (forcedBed != null)
            {
                if (ClaimUtils.ClaimBedIfPossible(pawn, forcedBed))
                {
                    Log.Message("[BedAssign] " + pawn.LabelShort + " claimed their forced bed.");
                    return;
                }
            }

            // Attempt to avoid the "Want to sleep with partner" moodlet
            Pawn pawnLover = ClaimUtils.GetMostLikedLovePartnerIfPossible(pawn);
            if (pawnLover != null && BedUtility.WillingToShareBed(pawn, pawnLover))
            {
                // Attempt to simply claim their lover's bed
                Building_Bed loverBed = pawnLover.ownership.OwnedBed;
                if (loverBed != null && currentBed != loverBed)
                {
                    if (loverBed.TotalSleepingSlots > 1)
                    {
                        if (ClaimUtils.ClaimBedIfPossible(pawn, loverBed, pawnLover))
                        {
                            Log.Message("[BedAssign] " + pawn.LabelShort + " claimed the bed of their lover, " + pawnLover.LabelShort + ".");
                            return;
                        }
                    }
                    else if (ClaimUtils.GetMostLikedLovePartnerIfPossible(pawnLover) == pawn)
                    {
                        // Attempt to claim a bed that has more than one sleeping spot for the lovers
                        foreach (Building_Bed bed in pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>())
                        {
                            if (!bed.Medical && bed.TotalSleepingSlots > 1)
                            {
                                bool canClaim = true;
                                List<Pawn> owners = bed.OwnersForReading;
                                if (owners.Count > 0)
                                {
                                    for (int i = owners.Count - 1; i >= 0; i--)
                                    {
                                        Pawn sleeper = owners[i];
                                        if (ClaimUtils.GetMostLikedLovePartnerIfPossible(sleeper) is null)
                                        {
                                            if (ClaimUtils.UnclaimBedIfPossible(sleeper))
                                            {
                                                Log.Message("[BedAssign] Kicked " + sleeper.LabelShort + " out of " + bed.LabelShort + " to make room for " + pawn.LabelShort + " and their lover, " + pawnLover.LabelShort);
                                            }
                                            else
                                            {
                                                canClaim = false;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            canClaim = false;
                                            break;
                                        }
                                    }
                                }
                                if (canClaim && ClaimUtils.ClaimBedIfPossible(pawn, bed) && ClaimUtils.ClaimBedIfPossible(pawnLover, bed))
                                {
                                    Log.Message($"[BedAssign] Lovers, {pawn.LabelShort} and {pawnLover.LabelShort}, claimed a bed together");
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Attempt to avoid the "Sharing bed" moodlet
            if (LovePartnerRelationUtility.GetMostDislikedNonPartnerBedOwner(pawn) != null)
            {
                if (ClaimUtils.UnclaimBedIfPossible(pawn))
                {
                    Log.Message("[BedAssign] " + pawn.LabelShort + " unclaimed their bed to avoid 'Sharing bed' moodlet.");
                    return;
                }
            }
        }
    }
}
