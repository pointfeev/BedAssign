using UnityEngine;

using Verse;

namespace BedAssign
{
    public class ModSettings : Verse.ModSettings
    {
        public static bool outputReassignmentMessages = true;
        public static bool avoidJealousPenalty = true;
        public static bool avoidGreedyPenalty = true;
        public static bool avoidAsceticPenalty = true;
        public static bool claimBetterBeds = true;
        public static float betterBedRoomImpressivenessThreshold = 3;
        public static bool avoidPartnerPenalty = true;
        public static bool avoidSharingPenalty = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref outputReassignmentMessages, "outputReassignmentMessages", true);
            Scribe_Values.Look(ref avoidJealousPenalty, "avoidJealousPenalty", true);
            Scribe_Values.Look(ref avoidGreedyPenalty, "avoidGreedyPenalty", true);
            Scribe_Values.Look(ref avoidAsceticPenalty, "avoidAsceticPenalty", true);
            Scribe_Values.Look(ref claimBetterBeds, "claimBetterBeds", true);
            Scribe_Values.Look(ref betterBedRoomImpressivenessThreshold, "betterBedRoomImpressivenessThreshold", 3);
            Scribe_Values.Look(ref avoidPartnerPenalty, "avoidPartnerPenalty", true);
            Scribe_Values.Look(ref avoidSharingPenalty, "avoidSharingPenalty", true);
            base.ExposeData();
        }
    }

    public class Mod : Verse.Mod
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly ModSettings settings;
#pragma warning restore IDE0052 // Remove unread private members

        public Mod(ModContentPack content) : base(content) => settings = GetSettings<ModSettings>();

        private string thresholdBuffer = ModSettings.betterBedRoomImpressivenessThreshold.ToString();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Output reassignment messages", ref ModSettings.outputReassignmentMessages, "Should the mod display messages in the top left whenever pawns are automatically reassigned by the mod?");
            listingStandard.GapLine();
            listingStandard.CheckboxLabeled("Attempt to avoid Jealous mood penalty", ref ModSettings.avoidJealousPenalty, "Should the mod care about the Jealous mood penalty?");
            listingStandard.CheckboxLabeled("Attempt to avoid Greedy mood penalty", ref ModSettings.avoidGreedyPenalty, "Should the mod care about the Greedy mood penalty?");
            listingStandard.CheckboxLabeled("Attempt to avoid Ascetic mood penalty", ref ModSettings.avoidAsceticPenalty, "Should the mod care about the Ascetic mood penalty?");
            listingStandard.CheckboxLabeled("Attempt to claim better empty beds", ref ModSettings.claimBetterBeds, "Should the mod care about the existance of more impressive bedrooms or more effective beds?");
            if (ModSettings.claimBetterBeds)
                listingStandard.TextFieldNumericLabeled("Better bed room impressiveness threshold", ref ModSettings.betterBedRoomImpressivenessThreshold, ref thresholdBuffer, 0, 30); listingStandard.CheckboxLabeled("Attempt to avoid \"Want to sleep with partner\" mood penalty", ref ModSettings.avoidPartnerPenalty, "Should the mod care about the partner separation mood penalty?");
            listingStandard.CheckboxLabeled("Attempt to avoid \"Sharing bed\" mood penalty", ref ModSettings.avoidSharingPenalty, "Should the mod care about the bed sharing mood penalty?");
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "Automatic Bed Reassignment";
    }
}
