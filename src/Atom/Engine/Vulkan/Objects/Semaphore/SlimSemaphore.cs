using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct SlimSemaphore
{
    public vk.Semaphore Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimSemaphore(
        Device device,
        SemaphoreCreateFlags flags = 0)
    {
        SemaphoreCreateInfo create_info = new(flags: (uint)flags);
        
        Result result = VK.API.CreateSemaphore(device, in create_info, null, out Handle);
    }

    public static unsafe SlimSemaphore CreateTimeline(Device device, u64 initial_value, SemaphoreCreateFlags flags = 0)
    {
        SemaphoreCreateInfo create_info = new(flags: (uint)flags);
        create_info.AddNext(out vk.SemaphoreTypeCreateInfo type_info);
        type_info.InitialValue = initial_value;
        type_info.SemaphoreType = vk.SemaphoreType.Timeline;
        
        Result result = VK.API.CreateSemaphore(device, in create_info, null, out vk.Semaphore semaphore);

        return Unsafe.As<vk.Semaphore, SlimSemaphore>(ref semaphore);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Semaphore(in SlimSemaphore view)
        => Unsafe.As<SlimSemaphore, Semaphore>(ref Unsafe.AsRef(in view));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimSemaphore(in Semaphore view)
        => Unsafe.As<Semaphore, SlimSemaphore>(ref Unsafe.AsRef(in view));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.Semaphore(in SlimSemaphore view)
        => Unsafe.As<SlimSemaphore, Silk.NET.Vulkan.Semaphore>(ref Unsafe.AsRef(in view));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimSemaphore(in Silk.NET.Vulkan.Semaphore view)
        => Unsafe.As<Silk.NET.Vulkan.Semaphore, SlimSemaphore>(ref Unsafe.AsRef(in view));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy(Device device) 
        => VK.API.DestroySemaphore(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Result Signal(Device device, ulong value)
    {
        vk.SemaphoreSignalInfo signal_info = new(
            semaphore: Handle,
            value: value
        );
        return VK.API.SignalSemaphore(device, in signal_info);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result Signal(Device device, out ulong value)
        => VK.API.GetSemaphoreCounterValue(device, Handle, out value);

    public unsafe Result Wait(Device device, ulong value, ulong timeout = ulong.MaxValue)
    {
        vk.Semaphore semaphore = Handle;
        SemaphoreWaitInfo wait_info = new(
            flags: 0,
            semaphoreCount: 1U,
            pSemaphores: &semaphore,
            pValues: &value
        );
        return VK.API.WaitSemaphores(device, in wait_info, timeout);
    }
    
#endregion

#region User defined

    public static unsafe Result Wait(Device device,
        ReadOnlySpan<SlimSemaphore> semaphores, ReadOnlySpan<ulong> values, 
        ulong timeout = ulong.MaxValue)
    {
        fixed (SlimSemaphore* p_semaphores = semaphores)
        fixed (ulong* p_values = values)
        {
            SemaphoreWaitInfo wait_info = new(
                flags: 0,
                semaphoreCount: 1U,
                pSemaphores: (vk.Semaphore*)p_semaphores,
                pValues: p_values
            );
            return VK.API.WaitSemaphores(device, in wait_info, timeout);
        }
    }
    
#endregion
}