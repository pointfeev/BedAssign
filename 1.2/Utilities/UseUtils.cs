using RimWorld;
using System.Linq;
using Verse;

namespace BedAssign
{
    public static class UseUtils
    {
        public static int GetBedSlotCount(this Building_Bed bed)
        {
            return bed.SleepingSlotsCount;
        }

        public static bool CanBeUsed(this Pawn pawn)
        {
            return !(pawn is null) && !(pawn.ownership is null) &&
                !(pawn.Faction is null) && pawn.Faction.IsPlayer && pawn.IsFreeColonist &&
                !(pawn.def is null) && !(pawn.def.race is null) && pawn.def.race.Humanlike;
        }

        public static bool IsDesignatedDeconstructOrUninstall(this Building building)
        {
            return building is null || building.Map is null || building.Map.designationManager is null ||
                building.Map.designationManager.AllDesignationsOn(building).ToList().Any(designation => !(designation is null) &&
                (designation.def == DesignationDefOf.Deconstruct || designation.def == DesignationDefOf.Uninstall));
        }

        public static bool CanBeUsed(this Building_Bed bed)
        {
            return CanBeUsedEver(bed) && !BedAssignData.UnusableBeds.Contains(bed) && !bed.IsDesignatedDeconstructOrUninstall();
        }
        public static bool CanBeUsedEver(this Building_Bed bed)
        {
            return !(bed is null) && !bed.IsHospitalityGuestBed() &&
                !bed.Medical && !bed.ForPrisoners && bed.def.building.bed_humanlike;
        }

        public static bool IsBetterThan(this Building_Bed bed1, Building_Bed bed2)
        {
            bool bed1IsBetter = true;
            bool bed2IsBetter = false;

            if (bed1 is null && !(bed2 is null))
                return bed2IsBetter;
            else if (bed2 is null && !(bed1 is null))
                return bed1IsBetter;

            // Prioritize bedrooms
            Room room1 = bed1.GetRoom();
            Room room2 = bed2.GetRoom();
            if (room1 is null && !(room2 is null))
                return bed2IsBetter;
            else if (room2 is null && !(room1 is null))
                return bed1IsBetter;

            // ... then bedroom impressiveness
            float impressive1 = room1.GetStat(RoomStatDefOf.Impressiveness);
            float impressive2 = room2.GetStat(RoomStatDefOf.Impressiveness);
            if (impressive1 < impressive2)
                return bed2IsBetter;
            else if (impressive1 > impressive2)
                return bed1IsBetter;

            // ... then bed rest effectiveness
            float effect1 = bed1.GetStatValue(StatDefOf.BedRestEffectiveness);
            float effect2 = bed2.GetStatValue(StatDefOf.BedRestEffectiveness);
            if (effect1 < effect2)
                return bed2IsBetter;
            else if (effect1 > effect2)
                return bed1IsBetter;

            // ... then bed comfort
            float comfort1 = bed1.GetStatValue(StatDefOf.Comfort);
            float comfort2 = bed2.GetStatValue(StatDefOf.Comfort);
            if (comfort1 < comfort2)
                return bed2IsBetter;
            else if (comfort1 > comfort2)
                return bed1IsBetter;

            // ... then bed beauty
            float beauty1 = bed1.GetStatValue(StatDefOf.Beauty);
            float beauty2 = bed2.GetStatValue(StatDefOf.Beauty);
            if (beauty1 < beauty2)
                return bed2IsBetter;
            else if (beauty1 > beauty2)
                return bed1IsBetter;

            // ... otherwise, use ID numbers for consistent sorting
            return (bed1.thingIDNumber < bed2.thingIDNumber) ? bed1IsBetter : bed2IsBetter;
        }
    }
}
