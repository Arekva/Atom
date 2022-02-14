using Silk.NET.Vulkan;

namespace Atom.Engine;

public class Tessellation : IRasterSettings
{
    public static Tessellation Default { get; } = new();
    
    
    
    private PipelineTessellationStateCreateInfo _tessellation;
    internal ref PipelineTessellationStateCreateInfo State => ref _tessellation;

    
    
    public unsafe Tessellation() => _tessellation = new(flags: 0);
    public Tessellation(Tessellation cloneFrom) => _tessellation = cloneFrom._tessellation;
    public IMaterialSettings Clone() => new Tessellation(this);
        
    

    public uint PatchControlPointsCount
    {
        get => _tessellation.PatchControlPoints;
        set => _tessellation.PatchControlPoints = value;
    }
}