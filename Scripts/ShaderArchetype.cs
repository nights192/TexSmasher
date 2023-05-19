using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate Texture2D TexInitializer(Material mat, Vector2Int dimensions);
public delegate Texture2D TexProcessor(Material mat, Texture2D tex, Vector2Int canonicalSize);

public class ShaderArchetype
{
    public static TexInitializer DefaultColorInit(Color color)
    {
        return (Material mat, Vector2Int dimensions) =>
        {
            return TextureUtils.GenSolidTexture(dimensions.x, dimensions.y, color);
        };
    }

    public static TexInitializer SlotAlphaInit(string slot)
    {
        return (Material mat, Vector2Int dimensions) =>
        {
            return TextureUtils.GenSolidTexture(dimensions.x, dimensions.y, new Color(1.0f, 1.0f, 1.0f, mat.GetFloat(slot)));
        };
    }

    public static TexProcessor ColorMixer(TexInitializer initializer, string colorSlot)
    {
        return (Material mat, Texture2D tex, Vector2Int canonicalSize) =>
        {
            Color tintColor = mat.GetColor(colorSlot);

            if (tex == null)
                initializer(mat, canonicalSize);

            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                    tex.SetPixel(x, y, tex.GetPixel(x, y) * tintColor);
            }

            return tex;
        };
    }

    public static TexProcessor ChannelMixer(TexInitializer initializer, string rSlot, string gSlot, string bSlot, string aSlot, bool executeWithInitializer = true)
    {
        return (Material mat, Texture2D tex, Vector2Int canonicalSize) =>
        {
            float rIntensity = 1.0f;
            float gIntensity = 1.0f;
            float bIntensity = 1.0f;
            float aIntensity = 1.0f;

            if (rSlot != null)
                rIntensity = mat.GetFloat(rSlot);

            if (gSlot != null)
                rIntensity = mat.GetFloat(gSlot);

            if (bSlot != null)
                rIntensity = mat.GetFloat(bSlot);

            if (aSlot != null)
                rIntensity = mat.GetFloat(aSlot);

            Color intensity = new Color(rIntensity, gIntensity, bIntensity, aIntensity);
            bool runMixer = true;

            if (tex == null)
            {
                tex = TextureUtils.GenSolidTexture(canonicalSize.x, canonicalSize.y, intensity);
                runMixer &= executeWithInitializer;
            }

            if (runMixer)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    for (int x = 0; x < tex.width; x++)
                        tex.SetPixel(x, y, tex.GetPixel(x, y).linear * intensity);
                }
            }

            return tex;
        };
    }

    public static TexProcessor DefaultBlendProcessor(Color defaultValue, string strengthSlot, bool invertBlend = true)
    {
        return (Material mat, Texture2D tex, Vector2Int canonicalSize) =>
        {
            float strength = mat.GetFloat(strengthSlot);
            if (invertBlend)
                strength = 1 - strength;

            if (tex == null)
            {
                tex = TextureUtils.GenSolidTexture(canonicalSize.x, canonicalSize.y, defaultValue);
            }
            else
            {
                for (int y = 0; y < tex.height; y++)
                {
                    for (int x = 0; x < tex.width; x++)
                        tex.SetPixel(x, y, Color.Lerp(tex.GetPixel(x, y), defaultValue, strength));
                }
            }

            return tex;
        };
    }

    public static TexProcessor ParallaxProcessor(string strengthSlot)
    {
        return (Material mat, Texture2D tex, Vector2Int canonicalSize) =>
        {
            float normStrength = (mat.GetFloat(strengthSlot) - 0.005f) / (0.08f - 0.005f);

            if (tex == null)
            {
                tex = TextureUtils.GenSolidTexture(canonicalSize.x, canonicalSize.y, new Color(0.0f, 0.0f, 0.0f));
            }
            else
            {
                Color normIntensity = new Color(normStrength, normStrength, normStrength);

                for (int y = 0; y < tex.height; y++)
                {
                    for (int x = 0; x < tex.width; x++)
                        tex.SetPixel(x, y, tex.GetPixel(x, y) * normIntensity);
                }
            }

            return tex;
        };
    }

    public static ShaderArchetype StandardArchetype = new ShaderArchetype(new Dictionary<string, TexProcessor>()
    {
        { "_MainTex", ColorMixer(DefaultColorInit(Color.white), "_Color")},
        { "_MetallicGlossMap", ChannelMixer(SlotAlphaInit("_Glossiness"), "_Metallic", "_Metallic", "_Metallic", "_GlossMapScale", false) },
        { "_BumpMap", DefaultBlendProcessor(new Color(0.5f, 0.5f, 1.0f), "_BumpScale") },
        { "_ParallaxMap", ParallaxProcessor("_Parallax") },
        { "_OcclusionMap", DefaultBlendProcessor(Color.white, "_OcclusionStrength") },
        { "_EmissionMap", ColorMixer(DefaultColorInit(Color.white), "_EmissionColor") }
    }, new HashSet<string>(new string[] { "_DetailMask", "_DetailAlbedoMap", "_DetailNormalMap" }));

    Dictionary<string, TexProcessor> ParamProcessors;
    HashSet<string> IgnoredSlots;

    public ShaderArchetype()
    {
        ParamProcessors = new Dictionary<string, TexProcessor>();
        IgnoredSlots = new HashSet<string>();
    }

    public ShaderArchetype(Dictionary<string, TexProcessor> pProcessors, HashSet<string> iSlots)
    {
        ParamProcessors = pProcessors;
        IgnoredSlots = iSlots;
    }

    public bool TextureAtlased(string property)
    {
        return !IgnoredSlots.Contains(property);
    }

    public Texture2D ProcessTexture(Material mat, string slot, Texture2D originalTex, Vector2Int canonicalSize)
    {
        bool hasProcessor = ParamProcessors.ContainsKey(slot);
        Texture2D texClone = null;

        if (originalTex != null)
        {
            texClone = new Texture2D(originalTex.width, originalTex.height, originalTex.format, true);
            Graphics.CopyTexture(originalTex, texClone);
        }
        else if (!hasProcessor)
            texClone = new Texture2D(canonicalSize.x, canonicalSize.y);

        // Should original content've been null, we'll fetch our data from the processor for per-shader handling.

        if (hasProcessor)
            texClone = ParamProcessors[slot](mat, texClone, canonicalSize);

        texClone.Resize(canonicalSize.x, canonicalSize.y);
        return texClone;
    }
}