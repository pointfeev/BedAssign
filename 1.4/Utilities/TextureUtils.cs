using UnityEngine;
using Verse;

namespace BedAssign.Utilities;

public static class TextureUtils
{
    public static Texture2D AsTexture2D(this RenderTexture renderTexture)
    {
        Texture2D texture2D = new(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        Graphics.CopyTexture(renderTexture, texture2D);
        return texture2D;
    }

    public static Texture2D AsTexture2D(this Thing thing) => Widgets.GetIconFor(thing.def, thing.Stuff, thing.StyleDef);
}