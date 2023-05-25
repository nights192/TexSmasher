using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISubmeshBuilder
{
    public int[] GetTriangles();
    public Material GetMaterial();
}

public class RetainedSubmesh : ISubmeshBuilder
{
    public int[] Triangles;
    public Material Material;

    public RetainedSubmesh(int[] triangles, Material material)
    {
        Triangles = triangles;
        Material = material;
    }

    public int[] GetTriangles()
    {
        return Triangles;
    }

    public Material GetMaterial()
    {
        return Material;
    }
}

public class MeshBuilder
{
    Mesh mesh;
    MeshRenderer renderer;

    Vector3[] vertices;
    List<int[]> tris;
    Vector2[] uv1;
    Vector2[] uv2;

    List<Material> materials;

    public MeshBuilder(Mesh mesh, MeshRenderer renderer, Vector3[] vertices, Vector2[] uv1, Vector2[] uv2)
    {
        this.mesh = mesh;
        this.renderer = renderer;

        this.vertices = vertices;
        this.uv1 = uv1;
        this.uv2 = uv2;
    }

    public void Add(int[] tris, Material mat)
    {
        this.tris.Add(tris);
        this.materials.Add(mat);
    }

    public void Add(ISubmeshBuilder submeshBuilder)
    {
        tris.Add(submeshBuilder.GetTriangles());
        materials.Add(submeshBuilder.GetMaterial());
    }

    public void Build()
    {
        mesh.Clear(true);
        mesh.subMeshCount = tris.Count;

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv1);
        mesh.SetUVs(1, uv2);

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            mesh.SetTriangles(tris[i], i);
        }

        renderer.materials = materials.ToArray();
        mesh.RecalculateNormals();
    }
}