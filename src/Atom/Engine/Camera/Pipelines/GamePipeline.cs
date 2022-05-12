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

    private ImageFormat _colorFormat;
    private ImageFormat _depthFormat;

    private FullScreen? _gBufferLit;
    

    public ImageSubresource GBufferAlbedoLuminance          => _workSubresources[0];
    public ImageSubresource GBufferNormalRoughnessMetalness => _workSubresources[1];
    public ImageSubresource GBufferPositionTranslucency     => _workSubresources[2];
    

    private vk.Device _device;

    private Vector2D<u32> _resolution;
    private vk.RenderPass _renderPass;

    private Culler _culler;
    
    

    private static Pin<vk.ClearValue> _clearValues = new vk.ClearValue[]
    {
        new(), new(), new(), new()
    };

    private Drawer.DrawRange[] _drawRanges = new Drawer.DrawRange[1 << 19];

    public GamePipeline(vk.Device? device = null)
    {
        _device = device ?? VK.Device;

        _workSubresources = new ImageSubresource[3];

        _culler = new Culler();
    }

    public void Resize(Vector2D<u32> resolution, RenderTarget target)
    {
        if (_resolution == resolution) return;
        if (resolution.X == 0 || resolution.Y == 0)
        {
            _resolution = resolution;
            return;
        }

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

        ImageFormat target_depth_format = target.DepthFormat;
        ImageFormat target_color_format = target.ColorFormat;

        if (target_depth_format != _depthFormat || target_color_format != _colorFormat)
        {
            _depthFormat = target_depth_format;
            _colorFormat = target_color_format;
            
            _gBufferLit?.Delete();
            
            unsafe
            {
                VK.API.DestroyRenderPass(_device, _renderPass, null);
            }
            
            DeferredRenderPass.CreateRenderPass(_device, target_color_format, target_depth_format, out _renderPass);
            _renderPass.SetName("G-Buffer RenderPass (GamePipeline)");
            
            Graphics.MainRenderPass = _renderPass;
            
            _gBufferLit = new FullScreen("Engine.Deferred", "Light", _renderPass, 1);
        }
        
        _gBufferLit!.SetInputImage("gAlbedo"  , _workSubresources[0]); // albedo   + luminance
        _gBufferLit!.SetInputImage("gNormal"  , _workSubresources[1]); // normal   + roughness    + metalness 🤘
        _gBufferLit!.SetInputImage("gPosition", _workSubresources[2]); // position + translucency
        _gBufferLit!.SetInputImage("gDepth"   , target.Depth        ); // depth (+inf..0) 

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
        }
    }

    public void CmdRender(Camera camera, u32 frameIndex, CommandRecorder recorder, IEnumerable<Drawer> drawers)
    {
        if (_resolution.X == 0 || _resolution.Y == 0) return; // do not attempt to draw anything.
        
        vk.Rect2D draw_area = new(default, new(_resolution.X, _resolution.Y));
        
        // first : planet shadows
        // for each planet, render its shadow map (doesn't need to be high-resolution)
        
        // second: planet shine
        // for each planets, get the light from the other surrounding planets with 1 or more passes.
        
        
        using (CommandRecorder.RenderPassRecorder draw_pass = 
               recorder.RenderPass(_renderPass, draw_area, _framebuffer, _clearValues.Array))
        {
            // G-Buffer
            Span<Drawer.DrawRange> ranges = _drawRanges;
            
            foreach (Drawer drawer in drawers)
            {
                _culler.CullPerspective(camera.Perspective.FieldOfView, _resolution, 
                    drawer.GetMeshes(), // get all mesh data
                    ranges, out i32 culled_count);
                
                drawer.Draw(camera, draw_pass, ranges[..culled_count], _resolution, frameIndex);
            }
            
            draw_pass.NextSubpass();
            
            // todo: light calculation
            // use planet lights + star lights for shadowed directional light
            // ambient light is got from reflection cubemap
            
            _gBufferLit.CmdDraw(recorder, _resolution, frameIndex);
        }
        
        
        
        
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Delete()
    {
        _gBufferLit?.Delete();
        unsafe
        {
            VK.API.DestroyRenderPass(_device, _renderPass, null);
        }
        CleanResources();
        GC.SuppressFinalize(this);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => Delete();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ~GamePipeline() => Dispose();
}