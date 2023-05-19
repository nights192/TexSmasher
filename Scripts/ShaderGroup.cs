using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PackingResult
{
    public List<PackingRect> atlas;
    public Material[] unfittedMaterials;

    public PackingResult((List<PackingRect>, Material[]) results)
    {
        (atlas, unfittedMaterials) = results;
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

    public List<Material> Pack(Vector3[] verts, Vector2[] uvs, Vector2Int resolution)
    {
        Dictionary<Material, Vector2Int> materialDimensions = PackingMaterials.ToDictionary(
            kv => { return kv.Key; },
            kv => { return kv.Value.CalculateCanonicalSize(); }
            );

        PackingResult packingResults = PackCanonical(materialDimensions, resolution);

        // First, we apply our packed atlas to the specified UV triangles.
        foreach (PackingRect rect in packingResults.atlas)
        {
            Rect uvRect = rect.NormalizedRect(resolution);

            foreach (int index in PackingMaterials[rect.PackingMaterial].Triangles)
            {
                Vector2 targetUV = uvs[index];

                uvs[index] = new Vector2(targetUV.x * uvRect.width + uvRect.x, targetUV.y * uvRect.height + uvRect.y);
            }
        }
    }

    // WARNING: DESTRUCTIVE OPERATION!
    private PackingResult PackCanonical(Dictionary<Material, Vector2Int> materialDimensions, Vector2Int resolution)
    {
        PackingAtlas atlas = new PackingAtlas();

        foreach (KeyValuePair<Material, MaterialInfo> elem in PackingMaterials)
        {
            atlas.Add(new PackingRect(elem.Key, new RectInt(
                new Vector2Int(0, 0), materialDimensions[elem.Key]
                )));
        }

        PackingResult results = new PackingResult(atlas.Pack(resolution));
        foreach (Material mat in results.unfittedMaterials)
            PackingMaterials.Remove(mat);

        return results;
    }

    private Dictionary<string, Vector2Int> GetSlotCoefficients()
    {
        Vector2Int largestCanonicalRes = PackingMaterials.Values.Select(x => x.CalculateCanonicalSize()).OrderBy(x => x.sqrMagnitude).FirstOrDefault();
        Dictionary<string, Vector2Int> res = new Dictionary<string, Vector2Int>();

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

            res.Add(entry.Key, new Vector2Int(maxRes.x / largestCanonicalRes.x, maxRes.y / largestCanonicalRes.y));
        }

        return res;
    }
}