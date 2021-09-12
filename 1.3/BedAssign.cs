using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BedAssign
{
    public static class BedAssign
    {
        public static List<Building_Bed> GetSortedBedsOnPawnsMap(Pawn pawn, bool descending = true)
        {
            List<Building_Bed> bedsSorted = pawn?.Map?.listerBuildings?.AllBuildingsColonistOfClass<Building_Bed>().ToList();
            if (bedsSorted is null)
                bedsSorted = new List<Building_Bed>();
            bedsSorted.RemoveAll(bed => bed is null || !bed.CanBeUsed());
            int bed1IsBetter = descending ? -1 : 1;
            int bed2IsBetter = descending ? 1 : -1;
            bedsSorted.Sort(delegate (Building_Bed bed1, Building_Bed bed2)
            {
                return bed1.IsBetterThan(bed2) ? bed1IsBetter : bed2IsBetter;
            });
            return bedsSorted;
        }

        public static void LookForBedReassignment(this Pawn pawn)
        {
            if (!pawn.CanBeUsed()) { return; }

            // Unclaim off-map bed to give space to other colonists
            Building_Bed currentBed = pawn.ownership.OwnedBed;
            if (currentBed != null && pawn.Map != currentBed.Map)
            {
                pawn.ownership.UnclaimBed();
                Log.Message("[BedAssign] " + pawn.LabelShort + " unclaimed their bed due to being off-map");
                currentBed = null;
            }

            // Attempt to claim forced bed
            Building_Bed forcedBed = pawn.GetForcedBed();
            if (!(forcedBed is null) && pawn.TryClaimBed(forcedBed))
            {
                Log.Message("[BedAssign] " + pawn.LabelShort + " claimed their forced bed");
                return;
            }

            if (pawn.Map is null) { return; }

            List<Building_Bed> bedsSorted = GetSortedBedsOnPawnsMap(pawn);
            Pawn pawnLover = pawn.GetMostLikedLovePartner();
            if (pawnLover?.GetMostLikedLovePartner() != pawn)
                pawnLover = null;

            bool PerformBetterBedSearch(string partnerOutput, string singleOutput, TraitDef forTraitDef = null, TraitDef[] excludedOwnerTraitDefs = null)
            {
                if (!(forTraitDef is null) && !pawn.story.traits.allTraits.Any(trait => trait.def == forTraitDef))
                {
                    return false;
                }
                else if (!(pawnLover is null) && BedUtility.WillingToShareBed(pawn, pawnLover))
                {
                    // ... with their lover
                    foreach (Building_Bed bed in bedsSorted)
                    {
                        bool bedUnowned = !bed.OwnersForReading.Any();
                        if (!bedUnowned && !(forTraitDef is null))
                        {
                            bedUnowned = !bed.OwnersForReading.Any(p => p.story.traits.allTraits.Any(t => t.def == forTraitDef));
                        }
                        bool bedHasOwnerWithExcludedTrait = false;
                        if (!bedUnowned && !(excludedOwnerTraitDefs is null) && excludedOwnerTraitDefs.Any())
                        {
                            bedHasOwnerWithExcludedTrait = bed.OwnersForReading.Any(p => p.story.traits.allTraits.Any(t => excludedOwnerTraitDefs.Contains(t.def)));
                        }
                        if (!bed.Medical && bedUnowned && !bedHasOwnerWithExcludedTrait && // is the bed unowned?
                            RestUtility.CanUseBedEver(pawn, bed.def) && RestUtility.CanUseBedEver(pawnLover, bed.def) && // can the bed be used by both lovers?
                            bed.GetBedSlotCount() >= 2 && // does the bed have slots for both lovers?
                            bed.IsBetterThan(currentBed) && // is the bed better than their current?
                            pawn.TryClaimBed(bed) && pawnLover.TryClaimBed(bed))
                        {
                            Log.Message(partnerOutput);
                            return true;
                        }
                        else if (!(currentBed is null)) // undo the change if both partners didn't successfully claim the bed together
                        {
                            pawn.TryClaimBed(currentBed);
                        }
                    }
                }
                else
                {
                    // ... for themself
                    foreach (Building_Bed bed in bedsSorted)
                    {
                        bool bedUnowned = !bed.OwnersForReading.Any();
                        if (!bedUnowned && !(forTraitDef is null))
                        {
                            bedUnowned = !bed.OwnersForReading.Any(p => p.story.traits.allTraits.Any(t => t.def == forTraitDef));
                        }
                        bool bedHasOwnerWithExcludedTrait = false;
                        if (!bedUnowned && !(excludedOwnerTraitDefs is null) && excludedOwnerTraitDefs.Any())
                        {
                            bedHasOwnerWithExcludedTrait = bed.OwnersForReading.Any(p => p.story.traits.allTraits.Any(t => excludedOwnerTraitDefs.Contains(t.def)));
                        }
                        if (!bed.Medical && bedUnowned && !bedHasOwnerWithExcludedTrait && // is the bed unowned?
                            RestUtility.CanUseBedEver(pawn, bed.def) && // can the bed be used?
                            bed.IsBetterThan(currentBed) && // is the bed better than their current?
                            pawn.TryClaimBed(bed))
                        {
                            Log.Message(singleOutput);
                            return true;
                        }
                    }
                }
                return false;
            }

            // Attempt to avoid the Jealous mood penalty
            if (PerformBetterBedSearch($"[BedAssign] Lovers, {pawn.LabelShort} and {pawnLover?.LabelShort}, claimed a better bed together so {pawn.LabelShort} could avoid the Jealous mood penalty",
                "[BedAssign] " + pawn.LabelShort + " claimed a better bed to avoid the Jealous mood penalty", forTraitDef: TraitDefOf.Jealous))
            {
                return;
            }

            // Attempt to avoid the Greedy mood penalty
            if (PerformBetterBedSearch($"[BedAssign] Lovers, {pawn.LabelShort} and {pawnLover?.LabelShort}, claimed a better bed together so {pawn.LabelShort} could avoid the Greedy mood penalty",
                "[BedAssign] " + pawn.LabelShort + " claimed a better bed to avoid the Greedy mood penalty", forTraitDef: TraitDefOf.Greedy))
            {
                return;
            }

            // Attempt to claim a better empty bed
            if (PerformBetterBedSearch($"[BedAssign] Lovers, {pawn.LabelShort} and {pawnLover?.LabelShort}, claimed a better empty bed together",
                "[BedAssign] " + pawn.LabelShort + " claimed a better empty bed", excludedOwnerTraitDefs: new TraitDef[] { TraitDefOf.Jealous, TraitDefOf.Greedy }))
            {
                return;
            }

            // Attempt to avoid the "Want to sleep with partner" mood penalty
            if (!(pawnLover is null) && BedUtility.WillingToShareBed(pawn, pawnLover))
            {
                // Attempt to simply claim their lover's bed
                Building_Bed loverBed = pawnLover.ownership.OwnedBed;
                if (loverBed != null && loverBed.CanBeUsed() && currentBed != loverBed)
                {
                    if (loverBed.AnyUnownedSleepingSlot && RestUtility.CanUseBedEver(pawn, loverBed.def))
                    {
                        if (pawn.TryClaimBed(loverBed))
                        {
                            Log.Message("[BedAssign] " + pawn.LabelShort + " claimed the bed of their lover, " + pawnLover.LabelShort);
                            return;
                        }
                    }
                    else
                    {
                        // Attempt to claim a bed that has more than one sleeping spot for the lovers
                        foreach (Building_Bed bed in bedsSorted)
                        {
                            if (!bed.Medical && bed.GetBedSlotCount() >= 2 &&
                                RestUtility.CanUseBedEver(pawn, bed.def) && RestUtility.CanUseBedEver(pawnLover, bed.def))
                            {
                                bool canClaim = true;
                                List<Pawn> otherOwners = bed.OwnersForReading.FindAll(p => p != pawn & p != pawnLover);
                                if (otherOwners.Any())
                                {
                                    for (int i = otherOwners.Count - 1; i >= 0; i--)
                                    {
                                        Pawn sleeper = otherOwners[i];
                                        if (sleeper.GetMostLikedLovePartner() is null)
                                        {
                                            if (sleeper.TryUnclaimBed())
                                            {
                                                Log.Message("[BedAssign] Kicked " + sleeper.LabelShort + " out of " + bed.LabelShort + " to make space for " + pawn.LabelShort + " and their lover, " + pawnLover.LabelShort);
                                                return;
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
                                if (canClaim && pawn.TryClaimBed(bed) && pawnLover.TryClaimBed(bed))
                                {
                                    Log.Message($"[BedAssign] Lovers, {pawn.LabelShort} and {pawnLover.LabelShort}, claimed a bed together");
                                    return;
                                }
                                else if (canClaim && !(currentBed is null)) // undo the change if both partners didn't successfully claim the bed together
                                {
                                    pawn.TryClaimBed(currentBed);
                                }
                            }
                        }
                    }
                }
            }

            // Attempt to avoid the "Sharing bed" mood penalty
            if (LovePartnerRelationUtility.GetMostDislikedNonPartnerBedOwner(pawn) != null && pawn.TryUnclaimBed())
            {
                Log.Message("[BedAssign] " + pawn.LabelShort + " unclaimed their bed to avoid the bed sharing mood penalty");
                return;
            }
        }
    }
}
