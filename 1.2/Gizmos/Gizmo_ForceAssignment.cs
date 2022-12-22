using BedAssign.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace BedAssign.Gizmos
{
    public sealed class Gizmo_ForceAssignment : Command_Toggle
    {
        public Building_Bed Bed;

        public Gizmo_ForceAssignment(Pawn pawn, Building_Bed bed)
        {
            Bed = bed;
            toggleAction = delegate
            {
                if (BedAssignData.ForcedBeds.TryGetValue(pawn) == Bed)
                    _ = BedAssignData.ForcedBeds.Remove(pawn);
                else
                    BedAssignData.ForcedBeds.SetOrAdd(pawn, Bed);
            };
            isActive = () => BedAssignData.ForcedBeds.TryGetValue(pawn) == Bed;
            defaultLabel = "Force assign " + pawn.LabelShort;
            defaultDesc = "Force " + pawn.LabelShort + " to always take this bed when they decide to get rest.";
            icon = PortraitsCache.Get(pawn, Vector2.one * 75f, cameraZoom: 1.25f).AsTexture2D();
            order = GizmoOrder.Special + 2;
        }

        public override bool GroupsWith(Gizmo gizmo) => false;
    }
}