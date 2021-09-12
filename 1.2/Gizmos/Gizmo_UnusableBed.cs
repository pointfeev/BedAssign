using RimWorld;
using Verse;

namespace BedAssign
{
    public class Gizmo_UnusableBed : Command_Toggle
    {
        public Building_Bed bed = null;
        public Gizmo_UnusableBed(Building_Bed bed) : base()
        {
            this.bed = bed;
            toggleAction = delegate ()
            {
                if (BedAssignData.UnusableBeds.Contains(bed))
                {
                    BedAssignData.UnusableBeds.Remove(bed);
                }
                else
                {
                    BedAssignData.UnusableBeds.Add(bed);
                }
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