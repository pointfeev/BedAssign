using UnityEngine;
using Verse;

namespace BedAssign;

public class ModSettings : Verse.ModSettings
{
    public static bool OutputReassignmentMessages = true;
    public static bool AvoidJealousPenalty = true;
    public static bool AvoidGreedyPenalty = true;
    public static bool AvoidAsceticPenalty = true;
    public static bool ClaimBetterBeds = true;
    public static int BetterBedRoomImpressivenessThreshold = 5;
    public static bool AvoidPartnerPenalty = true;
    public static bool AvoidSharingPenalty = true;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref OutputReassignmentMessages, "outputReassignmentMessages", true);
        Scribe_Values.Look(ref AvoidJealousPenalty, "avoidJealousPenalty", true);
        Scribe_Values.Look(ref AvoidGreedyPenalty, "avoidGreedyPenalty", true);
        Scribe_Values.Look(ref AvoidAsceticPenalty, "avoidAsceticPenalty", true);
        Scribe_Values.Look(ref ClaimBetterBeds, "claimBetterBeds", true);
        Scribe_Values.Look(ref BetterBedRoomImpressivenessThreshold, "betterBedRoomImpressivenessThreshold", 5);
        Scribe_Values.Look(ref AvoidPartnerPenalty, "avoidPartnerPenalty", true);
        Scribe_Values.Look(ref AvoidSharingPenalty, "avoidSharingPenalty", true);
        base.ExposeData();
    }
}

public class Mod : Verse.Mod
{
    private readonly ModSettings settings;

    private string thresholdBuffer = ModSettings.BetterBedRoomImpressivenessThreshold.ToString();

    public Mod(ModContentPack content) : base(content) => settings = GetSettings<ModSettings>();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listingStandard = new();
        listingStandard.Begin(inRect);
        listingStandard.CheckboxLabeled("Output reassignment messages", ref ModSettings.OutputReassignmentMessages,
            "Should the mod display messages in the top left whenever pawns are automatically reassigned by the mod?");
        listingStandard.GapLine();
        listingStandard.CheckboxLabeled("Attempt to avoid Jealous mood penalty", ref ModSettings.AvoidJealousPenalty,
            "Should the mod care about the Jealous mood penalty?");
        listingStandard.CheckboxLabeled("Attempt to avoid Greedy mood penalty", ref ModSettings.AvoidGreedyPenalty,
            "Should the mod care about the Greedy mood penalty?");
        listingStandard.CheckboxLabeled("Attempt to avoid Ascetic mood penalty", ref ModSettings.AvoidAsceticPenalty,
            "Should the mod care about the Ascetic mood penalty?");
        listingStandard.CheckboxLabeled("Attempt to claim better empty beds", ref ModSettings.ClaimBetterBeds,
            "Should the mod care about the existance of more impressive bedrooms or more effective beds?");
        if (ModSettings.ClaimBetterBeds)
            listingStandard.TextFieldNumericLabeled("Better bed room impressiveness threshold", ref ModSettings.BetterBedRoomImpressivenessThreshold,
                ref thresholdBuffer, 0, 30);
        listingStandard.CheckboxLabeled("Attempt to avoid \"Want to sleep with partner\" mood penalty", ref ModSettings.AvoidPartnerPenalty,
            "Should the mod care about the partner separation mood penalty?");
        listingStandard.CheckboxLabeled("Attempt to avoid \"Sharing bed\" mood penalty", ref ModSettings.AvoidSharingPenalty,
            "Should the mod care about the bed sharing mood penalty?");
        listingStandard.End();
        base.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory() => "Automatic Bed Reassignment";
}