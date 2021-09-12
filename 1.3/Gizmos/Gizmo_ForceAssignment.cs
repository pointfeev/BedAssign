using RimWorld;
using UnityEngine;
using Verse;

namespace BedAssign
{
    public class Gizmo_ForceAssignment : Command_Toggle
    {
        public Building_Bed bed = null;
        public Pawn pawn = null;
        public Gizmo_ForceAssignment(Building_Bed bed, Pawn pawn) : base()
        {
            this.bed = bed;
            this.pawn = pawn;
            toggleAction = delegate ()
            {
                if (BedAssignData.ForcedBeds.TryGetValue(pawn) == bed)
                {
                    BedAssignData.ForcedBeds.Remove(pawn);
                }
                else
                {
                    BedAssignData.ForcedBeds.SetOrAdd(pawn, bed);
                }
            };
            isActive = () => BedAssignData.ForcedBeds.TryGetValue(pawn) == bed;
            defaultLabel = "Force assign " + pawn.LabelShort;
            defaultDesc = "Force " + pawn.LabelShort + " to always take this bed when they decide to get rest.";
            icon = PortraitsCache.Get(pawn, Vector2.one * 75f, Rot4.South, cameraZoom: 1.25f).AsTexture2D();
            order = GizmoOrder.Special + 2;
        }

        public override bool GroupsWith(Gizmo gizmo) => false;
    }
}