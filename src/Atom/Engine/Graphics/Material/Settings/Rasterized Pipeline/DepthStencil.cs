using Silk.NET.Vulkan;

namespace Atom.Engine.GraphicsPipeline;

public class DepthStencil : IRasterSettings
{
    public static DepthStencil Default { get; set; } 
        = new()
        {
            DoDepthTest = true,
            DepthCompareOp = CompareOp.Greater,
            DepthWriteEnable = true,
            DepthBounds = new Clamp<float>(0.0F, 1.0F)
        };
    
    
    
    private PipelineDepthStencilStateCreateInfo _depth;
    
    internal ref PipelineDepthStencilStateCreateInfo State => ref _depth;

    
    
    public unsafe DepthStencil() => _depth = new PipelineDepthStencilStateCreateInfo(flags: 0);
    
    
    
    public DepthStencil(DepthStencil cloneFrom) => _depth = cloneFrom._depth;
    
    public IMaterialSettings Clone() => new DepthStencil(this);
    
    
    
        
    public bool DoDepthTest
    {
        get => _depth.DepthTestEnable;
        set => _depth.DepthTestEnable = value;
    }
    public bool DepthWriteEnable
    {
        get => _depth.DepthWriteEnable;
        set => _depth.DepthWriteEnable = value;
    }

    public CompareOp DepthCompareOp
    {
        get => _depth.DepthCompareOp;
        set => _depth.DepthCompareOp = value;
    }

    public bool DoDepthBoundsTest
    {
        get => _depth.DepthBoundsTestEnable;
        set => _depth.DepthBoundsTestEnable = value;
    }

    public bool DoStencilTest
    {
        get => _depth.StencilTestEnable;
        set => _depth.StencilTestEnable = value;
    }

    public StencilOpState Front
    {
        get => _depth.Front;
        set => _depth.Front = value;
    }

    public StencilOpState Back
    {
        get => _depth.Back;
        set => _depth.Back = value;
    }

    public Clamp<float> DepthBounds
    {
        get => new (_depth.MinDepthBounds, _depth.MaxDepthBounds);
        set
        {
            _depth.MinDepthBounds = value.Min;
            _depth.MaxDepthBounds = value.Min;
        }
    }
}