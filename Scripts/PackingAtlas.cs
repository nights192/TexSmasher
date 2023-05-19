using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;

public class PackingRect
{
    public Material PackingMaterial;
    public RectInt Rect;

    public PackingRect(Material mat, RectInt rect)
    {
        PackingMaterial = mat;
        Rect = rect;
    }

    public void Place(Vector2Int pos)
    {
        Rect.position = pos;
    }

    public bool IntersectsTranslated(Vector2Int pos, PackingRect oRect)
    {
        return (new RectInt(pos.x, pos.y, Rect.width, Rect.height)).Overlaps(oRect.Rect);
    }

    public Rect NormalizedRect(Vector2Int resolution)
    {
        return new Rect(
            Rect.x / (float)resolution.x,
            Rect.y / (float)resolution.y,
            Rect.width / (float)resolution.x,
            Rect.height / (float)resolution.y
            );
    }
}

public class MaterialInfo
{
    public List<TextureInfo> Textures;
    public int[] Triangles;

    public static Vector2 CalculateUVSize(Vector2Int canonicalSize, Vector2Int maxRes)
    {
        return new Vector2(canonicalSize.x / maxRes.x, canonicalSize.y / maxRes.y);
    }

    public MaterialInfo(int[] tris)
    {
        Textures = new List<TextureInfo>();
        Triangles = tris;
    }

    public void AddTexture(TextureInfo texture)
    {
        Textures.Add(texture);
    }

    public Vector2Int CalculateCanonicalSize()
    {
        Vector2Int res = new Vector2Int();

        foreach (TextureInfo texture in Textures)
        {
            res.x = (texture.Texture.width > res.x) ? texture.Texture.width : res.x;
            res.y = (texture.Texture.height > res.y) ? texture.Texture.height : res.y;
        }

        return res;
    }
}

public class TextureInfo
{
    public Material TexMaterial;
    public string TexSlot;
    public Texture2D Texture;

    public TextureInfo(Material mat, string slot, Texture2D tex)
    {
        TexMaterial = mat;
        TexSlot = slot;
        Texture = tex;
    }
}

public class PackingAtlas
{
    private const int _skipDist = 8;
    private List<PackingRect> TextureRects;

    public PackingAtlas()
    {
        TextureRects = new List<PackingRect>();
    }

    public void Add(PackingRect rect)
    {
        TextureRects.Add(rect);
    }

    public (List<PackingRect>, Material[]) Pack(Vector2Int resolution, bool removeUnfittable = false)
    {
        List<PackingRect> placedRects = new List<PackingRect>();
        Dictionary<PackingRect, int> unfittedIndices = new Dictionary<PackingRect, int>();
        Sort();

        Vector2Int insertionPosition = new Vector2Int(0, 0);

        for (int i = 0; i < TextureRects.Count; i++)
        {
            Vector2Int scanPosition = insertionPosition;
            PackingRect placingRect = TextureRects[i];

            bool canPlace = false;

            while (!canPlace)
            {
                canPlace = true;

                // As this is a horizontal-dominant scan, should we not have sufficient height, the rect is unplaceable.
                if (scanPosition.y + placingRect.Rect.height - 1 >= resolution.y)
                {
                    canPlace = false;
                    break;
                }

                // Should we exceed the width of the image, we may adjust our scan further down.
                if (scanPosition.x + placingRect.Rect.width - 1 >= resolution.x)
                {
                    canPlace = false;

                    scanPosition.x = 0;
                    scanPosition.y += _skipDist;

                    continue;
                }

                foreach (PackingRect checkRect in TextureRects.GetRange(0, i))
                {
                    if (placingRect.IntersectsTranslated(scanPosition, checkRect) && !unfittedIndices.ContainsKey(checkRect))
                    {
                        canPlace = false;
                        break;
                    }
                }

                if (!canPlace)
                    scanPosition.x += _skipDist;
            }

            if (canPlace)
            {
                insertionPosition = new Vector2Int(scanPosition.x + 8, scanPosition.y);
                placingRect.Place(scanPosition);
                placedRects.Add(placingRect);
            }
            else
            {
                unfittedIndices.Add(placingRect, i);
            }
        }

        if (removeUnfittable)
            TextureRects = placedRects;

        return (placedRects, unfittedIndices.Keys.Select(k => { return k.PackingMaterial; }).ToArray());
    }

    private void Sort()
    {
        TextureRects.Sort((PackingRect x, PackingRect y) =>
        {
            return y.Rect.size.sqrMagnitude - x.Rect.size.sqrMagnitude;
        });
    }
}
