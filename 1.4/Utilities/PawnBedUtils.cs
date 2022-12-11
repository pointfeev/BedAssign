using RimWorld;

using System;
using System.Collections.Generic;
using System.Linq;

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

        public static bool IsBetterThan(this Building_Bed bed, Building_Bed currentBed, Pawn pawn)
        {
            if (bed is null)
                return false;
            if (currentBed is null)
                return true;
            Room room = bed.GetRoom();
            Room currentRoom = currentBed.GetRoom();
            if (!(room is null) && currentRoom is null)
                return true; // Prioritize beds with rooms
            if (!(room is null) && !(currentRoom is null))
            {
                float impressiveness = room.GetStat(RoomStatDefOf.Impressiveness);
                float currentImpressiveness = currentRoom.GetStat(RoomStatDefOf.Impressiveness);
                if (impressiveness - currentImpressiveness > ModSettings.betterBedRoomImpressivenessThreshold)
                    return true; // ... then bed room impressiveness
            }
            float rest = bed.GetStatValueForPawn(StatDefOf.BedRestEffectiveness, pawn);
            float currentRest = currentBed.GetStatValueForPawn(StatDefOf.BedRestEffectiveness, pawn);
            if (rest - currentRest > 0)
                return true; // ... then bed rest effectiveness
            float comfort = bed.GetStatValueForPawn(StatDefOf.Comfort, pawn);
            float currentComfort = currentBed.GetStatValueForPawn(StatDefOf.Comfort, pawn);
            if (comfort - currentComfort > 0)
                return true; // ... then bed comfort
            return false;
        }

        public static bool PerformBetterBedSearch(IOrderedEnumerable<Building_Bed> orderedBeds, Building_Bed currentBed,
            Pawn pawn, Pawn pawnLover, string singleOutput, string partnerOutput,
            TraitDef forTraitDef = null, Func<Building_Bed, bool> betterBedCustomFunc = null,
            TraitDef[] excludedOwnerTraitDefs = null)
        {
            if (!(forTraitDef is null) && !(pawn.story?.traits?.HasTrait(forTraitDef)).GetValueOrDefault(false))
                return false;

            bool canIgnoreLover = !(forTraitDef is null);

            bool IsBetter(Building_Bed bed) => !(betterBedCustomFunc is null) && betterBedCustomFunc.Invoke(bed) // use the custom function if one was supplied
                || bed.IsBetterThan(currentBed, pawn); // default IsBetterThan method (bed room impressiveness & bed stats)

            // ... with their lover
            if (!(pawnLover is null))
            {
                foreach (Building_Bed bed in orderedBeds)
                {
                    if (bed != currentBed && bed.IsExcluded(pawn, pawnLover, forTraitDef, excludedOwnerTraitDefs))
                        continue;

                    Building_Bed pawnLoverCurrentBed = pawnLover?.ownership?.OwnedBed;
                    if (bed.GetBedSlotCount() >= 2 && IsBetter(bed) && pawn.TryClaimBed(bed) && pawnLover.TryClaimBed(bed))
                    {
                        if (bed != currentBed || bed != pawnLoverCurrentBed)
                            BedAssign.Message(partnerOutput, new LookTargets(new List<Pawn>() { pawn, pawnLover }));
                        return true;
                    }
                    else if (!(currentBed is null))
                        _ = pawn.TryClaimBed(currentBed);
                }
                if (!canIgnoreLover)
                    return false;
            }

            // ... for themself
            foreach (Building_Bed bed in orderedBeds)
            {
                if (bed != currentBed && bed.IsExcluded(pawn, pawnLover, forTraitDef, excludedOwnerTraitDefs))
                    continue;

                if (IsBetter(bed) && pawn.TryClaimBed(bed))
                {
                    if (bed != currentBed)
                        BedAssign.Message(singleOutput, new LookTargets(new List<Pawn>() { pawn }));
                    return true;
                }
            }

            return false;
        }

        private static bool IsExcluded(this Building_Bed bed, Pawn pawn, Pawn pawnLover = null, TraitDef forTraitDef = null, TraitDef[] excludedOwnerTraitDefs = null)
        {
            List<Pawn> bedOwners = bed.OwnersForReading;

            bool bedOwned = bedOwners.Any();
            if (bedOwned && !(forTraitDef is null))
                bedOwned = bedOwners.Any(p => p != pawn && p != pawnLover && p.CanBeUsed()
                    && (p.story?.traits?.HasTrait(forTraitDef)).GetValueOrDefault(false));
            if (bedOwned) return true;

            bool bedHasOwnerWithExcludedTrait = false;
            if (!(excludedOwnerTraitDefs is null) && excludedOwnerTraitDefs.Any())
                bedHasOwnerWithExcludedTrait = bedOwners.Any(p => p != pawn && p != pawnLover && p.CanBeUsed()
                    && (p.story?.traits?.allTraits?.Any(t => excludedOwnerTraitDefs.Contains(t.def))).GetValueOrDefault(false));
            return bedHasOwnerWithExcludedTrait;
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