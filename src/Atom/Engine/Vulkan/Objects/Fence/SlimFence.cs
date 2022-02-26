using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct SlimFence
{
    public vk.Fence Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimFence(Device device, bool signaled = false)
    {
        FenceCreateInfo create_info = new(
            flags: signaled ? FenceCreateFlags.FenceCreateSignaledBit : 0
        );
        Result result = VK.API.CreateFence(device, in create_info, null, out Handle);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Fence(in SlimFence fence)
        => Unsafe.As<SlimFence, Fence>(ref Unsafe.AsRef(in fence));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimFence(in Fence fence)
        => Unsafe.As<Fence, SlimFence>(ref Unsafe.AsRef(in fence));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.Fence(in SlimFence fence)
        => Unsafe.As<SlimFence, Silk.NET.Vulkan.Fence>(ref Unsafe.AsRef(in fence));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy(Device device) => VK.API.DestroyFence(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Result Reset(Device device, ReadOnlySpan<SlimFence> fences)
    {
        fixed (SlimFence* p_fences = fences)
        {
            return VK.API.ResetFences(device, (uint)fences.Length, (vk.Fence*)p_fences);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result GetStatus(Device device) => VK.API.GetFenceStatus(device, Handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Result Wait(Device device, ReadOnlySpan<SlimFence> fences, bool waitAll, ulong timeout)
    {
        fixed (SlimFence* p_fences = fences)
        {
            return VK.API.WaitForFences(device, (uint)fences.Length, (vk.Fence*)p_fences, waitAll, timeout);
        }
    }
    
#endregion

#region User defined

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result Reset(Device device) => VK.API.ResetFences(device, 1U, in Handle);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Result WaitAll(Device device, ReadOnlySpan<SlimFence> fences, ulong timeout = UInt64.MaxValue)
    {
        fixed (SlimFence* p_fences = fences)
        {
            return VK.API.WaitForFences(device, 
                (uint)fences.Length, (Silk.NET.Vulkan.Fence*)p_fences, 
                true, timeout);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Result> WaitAsync(Device device, ulong timeout = UInt64.MaxValue)
    {
        Silk.NET.Vulkan.Fence handle = Handle;
        return await Task.Run(()
            => VK.API.WaitForFences(device, 1U, in handle, true, timeout));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result Wait(Device device, ulong timeout = UInt64.MaxValue) 
        => VK.API.WaitForFences(device, 1U, in Handle, true, timeout);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<Result> WaitAllAsync(Device device, SlimFence[] fences, ulong timeout = UInt64.MaxValue)
        => await Task.Run(() => VK.API.WaitForFences(device, 
            (uint)fences.Length, Unsafe.As<SlimFence[], Silk.NET.Vulkan.Fence[]>(ref fences), 
            true, timeout));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<Result> WaitAnyAsync(Device device, SlimFence[] fences, ulong timeout = UInt64.MaxValue)
        => await Task.Run(() => VK.API.WaitForFences(device, 
            (uint)fences.Length, Unsafe.As<SlimFence[], Silk.NET.Vulkan.Fence[]>(ref fences), 
            false, timeout));
    
#endregion
}