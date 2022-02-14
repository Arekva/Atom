namespace Atom.Engine;

public struct ArrayDescriptor
{
    public bool IsArray { get; set; }
    public uint DimensionsCount { get; set; }
    public uint[] DimensionsLengths { get; set; }
    
    public uint TotalElementCount { get; set; }
}