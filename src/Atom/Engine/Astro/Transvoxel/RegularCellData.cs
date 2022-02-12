namespace Atom.Engine.Astro.Transvoxel;

/// <summary>
/// The RegularCellData structure holds information about the triangulation
/// used for a single equivalence class in the modified Marching Cubes algorithm,
/// described in Section 3.2.
/// </summary>
public struct RegularCellData
{
    public RegularCellData(byte geometryCounts, params byte[] vertexIndex)
    {
        GeometryCounts = geometryCounts;
        VertexIndex = vertexIndex;
    }
        
    /// <summary> High nibble is vertex count, low nibble is triangle count. </summary>
    public byte GeometryCounts;
    /// <summary> Groups of 3 indexes giving the triangulation. </summary>
    public byte[] VertexIndex; // 15 elements
    // todo: hardcode vertex array variables in struct (no heap alloc.)

    public int GetVertexCount => GeometryCounts >> 4;
    public int GetTriangleCount => GeometryCounts & 0x0F;
}