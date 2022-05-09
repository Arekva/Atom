using System.Runtime.CompilerServices;
using Atom.Engine.Vulkan;

namespace Atom.Engine;

public static class StructExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Span<T> AsSpan<T>(ref this T @struct) where T : struct =>
        new(Unsafe.AsPointer(ref @struct), length: 1);

    public static unsafe (SlimBuffer buffer, VulkanMemory memory) CreateVulkanMemory<T>(
        ref this Span<T> span, vk.Device device, BufferUsageFlags usages, MemoryPropertyFlags properties) where T : unmanaged
    {
        u64 buffer_size = (u64)span.Length * (u64)Unsafe.SizeOf<T>();

        u32 queue_fam = 0;
        
        SlimBuffer buffer = new (device,
            buffer_size,
            usage: usages,
            sharingMode: vk.SharingMode.Exclusive, queue_fam.AsSpan()
        );
        buffer.GetMemoryRequirements(device, out vk.MemoryRequirements reqs);

        VulkanMemory memory = new (
            device: device,
            size: reqs.Size,
            VK.GPU.PhysicalDevice.FindMemoryType(
                typeFilter: reqs.MemoryTypeBits,
                properties: properties
            )
        );

        buffer.BindMemory(memory.Whole);
        
        using (MemoryMap<T> map = memory.Map<T>(memory.Whole))
        {
            fixed (T* p_data = span)
            {
                System.Buffer.MemoryCopy(source: p_data, destination: map, reqs.Size, buffer_size);
            }
        }

        return (buffer, memory);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static vk.ObjectType GetObjectType<T>()
    {
             if (typeof(T) == typeof(SlimCommandBuffer)) return vk.ObjectType.CommandBuffer ;
        else if (typeof(T) == typeof(SlimCommandPool  )) return vk.ObjectType.CommandPool   ;
             
        else if (typeof(T) == typeof(SlimImage        )) return vk.ObjectType.Image         ;
        else if (typeof(T) == typeof(SlimImageView    )) return vk.ObjectType.ImageView     ;
             
        else if (typeof(T) == typeof(SlimDeviceMemory )) return vk.ObjectType.DeviceMemory  ;
             
        else if (typeof(T) == typeof(SlimFence        )) return vk.ObjectType.Fence         ;
        else if (typeof(T) == typeof(SlimSemaphore    )) return vk.ObjectType.Semaphore     ;
             
        else if (typeof(T) == typeof(vk.Instance      )) return vk.ObjectType.Instance      ;
        else if (typeof(T) == typeof(vk.PhysicalDevice)) return vk.ObjectType.PhysicalDevice;
        else if (typeof(T) == typeof(vk.Device        )) return vk.ObjectType.Device        ;
             
        else if (typeof(T) == typeof(SlimQueue        )) return vk.ObjectType.Queue         ;
        else if (typeof(T) == typeof(vk.Queue         )) return vk.ObjectType.Queue         ;
             
        else if (typeof(T) == typeof(vk.SurfaceKHR    )) return vk.ObjectType.SurfaceKhr    ;
             
        else if (typeof(T) == typeof(vk.RenderPass    )) return vk.ObjectType.RenderPass    ;
             
        else                                             throw new Exception("Type not supported.");
    }

    public static unsafe void SetName<T>(this T @struct, string name) where T : struct
    {
#if DEBUG
        VK.API.TryGetInstanceExtension(VK.Instance, out ext.ExtDebugUtils ext);

        u8* p_name = LowLevel.GetPointer(name);
        
        vk.DebugUtilsObjectNameInfoEXT info = new(
            objectType  : GetObjectType<T>(),
            objectHandle: Unsafe.As<T, u64>(ref @struct),
            pObjectName : p_name
        );
        
        ext.SetDebugUtilsObjectName(VK.Device, in info);
        
        LowLevel.Free(p_name);
#endif
    }
}