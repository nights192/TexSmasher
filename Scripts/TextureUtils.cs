using System.Collections;
using System.Linq;
using UnityEngine;

public static class TextureUtils
{
    public static Texture2D GenSolidTexture(int width, int height, Color color)
    {
        Texture2D result = new Texture2D(width, height);
        Color[] pixels = Enumerable.Repeat(color, width * height).ToArray();

        result.SetPixels(pixels);

        return result;
    }

    public static RenderTexture GenEmptyAtlasTexture(Vector2Int resolution)
    {
        RenderTexture previousActiveTarget = RenderTexture.active;

        RenderTexture res = new RenderTexture(resolution.x, resolution.y, 32, RenderTextureFormat.ARGB32);
        RenderTexture.active = res;
        GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        RenderTexture.active = previousActiveTarget;

        return res;
    }
}