//global using static Atom.Engine.Vulkan;

using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public static class VK
{
    // ReSharper disable once InconsistentNaming
    public static readonly vk.Vk API;

    static VK() => API = vk.Vk.GetApi();

    public static Version RequiredVersion { get; set; }

    private static vk.Instance _instance;
    public static vk.Instance Instance => _instance;


    private static vk.Device _device;
    public static vk.Device Device => _device;


    private static GPU _gpu;
    public static GPU GPU => _gpu;

    private static vk.Queue _queue;
    public static vk.Queue Queue => _queue;
    
    public static Version ApplicationVersion { get; private set; }

    public static unsafe void Initialize(
        string engineName,
        Version engineVersion,
        string gameName,
        Version gameVersion,
        
        string[] layers,
        string[] extensions)
    {
        uint vk_api_version = 0;
        API.EnumerateInstanceVersion(ref vk_api_version);

        Version api_version = new (name: "Vulkan", vk_api_version);

        Version version = new ("Vulkan", Vk.Version12);
        
        Log.Info($"Using Vulkan {version.ToString("{M}.{m}")} (API {api_version.ToString("{M}.{m}.{p}")})");

        if (new Version("Vulkan", version.Major, version.Minor, 0, 0) >
            new Version("Vulkan", api_version.Major, api_version.Minor, 0, 0))
        {
            Log.Warning("Application is requiring an higher Vulkan version than available. Instabilities may occur.");
        }

        ApplicationVersion = version;
        
        // sanity check: get if all the layers and extensions are available on this host.
        SanitizeLayerAvailability(layers);
        
        // idem for extensions
        SanitizeExtensionsAvailability(extensions);


        vk.ApplicationInfo app_info = new (
            apiVersion: version,
            pEngineName: LowLevel.GetPointer(engineName ?? throw new ArgumentNullException(nameof(engineName))),
            engineVersion: Version.GetApiVersion(engineVersion),
            pApplicationName: LowLevel.GetPointer(gameName ?? throw new ArgumentNullException(nameof(gameName))),
            applicationVersion: Version.GetApiVersion(gameVersion)
        );


        vk.InstanceCreateInfo instance_info = new (
            pApplicationInfo: &app_info,
            enabledExtensionCount: (uint)extensions.Length,
            ppEnabledExtensionNames: LowLevel.GetPointer(extensions),
            enabledLayerCount: (uint)layers.Length,
            ppEnabledLayerNames: LowLevel.GetPointer(layers)
        );


        API.CreateInstance(in instance_info, null, out _instance);

        // free app_info native data
        LowLevel.Free(app_info.PEngineName);
        LowLevel.Free(app_info.PApplicationName);
        // free instance_info native data
        LowLevel.Free(instance_info.PpEnabledExtensionNames);
        LowLevel.Free(instance_info.PpEnabledLayerNames);

        AutoSelectGPU();
    }

    public static unsafe void Terminate()
    {
        Log.Trace("Terminating Vulkan.");
        //Camera.Clean();
        API.DestroyDevice(_device, null);
        API.DestroyInstance(_instance, null);
    } 

    private static unsafe void SanitizeExtensionsAvailability(string[] extensions)
    {
        if (extensions.Length == 0) return;
        
        uint available_extensions_count = 0;
        API.EnumerateInstanceExtensionProperties((string)null!, ref available_extensions_count, null);
        Span<vk.ExtensionProperties> available_extensions = stackalloc vk.ExtensionProperties[(int)available_extensions_count];
        API.EnumerateInstanceExtensionProperties((string)null!, &available_extensions_count, available_extensions);

        for (int extension_index = 0; extension_index < extensions.Length; extension_index++)
        {
            ref string extension = ref extensions[extension_index];
            bool found = false;
            for (int available_extension_index = 0; available_extension_index < available_extensions_count; available_extension_index++)
            {
                fixed (byte* extension_name = available_extensions[available_extension_index].ExtensionName)
                {
                    if (extension == LowLevel.GetString(extension_name))
                    {
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
            {
                throw new LayerNotFoundException($"Extension {extension} is not available on this host.");
            }
        }
    }
    
    private static unsafe void SanitizeLayerAvailability(string[] layers)
    {
        if (layers.Length == 0) return;
        uint available_layers_count = 0;
        API.EnumerateInstanceLayerProperties(ref available_layers_count, pProperties: null);
        Span<LayerProperties> available_layers = stackalloc LayerProperties[(int)available_layers_count];
        API.EnumerateInstanceLayerProperties(&available_layers_count, available_layers);

        for (int layer_index = 0; layer_index < layers.Length; layer_index++)
        {
            ref string layer = ref layers[layer_index];
            bool found = false;
            for (int available_layer_index = 0; available_layer_index < available_layers_count; available_layer_index++)
            {
                fixed (byte* layer_name = available_layers[available_layer_index].LayerName)
                {
                    if (layer == LowLevel.GetString(layer_name))
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                throw new LayerNotFoundException($"Layer {layer} is not available on this host. " 
#if DEBUG
                                                 + "Try to make sure the layer name is valid.");
#else
                             + "Please check if your GPU does support it on https://vulkan.gpuinfo.org/. If yes, update"
                             + " your drivers and if the error still happen please contact me.");
#endif
            }
        }
    }

    private static unsafe void AutoSelectGPU()
    {
        uint physical_device_count = 0;
        API.EnumeratePhysicalDevices(_instance, ref physical_device_count, null);
        Span<vk.PhysicalDevice> physical_devices = stackalloc vk.PhysicalDevice[(int)physical_device_count];
        API.EnumeratePhysicalDevices(_instance, &physical_device_count, physical_devices);

        GPU best_gpu = null!;
        double best_score = 0;
        for (int i = 0; i < physical_devices.Length; i++)
        {
            GPU gpu = new (_instance, physical_devices[i]);
            
            double score = gpu.Score;

            if (score > best_score)
            {
                best_gpu = gpu;
                best_score = score;
            }
        }
        
        Log.Info($"Using {best_gpu.Name} [{best_gpu.UUID.ToString()}]");

        string[] layers = GetRequiredDeviceLayers();
        string[] extensions = GetRequiredDeviceExtensions();
        vk.DeviceQueueCreateInfo[] queues = GetRequiredQueues();

        //PhysicalDeviceFeatures enabled_features = 
        
        fixed (vk.DeviceQueueCreateInfo* p_queues = queues)
        {
            vk.DeviceCreateInfo info = new (
                enabledExtensionCount: (uint)extensions.Length,
                ppEnabledExtensionNames: LowLevel.GetPointer(extensions),
                enabledLayerCount: (uint)layers.Length,
                ppEnabledLayerNames: LowLevel.GetPointer(layers),
                queueCreateInfoCount: (uint)queues.Length,
                pQueueCreateInfos: p_queues
            );
            
            API.CreateDevice(best_gpu.PhysicalDevice, in info, null, out _device);
            VK.API.GetDeviceQueue(_device, 0, 0, out _queue);
            
            
            //Camera.Init(_device, best_gpu, best_gpu.QueueFamilies[0]);
            
            //VK.API.GetDeviceQueue(_device, 1, 0, out Queue transfer_queue);
            //Mesh.TransferQueue = transfer_queue;
            //Mesh.TransferQueueFamily = best_gpu.QueueFamilies[1];
            
            LowLevel.Free(info.PpEnabledExtensionNames);
            LowLevel.Free(info.PpEnabledLayerNames);
        }

        _gpu = best_gpu;
    }
    
    private static string[] GetRequiredDeviceExtensions()
    {
        List<string> extensions = new();
#if DEBUG
        extensions.AddRange(new string []
        {
            // Silk.NET.Vulkan.Extensions.EXT.ExtDebugUtils.ExtensionName,
        });
#endif
        extensions.AddRange(new string []
        {
            KhrSwapchain.ExtensionName,
        });

        return extensions.Distinct().ToArray();
    }
    
    private static string[] GetRequiredDeviceLayers()
    {
        List<string> layers = new();
#if DEBUG
        layers.AddRange(new [] { "VK_KHR_validation" });
#endif
        return layers.Distinct().ToArray();
    }
    
    private static readonly Pin<float>_queuePriorities = new float[]
    {
        1.00F, // Main graphics render queue, it has to be high priority to maintain good framerate.
        0.75F, // Image streaming and converter
        1.00F,  // Terrain generation compute queue, the priority shouldn't affect the rendering
    };

    private static unsafe vk.DeviceQueueCreateInfo[] GetRequiredQueues() => new vk.DeviceQueueCreateInfo[1] // 3 families on NVIDIA
    {
        new ( // graphic / compute / transfer / sparse binding
            queueFamilyIndex: 0U,
            pQueuePriorities: _queuePriorities.Pointer,
            queueCount: 1
        ),
        
        /*new ( // transfer
            queueFamilyIndex: 1U,
            pQueuePriorities: _queuePriorities.Pointer + 1,
            queueCount: 1
        ),
        
        new ( // compute / transfer / sparse binding
            queueFamilyIndex: 2U,
            pQueuePriorities: _queuePriorities.Pointer + 2,
            queueCount: 1
        ),*/
    };
    
    
    public static uint FindMemoryType(this vk.PhysicalDevice physicalDevice, uint typeFilter, MemoryPropertyFlags properties)
    {
        API.GetPhysicalDeviceMemoryProperties(physicalDevice, out vk.PhysicalDeviceMemoryProperties mem_properties);
        for (int i = 0; i < mem_properties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << i)) > 0 && mem_properties.MemoryTypes[i].PropertyFlags.HasFlag(properties.ToVk()))
            {
                return (uint)i;
            }
        }

        throw new Exception("No suitable memory type found.");
    }

    public static vk.Format FirstSupportedFormat(
        vk.PhysicalDevice physicalDevice,
        ReadOnlySpan<vk.Format> candidates, 
        vk.ImageTiling tiling,
        vk.FormatFeatureFlags features)
    {
        foreach(vk.Format format in candidates)
        {
            API.GetPhysicalDeviceFormatProperties(physicalDevice, format, out vk.FormatProperties props);

            switch (tiling)
            {
                case vk.ImageTiling.Linear when props.LinearTilingFeatures.HasFlag(features): return format;
                case vk.ImageTiling.Optimal when props.OptimalTilingFeatures.HasFlag(features): return format;
            }
        }
        throw new Exception("No format is supported with this tiling.");
    }
}