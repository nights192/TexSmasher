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
}