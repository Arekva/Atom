namespace Atom.Engine.Shader;

public struct VectorDescriptor
{
    public bool IsVector => VectorLength > 1U;
    public bool IsMatrix => MatrixColumns > 1U;
    
    
    public uint VectorLength { get; set; }
    public uint MatrixColumns { get; set; }
}
