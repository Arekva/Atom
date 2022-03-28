using System.Runtime.CompilerServices;
using Atom.Engine.Vulkan;

namespace Atom.Engine;

public class LightData
{
    public const u32 MAX_LIGHTS_COUNT = 1024;
    
#region Handles
    
    public static VulkanMemory LightListMemory;

    public static SlimBuffer LightList;

    private static vk.Device Device;

#endregion

    private static MemoryMap<LightGLSL> LightListMap;
    
    private static Action<u32>? OnFrameUpdateInternal;
    
    public static void UpdateFrame(u32 frameIndex)
    {
        OnFrameUpdateInternal?.Invoke(frameIndex);
    }
    
    
    public u32 Index { get; }
    
    public Action<u32> OnFrameUpdate;
    
    public LightData(u32 index)
    {
        Index = index;

        OnFrameUpdateInternal += FrameUpdate; // always assigned by owner light
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FrameUpdate(u32 frameIndex) => OnFrameUpdate!(frameIndex);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Update(in LightGLSL lightGlsl, u32 frameIndex) 
        => ((LightGLSL*)LightListMap)[Index * Graphics.MaxFramesCount + frameIndex] = lightGlsl;
    
    
    public static unsafe void Initialize(vk.Device? device = null)
    {
        vk.Device used_device = Device = device ?? VK.Device;

        u32 queue_family = 0U;
        LightList = new SlimBuffer(
            device: used_device,
            size: (u64)(MAX_LIGHTS_COUNT * sizeof(LightGLSL) * Graphics.MaxFramesCount),
            usage: BufferUsageFlags.StorageBuffer,
            sharingMode: vk.SharingMode.Exclusive, queue_family.AsSpan(), 
            flags: 0
        );

        LightList.GetMemoryRequirements(used_device, out vk.MemoryRequirements reqs);

        LightListMemory = new VulkanMemory(
            device: used_device,
            size  : reqs.Size,
            memoryTypeIndex: VK.GPU.PhysicalDevice.FindMemoryType(reqs.MemoryTypeBits,
                properties: MemoryPropertyFlags.DeviceLocal | MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent)
        );

        LightList.BindMemory(LightListMemory.Whole);

        LightListMap = LightListMemory.Map<LightGLSL>();
    }

    public static void Cleanup()
    {
        LightListMap.Dispose();
        
        LightList.Destroy(Device);
        
        LightListMemory.Dispose();
    }

    public void Dispose()
    {
        OnFrameUpdateInternal -= FrameUpdate;
        
        GC.SuppressFinalize(this);
    }

    ~LightData() => Dispose();
}