using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISubmeshBuilder
{
    public (int[], Material) BuildSubmesh();
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

    public (int[], Material) BuildSubmesh()
    {
        return (Triangles, Material);
    }
}

public class MeshBuilder
{
    Mesh mesh;
    MeshRenderer renderer;

    List<int[]> tris;
    List<Material> materials;

    public MeshBuilder(Mesh mesh, MeshRenderer renderer) {
        this.mesh = mesh;
        this.renderer = renderer;
    }

    public void Add(ISubmeshBuilder submeshBuilder)
    {
        (int[] submeshTris, Material materialTris) = submeshBuilder.BuildSubmesh();

        tris.Add(submeshTris);
        materials.Add(materialTris);
    }
}