namespace Atom.Engine.Astro.Transvoxel;

public struct TransitionCellData
{
    public TransitionCellData(byte geometryCounts, params byte[] vertexIndex)
    {
        GeometryCounts = geometryCounts;
        VertexIndex = vertexIndex;
    }
        
    /// <summary> High nibble is vertex count, low nibble is triangle count. </summary>
    public int GeometryCounts;
    /// <summary> Groups of 3 indexes giving the triangulation. </summary>
    private byte[] VertexIndex; // 36 elements
    // todo: hardcode array variables in struct
        
    public int GetVertexCount => GeometryCounts >> 4;
    public int GetTriangleCount => GeometryCounts & 0x0F;
}