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
}