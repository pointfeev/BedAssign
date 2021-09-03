using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;
using System;

namespace BedAssign
{
    public static class BedAssign
    {
        public static int GetSlotCount(this Building_Bed bed)
        {
            return bed.SleepingSlotsCount;
        }

        public static void CheckBeds(Pawn pawn)
        {
            if (!ClaimUtils.CanUsePawn(pawn)) { return; }

            // Unclaim off-map bed to give space to other colonists
            Building_Bed currentBed = pawn.ownership.OwnedBed;
            if (currentBed != null && pawn.Map != currentBed.Map)
            {
                pawn.ownership.UnclaimBed();
                Log.Message("[BedAssign] " + pawn.LabelShort + " unclaimed their bed due to being on a different map");
            }

            // Attempt to claim forced bed
            Building_Bed forcedBed = ClaimUtils.GetForcedPawnBedIfPossible(pawn);
            if (forcedBed != null)
            {
                if (ClaimUtils.ClaimBedIfPossible(pawn, forcedBed))
                {
                    Log.Message("[BedAssign] " + pawn.LabelShort + " claimed their forced bed");
                    return;
                }
            }

            if (pawn.Map is null) { return; }

            // Get and sort all beds on the pawn's map in descending order by their room's impressiveness
            List<Building_Bed> bedsSorted = pawn.Map.listerBuildings?.AllBuildingsColonistOfClass<Building_Bed>().ToList();
            if (bedsSorted is null)
                bedsSorted = new List<Building_Bed>();
            bedsSorted.RemoveAll(bed => !bed.def.building.bed_humanlike); // remove all non-humanlike beds since we don't touch animals anyways
            bedsSorted.Sort(delegate (Building_Bed bed1, Building_Bed bed2)
            {
                if (bed1 is null)
                    return -1;
                else if (bed2 is null)
                    return 1;

                Room room1 = bed1.GetRoom();
                Room room2 = bed2.GetRoom();

                if (room1 is null)
                    return -1;
                else if (room2 is null)
                    return 1;

                float imp1 = room1.GetStat(RoomStatDefOf.Impressiveness);
                float imp2 = room2.GetStat(RoomStatDefOf.Impressiveness);

                if (imp1 < imp2)
                    return 1;
                else if (imp1 > imp2)
                    return -1;
                return 0;
            });

            // Attempt to avoid the "Want to sleep with partner" moodlet
            Pawn pawnLover = ClaimUtils.GetMostLikedLovePartnerIfPossible(pawn);
            if (!(pawnLover is null))
            {
                // Attempt to simply claim their lover's bed
                Building_Bed loverBed = pawnLover.ownership.OwnedBed;
                if (loverBed != null && !loverBed.Medical && currentBed != loverBed)
                {
                    if (loverBed.AnyUnownedSleepingSlot)
                    {
                        if (ClaimUtils.ClaimBedIfPossible(pawn, loverBed, pawnLover))
                        {
                            Log.Message("[BedAssign] " + pawn.LabelShort + " claimed the bed of their lover, " + pawnLover.LabelShort);
                            return;
                        }
                    }
                    else if (ClaimUtils.GetMostLikedLovePartnerIfPossible(pawnLover) == pawn)
                    {
                        // Attempt to claim a bed that has more than one sleeping spot for the lovers
                        foreach (Building_Bed bed in bedsSorted)
                        {
                            if (!bed.Medical && bed.GetSlotCount() >= 2 &&
                                RestUtility.CanUseBedEver(pawn, bed.def) && RestUtility.CanUseBedEver(pawnLover, bed.def))
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
                                else if (canClaim && !(currentBed is null)) // undo the change if both partners didn't successfully claim the bed together
                                {
                                    ClaimUtils.ClaimBedIfPossible(pawn, currentBed);
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
                    Log.Message("[BedAssign] " + pawn.LabelShort + " unclaimed their bed to avoid 'Sharing bed' moodlet");
                    return;
                }
            }

            // Attempt to claim an empty bed in a better (more impressive) room
            float currentRoomImpressiveness = 0;
            if (!(currentBed is null) && !(currentBed.GetRoom() is null))
                currentRoomImpressiveness = currentBed.GetRoom().GetStat(RoomStatDefOf.Impressiveness);
            if (!(pawnLover is null))
            {
                // ... with their lover
                foreach (Building_Bed bed in bedsSorted)
                {
                    if (!bed.Medical && !bed.OwnersForReading.Any() && // is the bed unowned?
                        RestUtility.CanUseBedEver(pawn, bed.def) && RestUtility.CanUseBedEver(pawnLover, bed.def) && // can the bed be used by both lovers?
                        bed.GetSlotCount() >= 2 && // does the bed have slots for both lovers?
                        !(bed.GetRoom() is null) && bed.GetRoom().GetStat(RoomStatDefOf.Impressiveness) > currentRoomImpressiveness && // is the room's impressiveness better than their current?
                        ClaimUtils.ClaimBedIfPossible(pawn, bed) && ClaimUtils.ClaimBedIfPossible(pawnLover, bed))
                    {
                        Log.Message($"[BedAssign] Lovers, {pawn.LabelShort} and {pawnLover.LabelShort}, claimed an empty bed in a more impressive room together");
                        return;
                    }
                    else if (!(currentBed is null)) // undo the change if both partners didn't successfully claim the bed together
                    {
                        ClaimUtils.ClaimBedIfPossible(pawn, currentBed);
                    }
                }
            }
            else
            {
                // ... for themself
                foreach (Building_Bed bed in bedsSorted)
                {
                    if (!bed.Medical && !bed.OwnersForReading.Any() && // is the bed unowned?
                        RestUtility.CanUseBedEver(pawn, bed.def) && // can the bed be used?
                        !(bed.GetRoom() is null) && bed.GetRoom().GetStat(RoomStatDefOf.Impressiveness) > currentRoomImpressiveness && // is the room's impressiveness better than their current?
                        ClaimUtils.ClaimBedIfPossible(pawn, bed))
                    {
                        Log.Message("[BedAssign] " + pawn.LabelShort + " claimed an empty bed in a more impressive room");
                        return;
                    }
                }
            }
        }
    }
}
