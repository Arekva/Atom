using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Atom.Engine.GraphicsPipeline;

public class Viewport : IRasterSettings, IDisposable
{
    public static Viewport Default { get; } = new()
    {
        Viewports = new [] { new Silk.NET.Vulkan.Viewport(width: 1.0F, height: 1.0F, minDepth: 0.0F, maxDepth: 1.0F) },
        Scissors = new [] { new Rect2D(extent: new Extent2D(1U, 1U)) }
    };

    
    
    private PipelineViewportStateCreateInfo _viewport;
    
    internal ref PipelineViewportStateCreateInfo State => ref _viewport;
    
    
    private Pin<Rect2D> _scissors;
    private Pin<Silk.NET.Vulkan.Viewport> _viewports;


    public unsafe Viewport()
    {
        _viewport = new (flags: 0);

        _scissors = Array.Empty<Rect2D>();
        _viewports = Array.Empty<Silk.NET.Vulkan.Viewport>();
    }
    
    
    
    public Viewport(Viewport cloneFrom) => _viewport = cloneFrom._viewport;

    public IMaterialSettings Clone()
    {
        return new Viewport(this);
    }

    public void Dispose()
    {
        _scissors.Dispose();
        _viewports.Dispose();
        GC.SuppressFinalize(this);
    }

    ~Viewport() => Dispose();


    public unsafe Silk.NET.Vulkan.Rect2D[] Scissors
    {
        get => _scissors.Array;
        set
        {
            _scissors.Dispose();

            Silk.NET.Vulkan.Rect2D[] data = value ?? Array.Empty<Silk.NET.Vulkan.Rect2D>();

            _scissors = data;

            _viewport.ScissorCount = (uint)data.Length;
            _viewport.PScissors = _scissors;
        }
    }

    public unsafe Silk.NET.Vulkan.Viewport[] Viewports
    {
        get => _viewports.Array;
        set
        {
            _viewports.Dispose();

            Silk.NET.Vulkan.Viewport[] data = value ?? Array.Empty<Silk.NET.Vulkan.Viewport>();

            _viewports = data;

            _viewport.ViewportCount = (uint)data.Length;
            _viewport.PViewports = _viewports;
        }
    }
}