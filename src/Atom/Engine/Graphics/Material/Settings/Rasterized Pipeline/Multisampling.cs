using Silk.NET.Vulkan;

namespace Atom.Engine;

public class Multisampling : IRasterSettings
{
    public static Multisampling Default { get; } = new() { Count = SampleCountFlags.SampleCount1Bit };
    
    
    private PipelineMultisampleStateCreateInfo _multisample;
    internal ref PipelineMultisampleStateCreateInfo State => ref _multisample;
    
    

    public unsafe Multisampling() => _multisample = new (flags: 0);
    
    public Multisampling(Multisampling cloneFrom) => _multisample = cloneFrom._multisample;
    
    public IMaterialSettings Clone() => new Multisampling(this);
        
    
    
    public SampleCountFlags Count
    {
        get => _multisample.RasterizationSamples;
        set => _multisample.RasterizationSamples = value;
    }

    public bool DoMultisampling
    {
        get => _multisample.SampleShadingEnable;
        set => _multisample.SampleShadingEnable = value;
    }

    public float MinSampleShading
    {
        get => _multisample.MinSampleShading;
        set => _multisample.MinSampleShading = value;
    }
        
    // public nint SampleMask { get; set; } TODO
        
    public bool DoAlphaToCoverage
    {
        get => _multisample.AlphaToCoverageEnable;
        set => _multisample.AlphaToCoverageEnable = value;
    }

    public bool DoAlphaToOne
    {
        get => _multisample.AlphaToOneEnable;
        set => _multisample.AlphaToOneEnable = value;
    }
}