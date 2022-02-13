using System.Diagnostics;
using System.Runtime.CompilerServices;
using Silk.NET.Core;
using Silk.NET.Maths;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;


using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public class ViewportWindow : IDisposable
{
    public IWindow Window { get; private set; }

    private volatile bool _isDeleted = false;
    public bool IsDeleted => _isDeleted;

    private KhrSurface _surfaceExtension;
    private SurfaceKHR _surface;
    private bool _hasSurface;
    
    private DeferredRenderer _renderer;
    
    private Device _device;
    private GPU _gpu;
    private QueueFamily _renderFamily;
    
    
    public ViewportWindow()
    {
        _device = VK.Device;
        _gpu = VK.GPU;
        
        WindowOptions options = WindowOptions.DefaultVulkan;
        
        Version vk_version = VK.ApplicationVersion;
        
        // todo: in the future, check if older versions are also supported by the engine so more gpus are supported.
        // api version (vk core 1.0, vk core 1.2, ...)
        options.API = new GraphicsAPI(
            api: ContextAPI.Vulkan, 
            profile: ContextProfile.Core, // used on OpenGL 3.0, not on Vulkan 
            flags: ContextFlags.Default, // idem
            apiVersion: new APIVersion((int)vk_version.Major, (int)vk_version.Minor)
        );

        // vsync (refresh once the screen is ready, to avoid tearing)
        options.VSync = Video.VSync;
        Video.OnVSyncChanged += vsync => Window!.VSync = vsync;
        
        // resolution (/!\ surface resolution, not window !)
        Vector2D<uint> resolution = Video.Resolution;
        options.Size = Unsafe.As<Vector2D<uint>, Vector2D<int>>(source: ref resolution);
        // manually changed: only if the window is resized via the external scripts
        Video.OnResolutionManuallyChanged += resolution =>
        {
            Window!.Size = Unsafe.As<Vector2D<uint>, Vector2D<int>>(source: ref resolution);
            // Resize(resolution: size);
        };
        
        // titlebar text
        options.Title = Video.Title;
        Video.OnTitleChanged += title => Window!.Title = title;
        
        // display mode / window state (minimised, fullscreen, ...)
        options.WindowState = (WindowState)Video.DisplayMode;
        Video.OnDisplayModeChanged += mode => Window!.WindowState = (WindowState)mode;

        // lock to 144 updates/ticks per second for now
        options.UpdatesPerSecond = 0.0D;
        
        // fps - do not use UpdateFPSLimit(bool, double): it is for the runtime window, not create options!
        double framerate = 0.0D;
        if (Video.LimitFPS) framerate = Video.FPSLimit;
        options.FramesPerSecond = framerate;
        // do limit/cap FPS rate - 0 fps/seconds means no limit.
        Video.OnLimitFPSChanged += do_limit => update_fps_limit(do_limit, Video.FPSLimit);
        // fps limit cap
        Video.OnFPSLimitChanged += limit => update_fps_limit(Video.LimitFPS, limit);

        void update_fps_limit(bool doLimit, double limit)
        {
            double framerate = 0.0D;
            if (doLimit) framerate = limit;
            Window!.FramesPerSecond = framerate;
        }
        
        // todo: everything under: move in other classes (vk stuff in engine and deferredrenderer, inputs in its own input setting class + action listeners)
        
        Window = Silk.NET.Windowing.Window.Create(options);
        Window.Update += delta_time =>
        {
            Keyboard.NextFrame();
            ManageKeys();
        };
        
        Window.Load += () => Keyboard.Context = Silk.NET.Input.InputWindowExtensions.CreateInput(Window);
        
        Window.Closing += () =>
        {
            _renderer!.WaitForRenders();
            Dispose();
        };
        
        Window.Resize += resolution =>
        {
            Video.SetResolutionAutoChange(new Vector2D<uint>((uint)resolution.X, (uint)resolution.Y));
            if (TryResize())
            {
                Window.DoRender();
            }
        };
        
        Window.Render += Render;

        
        Window.Initialize();
        if (Window.VkSurface is null) throw new NotSupportedException($"Windowing platform {Window.GetType().Name} does not support Vulkan.");
        
        InitializeVulkanSurface();
        
        _renderer = new DeferredRenderer();
        _renderer.Setup(
            _device,
            VK.GPU.PhysicalDevice,
            _surfaceExtension!, 
            _surface, 
            _renderFamily = VK.GPU.QueueFamilies[0],
            VK.Queue
        );
        
        _fpsWatch.Start();
        //Updater.Go();
        TryResize();
    }

    private bool TryResize()
    {
        
        return _renderer.Update(updateVideoSettings: true);
    }

    private double _minTime = double.NegativeInfinity;
    private double _maxTime = double.PositiveInfinity;
    private List<double> _times = new (10000);
    private Stopwatch _fpsWatch = new();
    private double _showRate = 1.0D;

    private void DoFPS(double deltaTime)
    {
        _minTime = Math.Max(_minTime, deltaTime);
        _maxTime = Math.Min(_maxTime, deltaTime);
        _times.Add(deltaTime);
        
        double elapsed = _fpsWatch.Elapsed.TotalSeconds;
        if (elapsed >= 1.0D / _showRate)
        {
            double avg = _times.Average();
            double min = _minTime;
            double max = _maxTime;

            double[] ordered_times = _times.OrderBy(d => d).ToArray();
            int time_count = ordered_times.Length;

            double tenPct = ordered_times[time_count / 10];
            Log.Info($"[|#FF9100,FPS|] COUNT: {time_count} | AVG: {1.0D/avg:F0} ({avg*1000.0D:F2} ms) | 10% LOW: {1.0D/tenPct:F0} ({tenPct*1000.0D:F2} ms) /// MIN: {1.0D/min:F0} ({min*1000.0D:F2} ms) / MAX: {1.0D/max:F0} ({max*1.000D:F0} ms) ({elapsed:F2} sec)");
            
            _times.Clear();
            _minTime = double.NegativeInfinity;
            _maxTime = double.PositiveInfinity;
            
            _fpsWatch.Restart();
        }
    }

    private void Render(double deltaTime)
    {
        DoFPS(deltaTime);
        
        //Updater.WaitRenderReady();
        _renderer.Render();
    }

    private void InitializeVulkanSurface()
    {
        Instance instance = VK.Instance;
        
        if (!VK.API.TryGetInstanceExtension(instance, out _surfaceExtension))
        {
            throw new NotSupportedException("Instance does not support surfaces.");
        }
        
        unsafe
        {
            _surface = Silk.NET.Vulkan.StructExtensions.ToSurface(Window.VkSurface!.Create<AllocationCallbacks>(
                    Unsafe.As<Instance, VkHandle>(ref instance), null
                )
            );

            Bool32 supportsSurface;
            if (_surfaceExtension.GetPhysicalDeviceSurfaceSupport(_gpu.PhysicalDevice, _renderFamily.Index, _surface,
                    out supportsSurface) == Result.Success)
            {
                _hasSurface = true;
            }
        }
    }

    protected virtual void ManageKeys()
    {
        // toggle fullscreen: alt+enter
        if (Keyboard.IsPressed(Key.AltLeft) && Keyboard.IsPressing(Key.Enter))
        {
            WindowState state = Window.WindowState;
            Window.WindowState = state == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
        }

        if (Keyboard.IsPressing(Key.F11))
        {
            //Screenshot();
        }
    }

    public void Run()
    {
        Window.Run();
    }


    private bool _disposed = false; // avoid double dispose
    public unsafe void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _renderer.Dispose();
        if (_hasSurface)
        {
            _surfaceExtension.DestroySurface(VK.Instance, _surface, null);
        }
        //_surface.Dispose();
        GC.SuppressFinalize(this);
    }

    ~ViewportWindow() => Dispose();
}