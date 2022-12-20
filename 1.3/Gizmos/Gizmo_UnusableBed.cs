using BedAssign.Utilities;
using RimWorld;
using Verse;

namespace BedAssign.Gizmos
{
    public class Gizmo_UnusableBed : Command_Toggle
    {
        public Gizmo_UnusableBed(Building_Bed bed)
        {
            toggleAction = delegate
            {
                if (BedAssignData.UnusableBeds.Contains(bed))
                    _ = BedAssignData.UnusableBeds.Remove(bed);
                else
                    BedAssignData.UnusableBeds.Add(bed);
                _ = BedAssignData.ForcedBeds;
            };
            isActive = () => !BedAssignData.UnusableBeds.Contains(bed);
            defaultLabel = "Allow reassign";
            defaultDesc = "Allow automatic reassignment to utilize this bed.";
            icon = bed.AsTexture2D();
            order = GizmoOrder.Special + 1;
        }
    }
}