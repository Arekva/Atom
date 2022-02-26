using System.Globalization;
using System.Reflection;
using Silk.NET.GLFW;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public static class Engine
{
#if DEBUG
    public const bool Debug = true;
#else
    public const bool Debug = false;
#endif

    public static string Name { get; set; } = "Atom";
    
    public static Version Version { get; set; } = new Version("Prototype", 0U, 1U, 0U);
    
    
    private static readonly ManualResetEvent _stopResetEvent = new(false);
    private static readonly ManualResetEvent _startResetEvent = new(false);

    public static bool IsGUI { get; private set; } = true;

    public static bool IsRunning { get; private set; } = false;
    
    
    public static void Run(bool gui = true, string? gameAssemblyName = null)
    {
        Thread.CurrentThread.Name = "Main";
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        IsGUI = gui;

        IsRunning = true;
        
        VK.OnInit += () =>
        {
            CameraData.Initialize();
            Draw.Initialize();
        };
        VK.OnTerminate += () =>
        {
            Draw.Cleanup();
            CameraData.Cleanup();
        };
        
        try
        {
            Initialize();
            IsRunning = true;
            //Updater.Run();
            // Do run stuff
            CallGameEntryPoint(gameAssemblyName ?? Assembly.GetExecutingAssembly().GetName().Name!);
        }
        catch (Exception exception)
        {
            Stop(exception);
        }
        finally
        {
            WaitForShutdown();
            IsRunning = false;
            Shutdown();
        }
    }


    private static void Initialize()
    {
        DisplayWelcomeBanner();
        if (IsGUI)
        {
            VK.Initialize(
                Engine.Name,
                Engine.Version,
                Game.Name,
                Game.Version,
                GetRequiredInstanceLayers(), 
                GetRequiredInstanceExtensions()
            );
        }
        else // todo: use OpenCL for server sided compute shaders
        {
            
        }
    }

    private static void Shutdown()
    {
        //Updater.Stop();
        
        VK.Terminate();

        DisplayGoodbyeBanner();
        
        Log.TriggerEngineStop();
    }
    
    public static void Quit() => Stop();
    public static void Abort(Exception e) => Stop(e);

    private static void Stop(Exception? e = null)
    {
        if (e != null)
        {
            Log.Fatal($@"!!|#FF0000,Crash|!!`!` `{e!}`");
            Environment.ExitCode = e!.HResult;
        }

        _startResetEvent.Set();
        _stopResetEvent.Set();
    }
    
    public static void WaitForShutdown() => _stopResetEvent.WaitOne();

    private static unsafe void DisplayWelcomeBanner() => Log.Put($"{Engine.Name} version {Engine.Version} x{sizeof(IntPtr)*8}" + (Debug ? " [DEBUG]" : ""));

    private static void DisplayGoodbyeBanner() => Log.Put("Goodbye.");
    
    
    private static void CallGameEntryPoint(string assemblyName)
    {
        Assembly assembly = AppDomain.CurrentDomain
                                .GetAssemblies()
                                .FirstOrDefault(a => a.GetName().Name == assemblyName)
                            ??
                            throw new Exception($"The game failed to start, no assembly named {assemblyName} found.");

        bool isInitialized = false;
        
        foreach (Type type in assembly.GetTypes())
        {
            foreach (MethodInfo method in type
                         .GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (method.GetCustomAttribute<EntryAttribute>() != null)
                {
                    try
                    {
                        method.Invoke(null, null);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("The game failed to start, an error occured in its initial startup.");
                        Abort(ex);
                    }

                    isInitialized = true;
                    goto end_entry_point_search;
                }
            }
        }

        end_entry_point_search:
        if (!isInitialized)
        {
            throw new Exception("The game failed to start, no entry point has been found: this would result by the engine looping in the void, therefore the initialization is aborted.");
        }
    }
    
    private static string[] GetRequiredInstanceLayers()
    {
        List<string> layers = new();
#if DEBUG
        layers.AddRange(new []
        {
            "VK_LAYER_KHRONOS_validation"
        });
#endif
        return layers.Distinct().ToArray();
    }
    
    private static string[] GetRequiredInstanceExtensions()
    {
        List<string> extensions = new();
        extensions.AddRange(RequiredWindowExtensions());
#if DEBUG
        extensions.AddRange(new []
        {
            "VK_EXT_debug_utils",
            "VK_KHR_display",
        });
#endif
        return extensions.Distinct().ToArray();
    }
    
    private static unsafe string[] RequiredWindowExtensions()
    {
        Glfw glfw = Glfw.GetApi();

        glfw.Init();
        byte** exts = glfw.GetRequiredInstanceExtensions(out uint count);
        return LowLevel.GetStrings(exts, (int)count);
    }
}