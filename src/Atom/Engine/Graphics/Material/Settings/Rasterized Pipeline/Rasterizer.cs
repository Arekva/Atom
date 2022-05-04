using Silk.NET.Vulkan;

namespace Atom.Engine.GraphicsPipeline;

public class Rasterizer : IRasterSettings
{
    public static Rasterizer Default { get; } = new() { CullMode = CullModeFlags.CullModeFrontBit, LineWidth = 1.0F };

    
    
    private PipelineRasterizationStateCreateInfo _rasterizer;
    
    internal ref PipelineRasterizationStateCreateInfo State => ref _rasterizer;
    
    
    
    public unsafe Rasterizer() => _rasterizer = new (flags: 0);
    
    public Rasterizer(Rasterizer cloneFrom) => _rasterizer = cloneFrom._rasterizer;
    
    public IMaterialSettings Clone() => new Rasterizer(this);
    
    
        
    public bool DoDepthClamp
    {
        get => _rasterizer.DepthClampEnable;
        set => _rasterizer.DepthClampEnable = value;
    }

    public bool DoDisable
    {
        get => _rasterizer.RasterizerDiscardEnable;
        set => _rasterizer.RasterizerDiscardEnable = value;
    }

    /// <summary> <p>Changes how a polygon is rendered.</p> <p>Fill is the default "classic" mode, Line is used for </p></summary>
    public PolygonMode PolygonMode
    {
        get => _rasterizer.PolygonMode;
        set => _rasterizer.PolygonMode = value;
    }
    public CullModeFlags CullMode
    {
        get => _rasterizer.CullMode;
        set => _rasterizer.CullMode = value;
    }
    public FrontFace FrontFace
    {
        get => _rasterizer.FrontFace;
        set => _rasterizer.FrontFace = value;
    }
        
    public bool DoDepthBias
    {
        get => _rasterizer.DepthBiasEnable;
        set => _rasterizer.DepthBiasEnable = value;
    }

    public float DepthBiasConstantFactor
    {
        get => _rasterizer.DepthBiasConstantFactor;
        set => _rasterizer.DepthBiasConstantFactor = value;
    }

    public float DepthBiasClamp
    {
        get => _rasterizer.DepthBiasClamp;
        set => _rasterizer.DepthBiasClamp = value;
    }

    public float DepthBiasSlopeFactor
    {
        get => _rasterizer.DepthBiasSlopeFactor;
        set => _rasterizer.DepthBiasSlopeFactor = value;
    }

    public float LineWidth
    {
        get => _rasterizer.LineWidth;
        set => _rasterizer.LineWidth = value;
    }
}