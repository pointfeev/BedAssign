using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BedAssign.Utilities;

public static class PawnBedUtils
{
    public static bool CanBeUsed(this Pawn pawn) => pawn is { ownership: { }, Faction.IsPlayer: true, IsFreeColonist: true, def.race.Humanlike: true };

    private static bool IsDesignatedDeconstructOrUninstall(this Building building)
        => building?.MapHeld?.designationManager is null || building.MapHeld.designationManager.AllDesignationsOn(building).Any(designation
            => designation is not null && (designation.def == DesignationDefOf.Deconstruct || designation.def == DesignationDefOf.Uninstall));

    public static bool CanBeUsed(this Building_Bed bed)
        => CanBeUsedEver(bed) && !BedAssignData.UnusableBeds.Contains(bed) && !bed.IsDesignatedDeconstructOrUninstall();

    public static bool CanBeUsedEver(this Building_Bed bed)
        => bed is not null && !bed.IsHospitalityGuestBed() && !bed.Medical && !bed.ForPrisoners && bed.def.building.bed_humanlike;

    private static bool IsBetterThan(this Building_Bed bed, Building_Bed currentBed)
    {
        if (bed is null)
            return false;
        if (currentBed is null)
            return true;
        if (bed == currentBed)
            return false;
        Room room = bed.GetRoom();
        Room currentRoom = currentBed.GetRoom();
        if (room is not null)
        {
            if (currentRoom is null)
                return true; // Prioritize beds with rooms
            int impressiveness = Convert.ToInt32(room.GetStat(RoomStatDefOf.Impressiveness));
            int currentImpressiveness = Convert.ToInt32(currentRoom.GetStat(RoomStatDefOf.Impressiveness));
            if (impressiveness - currentImpressiveness > ModSettings.BetterBedRoomImpressivenessThreshold)
                return true; // ... then bed room impressiveness
        }
        int rest = Convert.ToInt32(bed.GetStatValue(StatDefOf.BedRestEffectiveness));
        int currentRest = Convert.ToInt32(currentBed.GetStatValue(StatDefOf.BedRestEffectiveness));
        if (rest - currentRest > 0)
            return true; // ... then bed rest effectiveness
        int comfort = Convert.ToInt32(bed.GetStatValue(StatDefOf.Comfort));
        int currentComfort = Convert.ToInt32(currentBed.GetStatValue(StatDefOf.Comfort));
        return comfort - currentComfort > 0; // ... then bed comfort
    }

    public static bool PerformBetterBedSearch(IOrderedEnumerable<Building_Bed> orderedBeds, Building_Bed currentBed, Pawn pawn, Pawn pawnLover,
        string singleOutput, string partnerOutput, TraitDef forTraitDef = null, Func<Building_Bed, bool> betterBedCustomFunc = null,
        TraitDef[] excludedOwnerTraitDefs = null)
    {
        if (forTraitDef is not null && !(pawn.story?.traits?.HasTrait(forTraitDef)).GetValueOrDefault(false))
            return false;
        bool canIgnoreLover = forTraitDef is not null;
        bool IsBetter(Building_Bed bed)
            => betterBedCustomFunc is not null && betterBedCustomFunc.Invoke(bed) // use the custom function if one was supplied
            || bed.IsBetterThan(currentBed); // default IsBetterThan method (bed room impressiveness & bed stats)

        // ... with their lover
        if (pawnLover is not null)
        {
            Building_Bed pawnLoverCurrentBed = pawnLover.ownership?.OwnedBed;
            foreach (Building_Bed bed in orderedBeds)
            {
                if (bed.CompAssignableToPawn.MaxAssignedPawnsCount < 2)
                    continue;
                if (bed != currentBed && bed.IsExcluded(pawn, pawnLover, forTraitDef, excludedOwnerTraitDefs))
                    continue;
                if (!IsBetter(bed) || !pawn.TryClaimBed(bed) || !pawnLover.TryClaimBed(bed))
                {
                    if (currentBed is not null)
                        _ = pawn.TryClaimBed(currentBed);
                    continue;
                }
                if (bed != currentBed || bed != pawnLoverCurrentBed)
                    BedAssign.Message(partnerOutput, new(new List<Pawn> { pawn, pawnLover }));
                return true;
            }
            if (!canIgnoreLover)
                return false;
        }

        // ... for themself
        foreach (Building_Bed bed in orderedBeds)
        {
            if (bed != currentBed && bed.IsExcluded(pawn, forTraitDef: forTraitDef, excludedOwnerTraitDefs: excludedOwnerTraitDefs))
                continue;
            if (!IsBetter(bed) || !pawn.TryClaimBed(bed))
                continue;
            if (bed != currentBed)
                BedAssign.Message(singleOutput, new(new List<Pawn> { pawn }));
            return true;
        }
        return false;
    }

    private static bool IsExcluded(this Building_Bed bed, Pawn pawn, Pawn pawnLover = null, TraitDef forTraitDef = null,
        TraitDef[] excludedOwnerTraitDefs = null)
    {
        IEnumerable<Pawn> bedOwners = bed.CompAssignableToPawn.AssignedPawns.ToList();
        bool bedOwned = bedOwners.Any();
        if (bedOwned && forTraitDef is not null)
            bedOwned = bedOwners.Any(p => p != pawn && p != pawnLover && p.CanBeUsed() && (p.story?.traits?.HasTrait(forTraitDef)).GetValueOrDefault(false));
        if (bedOwned)
            return true;
        bool bedHasOwnerWithExcludedTrait = false;
        if (excludedOwnerTraitDefs is not null && excludedOwnerTraitDefs.Any())
            bedHasOwnerWithExcludedTrait = bedOwners.Any(p => p != pawn && p != pawnLover && p.CanBeUsed()
                                                           && (p.story?.traits?.allTraits?.Any(t => excludedOwnerTraitDefs.Contains(t.def))).GetValueOrDefault(
                                                                  false));
        return bedHasOwnerWithExcludedTrait;
    }

    public static bool SufferingFromThought(this List<Thought> thoughts, string thoughtDefName, out Thought thought, out float currentBaseMoodEffect)
    {
        thought = thoughts.FindThought(thoughtDefName);
        currentBaseMoodEffect = (thought?.CurStage?.baseMoodEffect).GetValueOrDefault(0);
        return currentBaseMoodEffect < 0;
    }

    private static Thought FindThought(this List<Thought> thoughts, string thoughtDefName) => thoughts?.Find(t => t.def?.defName == thoughtDefName);
}