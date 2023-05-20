using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RectPackingResult
{
    public List<PackingRect> Atlas;
    public Material[] UnfittedMaterials;

    public RectPackingResult((List<PackingRect>, Material[]) results)
    {
        (Atlas, UnfittedMaterials) = results;
    }
}

public class PackingResult
{
    public Shader Shader;
    public int[] Triangles;
    public Dictionary<string, Texture2D> AtlasTextures;
    public Material[] UnfittedMaterials;

    public PackingResult(Shader shader, int[] triangles, Dictionary<string, Texture2D> atlasTextures, Material[] unfittedMaterials)
    {
        Shader = shader;
        Triangles = triangles;
        AtlasTextures = atlasTextures;
        UnfittedMaterials = unfittedMaterials;
    }
}

public class ShaderGroup
{
    public Dictionary<Material, MaterialInfo> PackingMaterials;
    public ShaderArchetype Archetype;

    public bool Add(Material mat, int[] triangles)
    {
        // TODO: Leverage archetype to check if the two materials may be atlased.

        if (!PackingMaterials.ContainsKey(mat))
            PackingMaterials.Add(mat, new MaterialInfo(triangles));

        MaterialInfo curMatInfo = PackingMaterials[mat];

        foreach (string prop in mat.GetTexturePropertyNames())
        {
            if (Archetype.TextureAtlased(prop))
            {
                Texture2D propTexture = (Texture2D)mat.GetTexture(prop); // A dangerous assumption; however, work for another day.
                TextureInfo propTexInfo = new TextureInfo(mat, prop, propTexture);

                curMatInfo.AddTexture(propTexInfo);
            }
        }

        return true;
    }

    public PackingResult Pack(Vector2[] uvs, Vector2Int resolution)
    {
        Dictionary<Material, Vector2Int> materialDimensions = PackingMaterials.ToDictionary(
            kv => { return kv.Key; },
            kv => { return kv.Value.CalculateCanonicalSize(); }
            );

        RectPackingResult packingResults = PackCanonical(materialDimensions, resolution);
        Dictionary<string, RenderTexture> PackingAtlases = new Dictionary<string, RenderTexture>();
        Dictionary<string, Vector2> slotCoefficients = GetSlotCoefficients();

        foreach (PackingRect rect in packingResults.Atlas)
        {
            MaterialInfo currentMat = PackingMaterials[rect.PackingMaterial];

            // First, we apply our packed atlas to the specified UV triangles.
            Rect uvRect = rect.NormalizedRect(resolution);

            // TODO: FIX VERTEX DOUBLING!
            foreach (int index in currentMat.Triangles)
            {
                Vector2 targetUV = uvs[index];

                uvs[index] = new Vector2(targetUV.x * uvRect.width + uvRect.x,
                    targetUV.y * uvRect.height + uvRect.y);
            }

            // Now, we add our images to the atlas.
            foreach (TextureInfo texInfo in currentMat.Textures)
            {
                Vector2 coefficient = slotCoefficients[texInfo.TexSlot];

                if (!PackingAtlases.ContainsKey(texInfo.TexSlot))
                    PackingAtlases.Add(texInfo.TexSlot, 
                        TextureUtils.GenEmptyAtlasTexture(Vector2Int.FloorToInt(resolution * coefficient)));

                RenderTexture targetAtlas = PackingAtlases[texInfo.TexSlot];
                Texture2D processedTexture = Archetype.ProcessTexture(texInfo.TexMaterial,
                    texInfo.TexSlot, texInfo.Texture,
                    Vector2Int.FloorToInt(materialDimensions[texInfo.TexMaterial] * coefficient));

                RenderTexture prev = RenderTexture.active;
                Graphics.Blit(processedTexture, targetAtlas, coefficient, uvRect.position);
                RenderTexture.active = prev;
            }
        }
    }

    // WARNING: DESTRUCTIVE OPERATION!
    private RectPackingResult PackCanonical(Dictionary<Material, Vector2Int> materialDimensions, Vector2Int resolution)
    {
        PackingAtlas atlas = new PackingAtlas();

        foreach (KeyValuePair<Material, MaterialInfo> elem in PackingMaterials)
        {
            atlas.Add(new PackingRect(elem.Key, new RectInt(
                new Vector2Int(0, 0), materialDimensions[elem.Key]
                )));
        }

        RectPackingResult results = new RectPackingResult(atlas.Pack(resolution));
        foreach (Material mat in results.UnfittedMaterials)
            PackingMaterials.Remove(mat);

        return results;
    }

    private Dictionary<string, Vector2> GetSlotCoefficients()
    {
        Vector2Int largestCanonicalRes = PackingMaterials.Values.Select(x => x.CalculateCanonicalSize()).OrderBy(x => x.sqrMagnitude).FirstOrDefault();
        Dictionary<string, Vector2> res = new Dictionary<string, Vector2>();

        Dictionary<string, List<Texture2D>> packingTextures = new Dictionary<string, List<Texture2D>>();

        foreach(MaterialInfo matInfo in PackingMaterials.Values)
        {
            foreach (TextureInfo textureInfo in matInfo.Textures)
            {
                if (!packingTextures.ContainsKey(textureInfo.TexSlot))
                    packingTextures.Add(textureInfo.TexSlot, new List<Texture2D>());

                packingTextures[textureInfo.TexSlot].Add(textureInfo.Texture);
            }
        }

        foreach (KeyValuePair<string, List<Texture2D>> entry in packingTextures)
        {
            Vector2Int maxRes = new Vector2Int();

            foreach (Texture2D texInfo in entry.Value)
            {
                Vector2Int texDimensions = new Vector2Int(texInfo.width, texInfo.height);

                maxRes = (texDimensions.sqrMagnitude > maxRes.sqrMagnitude) ? texDimensions : maxRes;
            }

            res.Add(entry.Key, new Vector2(maxRes.x / (float) largestCanonicalRes.x, maxRes.y / (float) largestCanonicalRes.y));
        }

        return res;
    }
}