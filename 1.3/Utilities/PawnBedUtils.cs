using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld;

using Verse;

namespace BedAssign
{
    public static class PawnBedUtils
    {
        public static int GetBedSlotCount(this Building_Bed bed) => Math.Max(bed.SleepingSlotsCount, bed.TotalSleepingSlots);

        public static bool CanBeUsed(this Pawn pawn) => !(pawn is null) && !(pawn.ownership is null) &&
                !(pawn.Faction is null) && pawn.Faction.IsPlayer && pawn.IsFreeNonSlaveColonist &&
                !(pawn.def is null) && !(pawn.def.race is null) && pawn.def.race.Humanlike;

        public static bool IsDesignatedDeconstructOrUninstall(this Building building) => building is null || building.Map is null || building.Map.designationManager is null ||
                building.Map.designationManager.AllDesignationsOn(building).ToList().Any(designation => !(designation is null) &&
                (designation.def == DesignationDefOf.Deconstruct || designation.def == DesignationDefOf.Uninstall));

        public static bool CanBeUsed(this Building_Bed bed) => CanBeUsedEver(bed) && !BedAssignData.UnusableBeds.Contains(bed) && !bed.IsDesignatedDeconstructOrUninstall();

        public static bool CanBeUsedEver(this Building_Bed bed) => !(bed is null) && !bed.IsHospitalityGuestBed() &&
                !bed.Medical && bed.ForColonists && bed.def.building.bed_humanlike;

        public static bool IsBetterThan(this Building_Bed bed1, Building_Bed bed2)
        {
            bool bed1IsBetter = true;
            bool bed2IsBetter = false;

            if (bed1 is null && !(bed2 is null))
            {
                return bed2IsBetter;
            }
            else if (bed2 is null && !(bed1 is null))
            {
                return bed1IsBetter;
            }

            // Prioritize bedrooms
            Room room1 = bed1.GetRoom();
            Room room2 = bed2.GetRoom();
            if (room1 is null && !(room2 is null))
            {
                return bed2IsBetter;
            }
            else if (room2 is null && !(room1 is null))
            {
                return bed1IsBetter;
            }

            // ... then bedroom impressiveness
            float impressive1 = room1.GetStat(RoomStatDefOf.Impressiveness);
            float impressive2 = room2.GetStat(RoomStatDefOf.Impressiveness);
            if (impressive1 < impressive2)
            {
                return bed2IsBetter;
            }
            else if (impressive1 > impressive2)
            {
                return bed1IsBetter;
            }

            // ... then bed rest effectiveness
            float effect1 = bed1.GetStatValue(StatDefOf.BedRestEffectiveness);
            float effect2 = bed2.GetStatValue(StatDefOf.BedRestEffectiveness);
            if (effect1 < effect2)
            {
                return bed2IsBetter;
            }
            else if (effect1 > effect2)
            {
                return bed1IsBetter;
            }

            // ... then bed comfort
            float comfort1 = bed1.GetStatValue(StatDefOf.Comfort);
            float comfort2 = bed2.GetStatValue(StatDefOf.Comfort);
            if (comfort1 < comfort2)
            {
                return bed2IsBetter;
            }
            else if (comfort1 > comfort2)
            {
                return bed1IsBetter;
            }

            // ... then bed beauty
            float beauty1 = bed1.GetStatValue(StatDefOf.Beauty);
            float beauty2 = bed2.GetStatValue(StatDefOf.Beauty);
            if (beauty1 < beauty2)
            {
                return bed2IsBetter;
            }
            else if (beauty1 > beauty2)
            {
                return bed1IsBetter;
            }

            // ... otherwise, use ID numbers for consistent sorting
            return (bed1.thingIDNumber < bed2.thingIDNumber) ? bed1IsBetter : bed2IsBetter;
        }

        public static bool PerformBetterBedSearch(List<Building_Bed> bedsSorted, Building_Bed currentBed,
            Pawn pawn, Pawn pawnLover, string singleOutput, string partnerOutput,
            TraitDef forTraitDef = null, Func<Building_Bed, bool> forTraitDefFunc_DoesBedSatisfy = null,
            TraitDef[] excludedOwnerTraitDefs = null)
        {
            if (!(forTraitDef is null) && !(pawn.story?.traits?.HasTrait(forTraitDef)).GetValueOrDefault(false))
            {
                return false;
            }

            bool canIgnoreLover = !(forTraitDef is null);

            // ... with their lover
            if (!(pawnLover is null))
            {
                foreach (Building_Bed bed in bedsSorted)
                {
                    if (IsBedExcluded(bed, pawn, forTraitDef, excludedOwnerTraitDefs))
                    {
                        continue;
                    }

                    bool isBedBetter = !(forTraitDefFunc_DoesBedSatisfy is null) && forTraitDefFunc_DoesBedSatisfy.Invoke(bed) || bed.IsBetterThan(currentBed);
                    if (bed.GetBedSlotCount() >= 2 && isBedBetter && pawn.TryClaimBed(bed) && pawnLover.TryClaimBed(bed))
                    {
                        BedAssign.Message(partnerOutput, new LookTargets(new List<Pawn>() { pawn, pawnLover }));
                        return true;
                    }
                    else if (!(currentBed is null))
                    {
                        pawn.TryClaimBed(currentBed);
                    }
                }
                if (!canIgnoreLover)
                {
                    return false;
                }
            }

            // ... for themself
            foreach (Building_Bed bed in bedsSorted)
            {
                if (IsBedExcluded(bed, pawn, forTraitDef, excludedOwnerTraitDefs))
                {
                    continue;
                }

                bool isBedBetter = !(forTraitDefFunc_DoesBedSatisfy is null) && forTraitDefFunc_DoesBedSatisfy.Invoke(bed) || bed.IsBetterThan(currentBed);
                if (isBedBetter && pawn.TryClaimBed(bed))
                {
                    BedAssign.Message(singleOutput, new LookTargets(new List<Pawn>() { pawn }));
                    return true;
                }
            }

            return false;
        }

        private static bool IsBedExcluded(Building_Bed bed, Pawn pawn, TraitDef forTraitDef, TraitDef[] excludedOwnerTraitDefs)
        {
            bool bedOwned = bed.OwnersForReading.Any();
            if (bedOwned && !(forTraitDef is null))
            {
                bedOwned = bed.OwnersForReading.Any(p => p != pawn && p.CanBeUsed() &&
                (p.story?.traits?.HasTrait(forTraitDef)).GetValueOrDefault(false));
            }

            if (bedOwned)
            {
                return true;
            }

            bool bedHasOwnerWithExcludedTrait = false;
            if (!(excludedOwnerTraitDefs is null) && excludedOwnerTraitDefs.Any())
            {
                bedHasOwnerWithExcludedTrait = bed.OwnersForReading.Any(p => p != pawn && p.CanBeUsed() &&
                (p.story?.traits?.allTraits?.Any(t => excludedOwnerTraitDefs.Contains(t.def))).GetValueOrDefault(false));
            }

            if (bedHasOwnerWithExcludedTrait)
            {
                return true;
            }

            return false;
        }

        public static bool SufferingFromThought(this List<Thought> thoughts, string thoughtDefName, out Thought thought, out float currentBaseMoodEffect)
        {
            thought = thoughts.FindThought(thoughtDefName);
            currentBaseMoodEffect = (thought?.CurStage?.baseMoodEffect).GetValueOrDefault(0);
            return currentBaseMoodEffect < 0;
        }

        public static Thought FindThought(this List<Thought> thoughts, string thoughtDefName) => thoughts?.Find(t => t.def?.defName == thoughtDefName);

        public static Thought FindThought(this Pawn pawn, string thoughtDefName)
        {
            List<Thought> thoughts = new List<Thought>();
            pawn.needs?.mood?.thoughts?.GetAllMoodThoughts(thoughts);
            return thoughts.FindThought(thoughtDefName);
        }
    }
}