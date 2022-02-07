using RimWorld;

using UnityEngine;

using Verse;

namespace BedAssign
{
    public class Gizmo_ForceAssignment : Command_Toggle
    {
        public Building_Bed bed = null;
        public Pawn pawn = null;

        public Gizmo_ForceAssignment(Pawn pawn, Building_Bed bed) : base()
        {
            this.bed = bed;
            this.pawn = pawn;
            toggleAction = delegate ()
            {
                if (BedAssignData.ForcedBeds.TryGetValue(pawn) == this.bed)
                {
                    BedAssignData.ForcedBeds.Remove(pawn);
                }
                else
                {
                    BedAssignData.ForcedBeds.SetOrAdd(pawn, this.bed);
                }
            };
            isActive = () => BedAssignData.ForcedBeds.TryGetValue(pawn) == this.bed;
            defaultLabel = "Force assign " + pawn.LabelShort;
            defaultDesc = "Force " + pawn.LabelShort + " to always take this bed when they decide to get rest.";
            icon = PortraitsCache.Get(pawn, Vector2.one * 75f, cameraZoom: 1.25f).AsTexture2D();
            order = GizmoOrder.Special + 2;
        }

        public override bool GroupsWith(Gizmo gizmo) => false;
    }
}