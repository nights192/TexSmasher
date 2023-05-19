using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class TexSmasher : MonoBehaviour
{
    public int maxWidth = 4096;
    public int maxHeight = 4096;
    public bool beginAtlas;

    private void OnValidate()
    {
        if (beginAtlas)
        {
            beginAtlas = false;

            AtlasTextures();
        }
    }

    void AtlasTextures()
    {
        Material[] materials;

        Vector3[] meshVertices;
        Vector2[] uv1;
        Vector2[] uv2;

        Mesh originalSharedMesh = GetComponent<MeshFilter>().sharedMesh;

        materials = GetComponent<MeshRenderer>().materials;
        meshVertices = originalSharedMesh.vertices;

        uv1 = originalSharedMesh.uv.Select(x => RemoveUVOffset(x)).ToArray();
        uv2 = originalSharedMesh.uv2; // Lightmap UVs ought to remain untouched.

        // Generate list of effected triangles by submesh.
        int[][] matTriangles;
        matTriangles = Enumerable.Range(0, originalSharedMesh.subMeshCount).Select(x => originalSharedMesh.GetTriangles(x)).ToArray();
    }

    private ShaderGroup[] GenerateShaderGroups(Material[] materials, int[][] matTriangles)
    {
        ShaderGroup standardGroup = null;
        List<ShaderGroup> otherGroups = new List<ShaderGroup>();
        Shader standardShader = Shader.Find("Standard");

        foreach (Material material in materials)
        {
            if (material.shader == standardShader)
            {
                if (standardGroup == null)
                {
                    standardGroup = new 
                }
            }
        }
    }

    private Vector2 RemoveUVOffset(Vector2 uv)
    {
        if (uv.x == 1 || uv.y == 1)
            return uv;

        return new Vector2(uv.x, uv.y);
    }
}
