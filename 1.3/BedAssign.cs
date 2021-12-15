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
            //Log.Message("" + text);
            if (lookTargets is null) Messages.Message(text, MessageTypeDefOf.PositiveEvent);
            else Messages.Message(text, lookTargets, MessageTypeDefOf.PositiveEvent);
        }

        public static void Error(string text) => Log.Error("" + text);

        public static List<Building_Bed> GetSortedBedsOnPawnsMap(Pawn pawn, bool descending = true)
        {
            List<Building_Bed> bedsSorted = pawn?.Map?.listerBuildings?.AllBuildingsColonistOfClass<Building_Bed>().ToList();
            if (bedsSorted is null) bedsSorted = new List<Building_Bed>();

            bedsSorted.RemoveAll(bed => bed is null || !bed.CanBeUsed());
            int bed1IsBetter = descending ? -1 : 1;
            int bed2IsBetter = descending ? 1 : -1;
            bedsSorted.Sort(delegate (Building_Bed bed1, Building_Bed bed2)
            {
                return bed1.IsBetterThan(bed2) ? bed1IsBetter : bed2IsBetter;
            });
            return bedsSorted;
        }

        private static TraitDef[] mutualLoverBedKickExcludedOwnerTraitDefs = new TraitDef[] { TraitDefOf.Jealous, TraitDefOf.Greedy };

        public static void LookForBedReassignment(this Pawn pawn)
        {
            if (!pawn.CanBeUsed()) return;

            // Unclaim off-map bed to give space to other colonists
            Building_Bed currentBed = pawn.ownership.OwnedBed;
            if (currentBed != null && pawn.Map != currentBed.Map)
            {
                pawn.ownership.UnclaimBed();
                Message(pawn.LabelShort + " unclaimed their bed due to being off-map.", new LookTargets(new List<Pawn>() { pawn }));
                currentBed = null;
            }

            // Attempt to claim forced bed
            Building_Bed forcedBed = pawn.GetForcedBed();
            if (!(forcedBed is null) && pawn.TryClaimBed(forcedBed))
            {
                if (currentBed != forcedBed) Message(pawn.LabelShort + " claimed their forced bed.", new LookTargets(new List<Pawn>() { pawn }));
                return;
            }

            if (pawn.Map is null) return;

            // Get the pawn's most liked lover if it's mutual
            Pawn pawnLover = pawn.GetMostLikedLovePartner();
            Pawn pawnLoverNonMutual = pawnLover;
            if (pawnLover?.GetMostLikedLovePartner() != pawn) pawnLover = null;

            // Get all beds on the pawn's map, sorted
            List<Building_Bed> bedsDescending = GetSortedBedsOnPawnsMap(pawn);
            List<Building_Bed> bedsAscending = GetSortedBedsOnPawnsMap(pawn, false);

            // Get all of the pawn's mood-related thoughts
            List<Thought> thoughts = new List<Thought>();
            pawn.needs?.mood?.thoughts?.GetAllMoodThoughts(thoughts);

            // Get all of the pawn's lover's mood-related thoughts
            List<Thought> thoughtsLover = new List<Thought>();
            pawnLover?.needs?.mood?.thoughts?.GetAllMoodThoughts(thoughtsLover);

            // Attempt to avoid the Jealous mood penalty
            if (thoughts.SufferingFromThought("Jealous", out _, out _) &&
                PawnBedUtils.PerformBetterBedSearch(bedsDescending, currentBed, pawn, pawnLover,
                singleOutput: pawn.LabelShort + " claimed a better bed to avoid the Jealous mood penalty.",
                partnerOutput: $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a better bed together so {pawn.LabelShort} could avoid the Jealous mood penalty.",
                forTraitDef: TraitDefOf.Jealous, forTraitDefFunc_DoesBedSatisfy: delegate (Building_Bed bed)
                {
                    float bedImpressiveness = (bed.GetRoom()?.GetStat(RoomStatDefOf.Impressiveness)).GetValueOrDefault(0);
                    foreach (Pawn p in pawn.Map.mapPawns?.SpawnedPawnsInFaction(Faction.OfPlayer))
                    {
                        if (p.HostFaction is null && (p.RaceProps?.Humanlike).GetValueOrDefault(false) && !(p.ownership is null))
                        {
                            float pImpressiveness = (p.ownership?.OwnedRoom?.GetStat(RoomStatDefOf.Impressiveness)).GetValueOrDefault(0);
                            if (pImpressiveness - bedImpressiveness >= Mathf.Abs(bedImpressiveness * 0.1f)) return false;
                        }
                    }
                    return true;
                }, excludedOwnerTraitDefs: new TraitDef[] { TraitDefOf.Jealous }))
                return;

            // Attempt to avoid the Greedy mood penalty
            if (thoughts.SufferingFromThought("Greedy", out Thought greedyThought, out float currentBaseMoodEffect) &&
                PawnBedUtils.PerformBetterBedSearch(bedsDescending, currentBed, pawn, pawnLover,
                singleOutput: pawn.LabelShort + " claimed a better bed to avoid the Greedy mood penalty.",
                partnerOutput: $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a better bed together so {pawn.LabelShort} could avoid the Greedy mood penalty.",
                forTraitDef: TraitDefOf.Greedy, forTraitDefFunc_DoesBedSatisfy: delegate (Building_Bed bed)
                {
                    float impressiveness = (bed.GetRoom()?.GetStat(RoomStatDefOf.Impressiveness)).GetValueOrDefault(0);
                    int stage = RoomStatDefOf.Impressiveness.GetScoreStageIndex(impressiveness) + 1;
                    float? bedBaseMoodEffect = greedyThought.def?.stages?[stage]?.baseMoodEffect;
                    return bedBaseMoodEffect.HasValue && bedBaseMoodEffect.Value > currentBaseMoodEffect;
                }, excludedOwnerTraitDefs: new TraitDef[] { TraitDefOf.Jealous, TraitDefOf.Greedy }))
                return;

            // Attempt to avoid the Ascetic mood penalty
            if (thoughts.SufferingFromThought("Ascetic", out Thought asceticThought, out currentBaseMoodEffect) &&
                (pawnLover is null || !(thoughtsLover?.Find(t => t.def?.defName == "Ascetic") is null)) && // lover should also have Ascetic
                PawnBedUtils.PerformBetterBedSearch(bedsAscending, currentBed, pawn, pawnLover,
                singleOutput: pawn.LabelShort + " claimed a worse bed to avoid the Ascetic mood penalty.",
                partnerOutput: $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a worse bed together so they could both avoid the Ascetic mood penalty.",
                forTraitDef: TraitDefOf.Ascetic, forTraitDefFunc_DoesBedSatisfy: delegate (Building_Bed bed)
                {
                    float impressiveness = (bed.GetRoom()?.GetStat(RoomStatDefOf.Impressiveness)).GetValueOrDefault(0);
                    int stage = RoomStatDefOf.Impressiveness.GetScoreStageIndex(impressiveness) + 1;
                    float? bedBaseMoodEffect = asceticThought.def?.stages?[stage]?.baseMoodEffect;
                    return bedBaseMoodEffect.HasValue && bedBaseMoodEffect.Value > currentBaseMoodEffect;
                }, excludedOwnerTraitDefs: new TraitDef[] { TraitDefOf.Ascetic }))
                return;

            // Attempt to claim a better empty bed
            if (PawnBedUtils.PerformBetterBedSearch(bedsDescending, currentBed, pawn, pawnLover,
                singleOutput: pawn.LabelShort + " claimed a better empty bed.",
                partnerOutput: $"Lovers {pawn.LabelShort} and {pawnLover?.LabelShort} claimed a better empty bed together.",
                excludedOwnerTraitDefs: new TraitDef[] { TraitDefOf.Jealous, TraitDefOf.Greedy }))
                return;

            // Attempt to avoid the "Want to sleep with partner" mood penalty
            if (thoughts.SufferingFromThought("WantToSleepWithSpouseOrLover", out _, out _) &&
                (!(pawnLover is null) || !(pawnLoverNonMutual is null))) // must have a lover (obviously)
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
                        if (pawn.TryClaimBed(loverBed))
                        {
                            Message(pawn.LabelShort + " claimed the bed of their lover " + pawnLover.LabelShort + ".", new LookTargets(new List<Pawn>() { pawn, pawnLover }));
                            return;
                        }
                        else
                        {
                            // Attempt to claim a bed that has more than one sleeping spot for the mutual lovers
                            foreach (Building_Bed bed in bedsDescending)
                            {
                                if (!bed.Medical && bed.GetBedSlotCount() >= 2 && RestUtility.CanUseBedEver(pawn, bed.def) && RestUtility.CanUseBedEver(pawnLover, bed.def))
                                {
                                    bool canClaim = true;
                                    List<Pawn> otherOwners = bed.OwnersForReading.FindAll(p => p != pawn && p != pawnLover && p.CanBeUsed());
                                    List<Pawn> bootedPawns = new List<Pawn>();
                                    List<string> bootedPawnNames = new List<string>();
                                    if (otherOwners.Any())
                                    {
#pragma warning disable CS0162 // Unreachable code detected? I think not. Bad Visual Studio, bad!
                                        for (int i = otherOwners.Count - 1; i >= 0; i--)
#pragma warning restore CS0162
                                        {
                                            Pawn sleeper = otherOwners[i];
                                            bool bedHasOwnerWithExcludedTrait = bed.OwnersForReading.Any(p => (p.story?.traits?.allTraits?.Any(t => mutualLoverBedKickExcludedOwnerTraitDefs.Contains(t.def))).GetValueOrDefault(false));
                                            if (!bedHasOwnerWithExcludedTrait && sleeper.GetMostLikedLovePartner() is null && sleeper.TryUnclaimBed())
                                            {
                                                bootedPawns.Add(sleeper);
                                                bootedPawnNames.Add(sleeper.LabelShort);
                                                return;
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
                                        if (bootedPawns.Any()) Message($"Lovers {pawn.LabelShort} and {pawnLover.LabelShort} kicked {string.Join(", ", bootedPawnNames)} out of their bed so they could claim it together.", new LookTargets(new List<Pawn>() { pawn, pawnLover }.Concat(bootedPawns)));
                                        else Message($"Lovers {pawn.LabelShort} and {pawnLover.LabelShort} claimed an empty bed together.", new LookTargets(new List<Pawn>() { pawn, pawnLover }));
                                        return;
                                    }
                                    else if (canClaim && !(currentBed is null)) pawn.TryClaimBed(currentBed);
                                }
                            }
                        }
                    }
                }
            }

            // Attempt to avoid the "Sharing bed" mood penalty
            if (thoughts.SufferingFromThought("SharedBed", out _, out _) && pawn.TryUnclaimBed())
            {
                Message(pawn.LabelShort + " unclaimed their bed to avoid the bed sharing mood penalty", new LookTargets(new List<Pawn>() { pawn }));
                return;
            }
        }
    }
}