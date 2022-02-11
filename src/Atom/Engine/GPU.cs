using Silk.NET.Vulkan;

namespace Atom.Engine;

public class GPU
{
    private static readonly Dictionary<PhysicalDeviceType, double> SCORE_TYPE_MULTIPLIERS = new()
    {
        [PhysicalDeviceType.DiscreteGpu  ] = 1.00D,
        [PhysicalDeviceType.VirtualGpu   ] = 0.20D,
        [PhysicalDeviceType.IntegratedGpu] = 0.10D,
        [PhysicalDeviceType.Cpu          ] = 0.05D,
        [PhysicalDeviceType.Other        ] = 0.001D,
    };
    
    public readonly PhysicalDeviceProperties GeneralProperties;
    
    public readonly PhysicalDeviceIDProperties IDProperties;

    public readonly PhysicalDeviceMemoryProperties MemoryProperties;

    public readonly PhysicalDeviceFeatures Features;
    
    private QueueFamily[] _queueFamilies;

    public QueueFamily[] QueueFamilies => _queueFamilies;
    
    
    public string Name { get; }

    // ReSharper disable once InconsistentNaming
    public Guid UUID { get; }
    
    public double Score
    {
        get
        {   // shit GPU usage score, enough for now.
        
            // get the score based on the local device memory heap (often, discrete gpus with high memory are the
            // newer / faster ones) then multiply by its type

            double score = 0.0D;

            for (int i = 0; i < MemoryProperties.MemoryHeapCount; i++)
            {
                if (MemoryProperties.MemoryHeaps[i].Flags.HasFlag(MemoryHeapFlags.MemoryHeapDeviceLocalBit))
                {
                    score += MemoryProperties.MemoryHeaps[i].Size;
                }
            }

            return score * SCORE_TYPE_MULTIPLIERS[GeneralProperties.DeviceType];
        }
    }

    private Instance _instance;
    public Instance Instance => _instance;
    
    private PhysicalDevice _physicalDevice;
    public PhysicalDevice PhysicalDevice => _physicalDevice;
    
    public unsafe GPU(Instance instance, PhysicalDevice physicalDevice)
    {
        _instance = instance;
        _physicalDevice = physicalDevice;

        
        PhysicalDeviceProperties2
            .Chain(out PhysicalDeviceProperties2 properties)
            .AddNext(out IDProperties);
        VK.API.GetPhysicalDeviceProperties2(physicalDevice, &properties);

        Name = LowLevel.GetString(properties.Properties.DeviceName)!;
        fixed (byte* p_uuid = IDProperties.DeviceUuid)
        {
            UUID = new Guid(new Span<byte>(p_uuid, 16));
        }
        
        VK.API.GetPhysicalDeviceMemoryProperties(physicalDevice, out MemoryProperties);


        PhysicalDeviceFeatures2
            .Chain(out PhysicalDeviceFeatures2 features)
            .AddNext(out PhysicalDeviceIndexTypeUint8FeaturesEXT index_uint8);
        VK.API.GetPhysicalDeviceFeatures2(physicalDevice, &features);
        Features = features.Features;

        uint queue_families_count = 0;
        VK.API.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queue_families_count, null);
        Span<QueueFamilyProperties> queue_families = stackalloc QueueFamilyProperties[(int)queue_families_count];
        fixed (QueueFamilyProperties* p_families = queue_families)
        {
            VK.API.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queue_families_count, p_families);
        }
        
        _queueFamilies = new QueueFamily[queue_families.Length];
        for (int i = 0; i < queue_families.Length; i++)
        {
            _queueFamilies[i] = new QueueFamily(_physicalDevice, queue_families[i], (uint)i);
        }
    }
}