using System.Runtime.CompilerServices;
using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Engine.Pipelines;

public class GamePipeline : IPipeline
{
    public bool OutputsColor => true;
    
    public bool OutputsDepth => true;

    private Image _workImage;
    private ImageSubresource[] _workSubresources;
    private SlimFramebuffer _framebuffer;

    public ImageSubresource GBufferAlbedoLuminance          => _workSubresources[0];
    public ImageSubresource GBufferNormalRoughnessMetalness => _workSubresources[1];
    public ImageSubresource GBufferPositionTranslucency     => _workSubresources[2];
    

    private vk.Device _device;

    private Vector2D<u32> _resolution;
    private vk.RenderPass _renderPass;

    private static Pin<vk.ClearValue> _clearValues = new vk.ClearValue[]
    {
        new(), new(), new(), new(), new (new vk.ClearColorValue(1.0F, 0.0F, 0.0F, 1.0F))
    };


    public GamePipeline(vk.Device? device = null)
    {
        _device = device ?? VK.Device;

        _workSubresources = new ImageSubresource[3];
    }

    public void Resize(Vector2D<u32> resolution, RenderTarget target)
    {
        if (_resolution == resolution) return;

        u32 queue_family = 0;

        CleanResources();
        
        _workImage = new Image(
            device       : _device                         ,
            format       : ImageFormat.R32G32B32A32_SFloat ,
            resolution   : resolution                      ,
            tiling       : vk.ImageTiling.Optimal          ,
            usage        : ImageUsageFlags.ColorAttachment | 
                           ImageUsageFlags.InputAttachment ,
            queueFamilies: queue_family.AsSpan()         ,
            arrayLayers  : 3U
        );
        _workImage.CreateImage();
        _workImage.CreateMemory(MemoryPropertyFlags.DeviceLocal);
        

        _workSubresources[0] = _workImage.CreateSubresource(arrayLayers: 0..1);
        _workSubresources[1] = _workImage.CreateSubresource(arrayLayers: 1..2);
        _workSubresources[2] = _workImage.CreateSubresource(arrayLayers: 2..3);

        Span<SlimImageView> views = stackalloc SlimImageView[5];
        views[0] = _workSubresources[0];
        views[1] = _workSubresources[1];
        views[2] = _workSubresources[2];
        views[3] = target.Depth!;
        views[4] = target.Color!;
        
        DeferredRenderPass.CreateRenderPass(_device, target.Color!.Format, target.Depth!.Format, out _renderPass);
        
        _framebuffer = new SlimFramebuffer(_device, _renderPass, views, resolution.X, resolution.Y, 1U);

        _resolution = resolution;
    }

    private void CleanResources()
    {
        if (_workImage != null!)
        {
            _framebuffer.Destroy(_device);
            
            for (i32 i = 0; i < _workSubresources.Length; i++)
            {
                _workSubresources[i].Delete();
            }
        
            _workImage.Delete();

            unsafe
            {
                VK.API.DestroyRenderPass(_device, _renderPass, null);
            }
        }
    }

    public void CmdRender(CommandRecorder recorder)
    {
        vk.Rect2D area = new(default, new(_resolution.X, _resolution.Y));
        using (CommandRecorder.RenderPassRecorder pass = 
               recorder.RenderPass(_renderPass, area, _framebuffer, _clearValues.Array))
        {
            pass.NextSubpass();
        }
        
        // first : planet shadows
        // for each planet, render its shadow map (doesn't need to be high-resolution)
        
        // second: planet shine
        // for each planets, get the light from the 
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Delete()
    {
        CleanResources();
        GC.SuppressFinalize(this);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => Delete();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ~GamePipeline() => Dispose();
}