using System.Collections.Generic;
using System.Linq;
using BedAssign.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace BedAssign
{
    public static class BedAssign
    {
        private static readonly TraitDef[] JealousPenaltyExcludedOwnerTraitDefs = { TraitDefOf.Jealous };

        private static readonly TraitDef[] GreedyPenaltyExcludedOwnerTraitDefs
            = { TraitDefOf.Jealous, TraitDefOf.Greedy };

        private static readonly TraitDef[] AsceticPenaltyExcludedOwnerTraitDefs = { TraitDefOf.Ascetic };
        private static readonly TraitDef[] BetterBedExcludedOwnerTraitDefs = { TraitDefOf.Jealous, TraitDefOf.Greedy };

        private static readonly TraitDef[] MutualLoverBedKickExcludedOwnerTraitDefs
            = { TraitDefOf.Jealous, TraitDefOf.Greedy };

        public static void Message(string text, LookTargets lookTargets = null)
        {
            if (!ModSettings.OutputReassignmentMessages)
                return;
            if (lookTargets is null)
                Messages.Message(text, MessageTypeDefOf.PositiveEvent);
            else
                Messages.Message(text, lookTargets, MessageTypeDefOf.PositiveEvent);
        }

        public static void Error(string text) => Log.Error(text);

        private static float GetBedRoomImpressiveness(Building_Bed b)
            => (b.GetRoom()?.GetStat(RoomStatDefOf.Impressiveness)).GetValueOrDefault(0);

        private static IOrderedEnumerable<Building_Bed> GetOrderedBedsForPawn(
            Pawn pawn, bool ascendingImpressiveness = false)
        {
            IEnumerable<Building_Bed> beds = pawn?.Map?.listerBuildings?.AllBuildingsColonistOfClass<Building_Bed>()
                                                 ?.Where(b => b.CanBeUsed());
            IOrderedEnumerable<Building_Bed> orderedBeds = ascendingImpressiveness
                ? beds.OrderBy(GetBedRoomImpressiveness)
                : beds.OrderByDescending(GetBedRoomImpressiveness);
            return orderedBeds.ThenByDescending(b => b.GetStatValueForPawn(StatDefOf.BedRestEffectiveness, pawn))
                              .ThenByDescending(b => b.GetStatValueForPawn(StatDefOf.Comfort, pawn))
                              .ThenBy(b => b.thingIDNumber);
        }

        public static void LookForBedReassignment(this Pawn pawn)
        {
            if (!pawn.CanBeUsed()) return;
            Building_Bed currentBed = pawn.ownership.OwnedBed;

            // Un-claim off-map bed to give space to other colonists
            if (currentBed != null && pawn.Map != currentBed.Map)
            {
                _ = pawn.ownership.UnclaimBed();
                Message(pawn.LabelShort + " un-claimed their bed due to being off-map.",
                        new LookTargets(new List<Pawn> { pawn }));
                currentBed = null;
            }

            // Attempt to claim forced bed
            Building_Bed forcedBed = pawn.GetForcedBed();
            if (!(forcedBed is null) && pawn.TryClaimBed(forcedBed))
            {
                if (currentBed != forcedBed)
                    Message(pawn.LabelShort + " claimed their forced bed.", new LookTargets(new List<Pawn> { pawn }));
                return;
            }
            if (pawn.Map is null) return;

            // Get the pawn's most liked lover if it's mutual
            Pawn pawnLover = pawn.GetMostLikedLovePartner();
            Pawn pawnLoverNonMutual = pawnLover;
            if (pawnLover?.GetMostLikedLovePartner() != pawn)
                pawnLover = null;

            // Get all of the pawn's mood-related thoughts
            List<Thought> thoughts = new List<Thought>();
            pawn.needs?.mood?.thoughts?.GetAllMoodThoughts(thoughts);

            // Get all of the pawn's lover's mood-related thoughts
            List<Thought> thoughtsLover = new List<Thought>();
            pawnLover?.needs?.mood?.thoughts?.GetAllMoodThoughts(thoughtsLover);
            IOrderedEnumerable<Building_Bed> orderedBeds = null;
            if (ModSettings.AvoidJealousPenalty) // Attempt to avoid the Jealous mood penalty
                if (thoughts.SufferingFromThought("Jealous", out _, out _))
                {
                    orderedBeds = GetOrderedBedsForPawn(pawn);
                    if (PawnBedUtils.PerformBetterBedSearch(orderedBeds, currentBed, pawn, pawnLover,
                                                            pawn.LabelShort
                                                          + " claimed a better bed to avoid the Jealous mood penalty.",
                                                            $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a better bed together so {pawn.LabelShort} could avoid the Jealous mood penalty.",
                                                            TraitDefOf.Jealous, delegate(Building_Bed bed)
                                                            {
                                                                float bedImpressiveness = GetBedRoomImpressiveness(bed);
                                                                foreach (Pawn p in pawn.Map.mapPawns
                                                                           ?.SpawnedPawnsInFaction(Faction.OfPlayer))
                                                                    if (p.HostFaction is null
                                                                     && (p.RaceProps?.Humanlike).GetValueOrDefault(
                                                                            false) && !(p.ownership is null))
                                                                    {
                                                                        float pImpressiveness
                                                                            = (p.ownership?.OwnedRoom?.GetStat(
                                                                                RoomStatDefOf.Impressiveness))
                                                                           .GetValueOrDefault(0);
                                                                        if (pImpressiveness - bedImpressiveness
                                                                         >= Mathf.Abs(bedImpressiveness * 0.1f))
                                                                            return false;
                                                                    }
                                                                return true;
                                                            }, JealousPenaltyExcludedOwnerTraitDefs))
                        return;
                }
            if (ModSettings.AvoidGreedyPenalty) // Attempt to avoid the Greedy mood penalty
                if (thoughts.SufferingFromThought("Greedy", out Thought greedyThought, out float currentBaseMoodEffect))
                {
                    orderedBeds = orderedBeds ?? GetOrderedBedsForPawn(pawn);
                    if (PawnBedUtils.PerformBetterBedSearch(orderedBeds, currentBed, pawn, pawnLover,
                                                            pawn.LabelShort
                                                          + " claimed a better bed to avoid the Greedy mood penalty.",
                                                            $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a better bed together so {pawn.LabelShort} could avoid the Greedy mood penalty.",
                                                            TraitDefOf.Greedy, delegate(Building_Bed bed)
                                                            {
                                                                int stage = RoomStatDefOf.Impressiveness
                                                                               .GetScoreStageIndex(
                                                                                    GetBedRoomImpressiveness(bed))
                                                                          + 1;
                                                                float? bedBaseMoodEffect = greedyThought.def
                                                                  ?.stages?[stage]?.baseMoodEffect;
                                                                return bedBaseMoodEffect > currentBaseMoodEffect;
                                                            }, GreedyPenaltyExcludedOwnerTraitDefs))
                        return;
                }
            if (ModSettings.AvoidAsceticPenalty) // Attempt to avoid the Ascetic mood penalty
                if (thoughts.SufferingFromThought("Ascetic", out Thought asceticThought,
                                                  out float currentBaseMoodEffect) &&
                    (pawnLover is null
                  || !(thoughtsLover?.Find(t => t.def?.defName
                                             == "Ascetic") is null))) // lover should also have Ascetic
                {
                    IOrderedEnumerable<Building_Bed> orderedBedsAscetic = GetOrderedBedsForPawn(pawn, true);
                    if (PawnBedUtils.PerformBetterBedSearch(orderedBedsAscetic, currentBed, pawn, pawnLover,
                                                            pawn.LabelShort
                                                          + " claimed a worse bed to avoid the Ascetic mood penalty.",
                                                            $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a worse bed together so they could both avoid the Ascetic mood penalty.",
                                                            TraitDefOf.Ascetic, delegate(Building_Bed bed)
                                                            {
                                                                int stage = RoomStatDefOf.Impressiveness
                                                                               .GetScoreStageIndex(
                                                                                    GetBedRoomImpressiveness(bed))
                                                                          + 1;
                                                                float? bedBaseMoodEffect = asceticThought.def
                                                                  ?.stages?[stage]?.baseMoodEffect;
                                                                return bedBaseMoodEffect > currentBaseMoodEffect;
                                                            }, AsceticPenaltyExcludedOwnerTraitDefs))
                        return;
                }
            if (ModSettings.ClaimBetterBeds) // Attempt to claim a better empty bed
                if (!(pawnLover is null && !(pawnLoverNonMutual is null)
                                        && pawnLoverNonMutual.ownership?.OwnedBed?.CompAssignableToPawn
                                                             .MaxAssignedPawnsCount
                                         > 2)) // if not a third wheel in a polyamorous relationship involving bigger beds
                {
                    orderedBeds = orderedBeds ?? GetOrderedBedsForPawn(pawn);
                    if (PawnBedUtils.PerformBetterBedSearch(orderedBeds, currentBed, pawn, pawnLover,
                                                            pawn.LabelShort + " claimed a better empty bed.",
                                                            $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a better empty bed together.",
                                                            excludedOwnerTraitDefs: BetterBedExcludedOwnerTraitDefs))
                        return;
                }
            if (ModSettings.AvoidPartnerPenalty) // Attempt to avoid the "Want to sleep with partner" mood penalty
                if (thoughts.SufferingFromThought("WantToSleepWithSpouseOrLover", out _, out _)
                 && (!(pawnLover is null) || !(pawnLoverNonMutual is null)))
                {
                    if (pawnLover is null) // if pawn only has a polyamorous lover
                    {
                        // Attempt to claim their polyamorous lover's bed if there's extra space
                        Building_Bed loverNonMutualBed = pawnLoverNonMutual.ownership?.OwnedBed;
                        if (currentBed != loverNonMutualBed && pawn.TryClaimBed(loverNonMutualBed, false))
                        {
                            Message(pawn.LabelShort + " claimed the bed of their polyamorous lover "
                                                    + pawnLoverNonMutual.LabelShort + ".",
                                    new LookTargets(new List<Pawn> { pawn, pawnLoverNonMutual }));
                            return;
                        }
                    }
                    else
                    {
                        // Attempt to simply claim their mutual lover's bed
                        Building_Bed loverBed = pawnLover.ownership.OwnedBed;
                        if (currentBed != loverBed)
                        {
                            if (loverBed.CompAssignableToPawn.MaxAssignedPawnsCount >= 2 && pawn.TryClaimBed(loverBed))
                            {
                                Message(
                                    pawn.LabelShort + " claimed the bed of their lover " + pawnLover.LabelShort + ".",
                                    new LookTargets(new List<Pawn> { pawn, pawnLover }));
                                return;
                            }
                            // Attempt to claim a bed that has more than one sleeping spot for the mutual lovers
                            // I don't use PawnBedUtils.PerformBetterBedSearch for this due to its more custom nature
                            orderedBeds = orderedBeds ?? GetOrderedBedsForPawn(pawn);
                            foreach (Building_Bed bed in orderedBeds)
                                if (bed.CompAssignableToPawn.MaxAssignedPawnsCount >= 2
                                 && RestUtility.CanUseBedEver(pawn, bed.def)
                                 && RestUtility.CanUseBedEver(pawnLover, bed.def))
                                {
                                    bool canClaim = true;
                                    IEnumerable<Pawn> otherOwners
                                        = bed.OwnersForReading.Where(p => p != pawn && p != pawnLover && p.CanBeUsed());
                                    List<Pawn> bootedPawns = new List<Pawn>();
                                    List<string> bootedPawnNames = new List<string>();
                                    if (otherOwners.Any())
                                    {
                                        bool bedHasOwnerWithExcludedTrait = bed.OwnersForReading.Any(
                                            p => (p.story?.traits?.allTraits?
                                                   .Any(t => MutualLoverBedKickExcludedOwnerTraitDefs.Contains(t.def)))
                                               .GetValueOrDefault(false));
                                        foreach (Pawn sleeper in otherOwners)
                                        {
                                            Pawn partner = sleeper.GetMostLikedLovePartner();
                                            if (!(partner is null) && (partner == pawn || partner == pawnLover)
                                                                   && bed.CompAssignableToPawn.MaxAssignedPawnsCount
                                                                   >= 3)
                                                continue;
                                            if (!bedHasOwnerWithExcludedTrait && partner is null
                                             && sleeper.TryUnClaimBed())
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
                                    switch (canClaim)
                                    {
                                        case true when pawn.TryClaimBed(bed) && pawnLover.TryClaimBed(bed):
                                        {
                                            if (bootedPawns.Any())
                                                Message(
                                                    $"Lovers {pawn.LabelShort} and {pawnLover.LabelShort} kicked {string.Join(" and ", bootedPawnNames)} out of their bed so they could claim it together.",
                                                    new LookTargets(
                                                        new List<Pawn> { pawn, pawnLover }.Concat(bootedPawns)));
                                            else
                                                Message(
                                                    $"Lovers {pawn.LabelShort} and {pawnLover.LabelShort} claimed an empty bed together.",
                                                    new LookTargets(new List<Pawn> { pawn, pawnLover }));
                                            return;
                                        }
                                        case true when !(currentBed is null):
                                            _ = pawn.TryClaimBed(currentBed);
                                            break;
                                    }
                                }
                        }
                    }
                }
            if (!ModSettings
                   .AvoidSharingPenalty) // Attempt to avoid the "Sharing bed" mood penalty (if it's not due to polyamory)
                return;
            if (thoughts.SufferingFromThought("SharedBed", out _, out _)
             && !((pawnLover?.ownership is null || pawnLover.ownership.OwnedBed != currentBed)
               && (pawnLoverNonMutual?.ownership is null || pawnLoverNonMutual.ownership.OwnedBed != currentBed))
             && pawn.TryUnClaimBed())
                Message(pawn.LabelShort + " unclaimed their bed to avoid the bed sharing mood penalty",
                        new LookTargets(new List<Pawn> { pawn }));
        }
    }
}