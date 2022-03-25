using System.Runtime.CompilerServices;

namespace Atom.Engine.Vulkan;

public struct SlimBuffer
{
    public vk.Buffer Handle; // Opaque type for GPU buffer

#region Creation & Non-API stuff

    public unsafe SlimBuffer(
        vk.Device device, 
        ulong size, 
        BufferUsageFlags usage, 
        vk.SharingMode sharingMode,
        ReadOnlySpan<uint> queueFamilyIndices, 
        BufferCreateFlags flags = 0)
    {
        fixed (uint* p_queue_families = queueFamilyIndices)
        {
            vk.BufferCreateInfo info = new(
                flags: flags.ToVk(), size: size, usage: usage.ToVk(), sharingMode: sharingMode, 
                queueFamilyIndexCount: (uint)queueFamilyIndices.Length, 
                pQueueFamilyIndices: p_queue_families);

            vk.Result result = VK.API.CreateBuffer(device, in info, null, out Handle);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Buffer(in SlimBuffer buffer)
        => Unsafe.As<SlimBuffer, Buffer>(ref Unsafe.AsRef(in buffer));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimBuffer(in Buffer buffer)
        => Unsafe.As<Buffer, SlimBuffer>(ref Unsafe.AsRef(in buffer));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.Buffer(in SlimBuffer buffer)
        => Unsafe.As<SlimBuffer, Silk.NET.Vulkan.Buffer>(ref Unsafe.AsRef(in buffer));

#endregion

#region Standard API Proxying

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe void Destroy(vk.Device device) => VK.API.DestroyBuffer(device, Handle, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void GetMemoryRequirements(vk.Device device, out vk.MemoryRequirements reqs)
        => VK.API.GetBufferMemoryRequirements(device, Handle, out reqs);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public vk.Result BindMemory(vk.Device device, vk.DeviceMemory memory, ulong offset)
        => VK.API.BindBufferMemory(device, Handle, memory, offset);

#endregion
    
#region User defined



#endregion

}