using Silk.NET.Vulkan;

namespace Atom.Engine;

public class Topology : IRasterSettings
{
    public static Topology Default { get; } = new() { Primitive = PrimitiveTopology.TriangleList };
    
    
    
    private PipelineInputAssemblyStateCreateInfo _topology;
    internal ref PipelineInputAssemblyStateCreateInfo State => ref _topology;

    
    
    public unsafe Topology() => _topology = new(flags: 0);
    
    public Topology(Topology cloneFrom) => _topology = cloneFrom._topology;
    
    public IMaterialSettings Clone() => new Topology(this);
        
    

    public PrimitiveTopology Primitive
    {
        get => _topology.Topology;
        set => _topology.Topology = value;
    }

    /// <summary> Breaks the vertices drawing queue, allowing to not directly connect elements with each other using the .MaxValue of the current <see cref="Carbon.Core.IndexType"/> value type. </summary>
    public bool DoRestart
    {
        get => _topology.PrimitiveRestartEnable;
        set => _topology.PrimitiveRestartEnable = value;
    }
}