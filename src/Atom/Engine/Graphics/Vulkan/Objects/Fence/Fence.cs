using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public struct Fence
{
    private static ConcurrentDictionary<SlimFence, Device> _fences = new();

    public SlimFence Handle;
    
    public Device Device => _fences[Handle];
    
#region Creation & Non-API stuff

    public Fence(bool signaled = false, Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimFence(used_device, signaled);

        _fences.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        if(_fences.TryRemove(Handle, out Device device))
        {
            Handle.Destroy(device);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Result Reset(ReadOnlySpan<Fence> fences)
    {
        int length = fences.Length;
        if (length != 0)
        {
            Device device = _fences[fences[0].Handle];
            fixed (Fence* p_fences = fences)
            {
                return SlimFence.Reset(device, new(p_fences, length));
            }
        }

        return Result.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result GetStatus() => Handle.GetStatus(_fences[Handle]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Result Wait(ReadOnlySpan<Fence> fences, bool waitAll, ulong timeout)
    {
        int length = fences.Length;
        if (length != 0)
        {
            Device device = _fences[fences[0].Handle];
            fixed (Fence* p_fences = fences)
            {
                return SlimFence.Wait(device, new(p_fences, length), waitAll, timeout);
            }
        }

        return Result.Success;
    }
    
#endregion

#region User defined

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result Reset() => Handle.Reset(_fences[Handle]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Result WaitAll(ReadOnlySpan<Fence> fences, ulong timeout = ulong.MaxValue)
    {
        fixed (Fence* p_fences = fences)
        {
            return SlimFence.WaitAll(
                _fences[fences[0].Handle], 
                fences: new ReadOnlySpan<SlimFence>(p_fences, fences.Length), 
                timeout);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Result> WaitAsync(ulong timeout = ulong.MaxValue)
        => await Handle.WaitAsync(_fences[Handle], timeout);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result Wait(ulong timeout = UInt64.MaxValue)
        => Handle.Wait(_fences[Handle], timeout);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<Result> WaitAllAsync(Fence[] fences, ulong timeout = UInt64.MaxValue)
        => await SlimFence.WaitAllAsync(_fences[fences[0].Handle],
            Unsafe.As<Fence[], SlimFence[]>(ref fences),
            timeout);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<Result> WaitAnyAsync(Fence[] fences, ulong timeout = UInt64.MaxValue)
        => await SlimFence.WaitAnyAsync(_fences[fences[0].Handle],
            Unsafe.As<Fence[], SlimFence[]>(ref fences),
            timeout);
    
#endregion

}