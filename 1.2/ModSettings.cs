using UnityEngine;

using Verse;

namespace BedAssign
{
    public class ModSettings : Verse.ModSettings
    {
        public static bool outputReassignmentMessages = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref outputReassignmentMessages, "outputReassignmentMessages", true);
            base.ExposeData();
        }
    }

    public class Mod : Verse.Mod
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly ModSettings settings;
#pragma warning restore IDE0052 // Remove unread private members

        public Mod(ModContentPack content) : base(content) => settings = GetSettings<ModSettings>();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Output Reassignment Messages", ref ModSettings.outputReassignmentMessages, "Whether or not the mod will display messages whenever pawns are automatically reassigned.");
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "Automatic Bed Reassignment";
    }
}
