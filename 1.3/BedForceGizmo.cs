using Verse;
using UnityEngine;
using RimWorld;

namespace BedAssign
{
	public class BedForceGizmo : Command_Toggle
	{
		protected override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms gizmoRenderParms)
        {
			if (this.owner != null)
            {
				GUI.color = Color.white;
				Texture portrait = PortraitsCache.Get(this.owner, Vector2.one * 75f, Rot4.South, default, 1.25f);
				Widgets.DrawTextureFitted(rect, portrait, 1, new Vector2(portrait.width, portrait.height), new Rect(0f, 0f, 1f, 1f));
			}
			else
            {
				base.DrawIcon(rect, buttonMat, gizmoRenderParms);
            }
		}

		public override float GetWidth(float maxWidth)
		{
			return 100f;
		}


		public override bool GroupsWith(Gizmo other)
		{
			return false;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms gizmoRenderParms)
		{
			GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, gizmoRenderParms);
			if (this.owner != null)
            {
				Rect butRect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);

				Text.Font = GameFont.Tiny;
				GUI.color = Color.white;

				string topText = this.owner.LabelShort;
				float topTextHeight = Text.CalcHeight(topText, butRect.width);
				Rect topTextRect = new Rect(butRect.x, butRect.yMin - topTextHeight + 12f, butRect.width, topTextHeight);

				GUI.DrawTexture(topTextRect, TexUI.GrayTextBG);
				Text.Anchor = TextAnchor.UpperCenter;
				Widgets.Label(topTextRect, topText);
				Text.Anchor = TextAnchor.UpperLeft;

				string bottomText = "Force pawn to use this bed";
				float bottomTextHeight = Text.CalcHeight(bottomText, butRect.width);
				Rect bottomTextRect = new Rect(butRect.x, butRect.yMax - bottomTextHeight + 12f, butRect.width, bottomTextHeight);

				GUI.DrawTexture(bottomTextRect, TexUI.GrayTextBG);
				Text.Anchor = TextAnchor.UpperCenter;
				Widgets.Label(bottomTextRect, bottomText);
				Text.Anchor = TextAnchor.UpperLeft;
			}
			return result;
		}

		public override string TopRightLabel
		{
			get
			{
				return null;
			}
		}

		public Pawn owner = null;
	}
}
