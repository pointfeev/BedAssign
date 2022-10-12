using RimWorld;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Verse;

namespace BedAssign
{
    public static class BedAssign
    {
        public static void Message(string text, LookTargets lookTargets = null)
        {
            if (ModSettings.outputReassignmentMessages)
                if (lookTargets is null)
                    Messages.Message(text, MessageTypeDefOf.PositiveEvent);
                else
                    Messages.Message(text, lookTargets, MessageTypeDefOf.PositiveEvent);
        }

        public static void Error(string text) => Log.Error(text);

        public static List<Building_Bed> GetSortedBedsOnPawnsMap(Pawn pawn, List<Building_Bed> toOnlyBeSorted = null, bool descending = true)
        {
            List<Building_Bed> bedsSorted = toOnlyBeSorted;
            if (bedsSorted is null)
            {
                bedsSorted = pawn?.Map?.listerBuildings?.AllBuildingsColonistOfClass<Building_Bed>().ToList();
                if (bedsSorted is null)
                    bedsSorted = new List<Building_Bed>();
            }
            _ = bedsSorted.RemoveAll(bed => !bed.CanBeUsed());
            int bed1IsBetter = descending ? -1 : 1;
            int bed2IsBetter = descending ? 1 : -1;
            bedsSorted.Sort((bed1, bed2) => bed1.IsBetterThan(bed2, useThingID: true) ? bed1IsBetter : bed2IsBetter);
            return bedsSorted;
        }

        private static readonly TraitDef[] mutualLoverBedKickExcludedOwnerTraitDefs = new TraitDef[] { TraitDefOf.Jealous, TraitDefOf.Greedy };

        public static void LookForBedReassignment(this Pawn pawn)
        {
            if (!pawn.CanBeUsed()) return;
            Building_Bed currentBed = pawn.ownership.OwnedBed;

            // Unclaim off-map bed to give space to other colonists
            if (currentBed != null && pawn.Map != currentBed.Map)
            {
                _ = pawn.ownership.UnclaimBed();
                Message(pawn.LabelShort + " unclaimed their bed due to being off-map.", new LookTargets(new List<Pawn>() { pawn }));
                currentBed = null;
            }

            // Attempt to claim forced bed
            Building_Bed forcedBed = pawn.GetForcedBed();
            if (!(forcedBed is null) && pawn.TryClaimBed(forcedBed))
            {
                if (currentBed != forcedBed)
                    Message(pawn.LabelShort + " claimed their forced bed.", new LookTargets(new List<Pawn>() { pawn }));
                return;
            }

            if (pawn.Map is null) return;

            // Get the pawn's most liked lover if it's mutual
            Pawn pawnLover = pawn.GetMostLikedLovePartner();
            Pawn pawnLoverNonMutual = pawnLover;
            if (pawnLover?.GetMostLikedLovePartner() != pawn)
                pawnLover = null;

            // Get all beds on the pawn's map, sorted
            List<Building_Bed> bedsDescending = GetSortedBedsOnPawnsMap(pawn);
            List<Building_Bed> bedsAscending = GetSortedBedsOnPawnsMap(pawn, toOnlyBeSorted: bedsDescending, descending: false);

            // Get all of the pawn's mood-related thoughts
            List<Thought> thoughts = new List<Thought>();
            pawn.needs?.mood?.thoughts?.GetAllMoodThoughts(thoughts);

            // Get all of the pawn's lover's mood-related thoughts
            List<Thought> thoughtsLover = new List<Thought>();
            pawnLover?.needs?.mood?.thoughts?.GetAllMoodThoughts(thoughtsLover);

            if (ModSettings.avoidJealousPenalty) // Attempt to avoid the Jealous mood penalty
                if (thoughts.SufferingFromThought("Jealous", out _, out _) &&
                    PawnBedUtils.PerformBetterBedSearch(bedsDescending, currentBed, pawn, pawnLover,
                    singleOutput: pawn.LabelShort + " claimed a better bed to avoid the Jealous mood penalty.",
                    partnerOutput: $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a better bed together so {pawn.LabelShort} could avoid the Jealous mood penalty.",
                    forTraitDef: TraitDefOf.Jealous, betterBedCustomFunc: delegate (Building_Bed bed)
                    {
                        float bedImpressiveness = (bed.GetRoom()?.GetStat(RoomStatDefOf.Impressiveness)).GetValueOrDefault(0);
                        foreach (Pawn p in pawn.Map.mapPawns?.SpawnedPawnsInFaction(Faction.OfPlayer))
                        {
                            if (p.HostFaction is null && (p.RaceProps?.Humanlike).GetValueOrDefault(false) && !(p.ownership is null))
                            {
                                float pImpressiveness = (p.ownership?.OwnedRoom?.GetStat(RoomStatDefOf.Impressiveness)).GetValueOrDefault(0);
                                if (pImpressiveness - bedImpressiveness >= Mathf.Abs(bedImpressiveness * 0.1f))
                                    return false;
                            }
                        }
                        return true;
                    }, excludedOwnerTraitDefs: new TraitDef[] { TraitDefOf.Jealous }))
                    return;

            if (ModSettings.avoidGreedyPenalty) // Attempt to avoid the Greedy mood penalty
                if (thoughts.SufferingFromThought("Greedy", out Thought greedyThought, out float currentBaseMoodEffect) &&
                    PawnBedUtils.PerformBetterBedSearch(bedsDescending, currentBed, pawn, pawnLover,
                    singleOutput: pawn.LabelShort + " claimed a better bed to avoid the Greedy mood penalty.",
                    partnerOutput: $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a better bed together so {pawn.LabelShort} could avoid the Greedy mood penalty.",
                    forTraitDef: TraitDefOf.Greedy, betterBedCustomFunc: delegate (Building_Bed bed)
                    {
                        float impressiveness = (bed.GetRoom()?.GetStat(RoomStatDefOf.Impressiveness)).GetValueOrDefault(0);
                        int stage = RoomStatDefOf.Impressiveness.GetScoreStageIndex(impressiveness) + 1;
                        float? bedBaseMoodEffect = greedyThought.def?.stages?[stage]?.baseMoodEffect;
                        return bedBaseMoodEffect.HasValue && bedBaseMoodEffect.Value > currentBaseMoodEffect;
                    }, excludedOwnerTraitDefs: new TraitDef[] { TraitDefOf.Jealous, TraitDefOf.Greedy }))
                    return;

            if (ModSettings.avoidAsceticPenalty) // Attempt to avoid the Ascetic mood penalty
                if (thoughts.SufferingFromThought("Ascetic", out Thought asceticThought, out float currentBaseMoodEffect) &&
                    (pawnLover is null || !(thoughtsLover?.Find(t => t.def?.defName == "Ascetic") is null)) && // lover should also have Ascetic
                    PawnBedUtils.PerformBetterBedSearch(bedsAscending, currentBed, pawn, pawnLover,
                    singleOutput: pawn.LabelShort + " claimed a worse bed to avoid the Ascetic mood penalty.",
                    partnerOutput: $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a worse bed together so they could both avoid the Ascetic mood penalty.",
                    forTraitDef: TraitDefOf.Ascetic, betterBedCustomFunc: delegate (Building_Bed bed)
                    {
                        float impressiveness = (bed.GetRoom()?.GetStat(RoomStatDefOf.Impressiveness)).GetValueOrDefault(0);
                        int stage = RoomStatDefOf.Impressiveness.GetScoreStageIndex(impressiveness) + 1;
                        float? bedBaseMoodEffect = asceticThought.def?.stages?[stage]?.baseMoodEffect;
                        return bedBaseMoodEffect.HasValue && bedBaseMoodEffect.Value > currentBaseMoodEffect;
                    }, excludedOwnerTraitDefs: new TraitDef[] { TraitDefOf.Ascetic }))
                    return;

            if (ModSettings.claimBetterBeds) // Attempt to claim a better empty bed
                if (!(pawnLover is null && !(pawnLoverNonMutual is null) && pawnLoverNonMutual.ownership?.OwnedBed?.GetBedSlotCount() > 2) // if not a third wheel in a polyamorous relationship involving bigger beds
                    && PawnBedUtils.PerformBetterBedSearch(bedsDescending, currentBed, pawn, pawnLover,
                    singleOutput: pawn.LabelShort + " claimed a better empty bed.",
                    partnerOutput: $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a better empty bed together.",
                    excludedOwnerTraitDefs: new TraitDef[] { TraitDefOf.Jealous, TraitDefOf.Greedy }))
                    return;

            if (ModSettings.avoidPartnerPenalty) // Attempt to avoid the "Want to sleep with partner" mood penalty
                if (thoughts.SufferingFromThought("WantToSleepWithSpouseOrLover", out _, out _) && (!(pawnLover is null) || !(pawnLoverNonMutual is null)))
                {
                    if (pawnLover is null && !(pawnLoverNonMutual is null)) // if pawn only has a polyamorous lover
                    {
                        // Attempt to claim their polyamorous lover's bed if there's extra space
                        Building_Bed loverNonMutualBed = pawnLoverNonMutual.ownership.OwnedBed;
                        if (currentBed != loverNonMutualBed && pawn.TryClaimBed(loverNonMutualBed, false))
                        {
                            Message(pawn.LabelShort + " claimed the bed of their polyamorous lover " + pawnLoverNonMutual.LabelShort + ".", new LookTargets(new List<Pawn>() { pawn, pawnLoverNonMutual }));
                            return;
                        }
                    }
                    else if (!(pawnLover is null)) // if pawn's lover also claims them as their most-liked lover (mutual)
                    {
                        // Attempt to simply claim their mutual lover's bed
                        Building_Bed loverBed = pawnLover.ownership.OwnedBed;
                        if (currentBed != loverBed)
                        {
                            if (loverBed.GetBedSlotCount() >= 2 && pawn.TryClaimBed(loverBed))
                            {
                                Message(pawn.LabelShort + " claimed the bed of their lover " + pawnLover.LabelShort + ".", new LookTargets(new List<Pawn>() { pawn, pawnLover }));
                                return;
                            }
                            else
                            {
                                // Attempt to claim a bed that has more than one sleeping spot for the mutual lovers
                                // I don't use PawnBedUtils.PerformBetterBedSearch for this due to its more custom nature
                                foreach (Building_Bed bed in bedsDescending)
                                {
                                    if (bed.GetBedSlotCount() >= 2 && RestUtility.CanUseBedEver(pawn, bed.def) && RestUtility.CanUseBedEver(pawnLover, bed.def))
                                    {
                                        bool canClaim = true;
                                        List<Pawn> otherOwners = bed.OwnersForReading.FindAll(p => p != pawn && p != pawnLover && p.CanBeUsed());
                                        List<Pawn> bootedPawns = new List<Pawn>();
                                        List<string> bootedPawnNames = new List<string>();
                                        if (otherOwners.Any())
                                        {
                                            for (int i = otherOwners.Count - 1; i >= 0; i--)
                                            {
                                                Pawn sleeper = otherOwners[i];
                                                bool bedHasOwnerWithExcludedTrait = bed.OwnersForReading.Any(p => (p.story?.traits?.allTraits?.Any(t => mutualLoverBedKickExcludedOwnerTraitDefs.Contains(t.def))).GetValueOrDefault(false));
                                                Pawn partner = sleeper.GetMostLikedLovePartner();
                                                if (!(partner is null) && (partner == pawn || partner == pawnLover) && bed.GetBedSlotCount() >= 3)
                                                    continue;
                                                else if (!bedHasOwnerWithExcludedTrait && partner is null && sleeper.TryUnclaimBed())
                                                {
                                                    bootedPawns.Add(sleeper);
                                                    bootedPawnNames.Add(sleeper.LabelShort);
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
                                            if (bootedPawns.Any())
                                                Message($"Lovers {pawn.LabelShort} and {pawnLover.LabelShort} kicked {string.Join(" and ", bootedPawnNames)} out of their bed so they could claim it together.", new LookTargets(new List<Pawn>() { pawn, pawnLover }.Concat(bootedPawns)));
                                            else
                                                Message($"Lovers {pawn.LabelShort} and {pawnLover.LabelShort} claimed an empty bed together.", new LookTargets(new List<Pawn>() { pawn, pawnLover }));
                                            return;
                                        }
                                        else if (canClaim && !(currentBed is null))
                                            _ = pawn.TryClaimBed(currentBed);
                                    }
                                }
                            }
                        }
                    }
                }

            if (ModSettings.avoidSharingPenalty) // Attempt to avoid the "Sharing bed" mood penalty (if it's not due to polyamory)
                if (thoughts.SufferingFromThought("SharedBed", out _, out _)
                    && !((pawnLover is null || pawnLover.ownership is null || pawnLover.ownership.OwnedBed != currentBed)
                    && (pawnLoverNonMutual is null || pawnLoverNonMutual.ownership is null || pawnLoverNonMutual.ownership.OwnedBed != currentBed))
                    && pawn.TryUnclaimBed())
                {
                    Message(pawn.LabelShort + " unclaimed their bed to avoid the bed sharing mood penalty", new LookTargets(new List<Pawn>() { pawn }));
                    return;
                }
        }
    }
}