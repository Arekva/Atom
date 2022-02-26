using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct Semaphore
{
    private static ConcurrentDictionary<SlimSemaphore, Device> _semaphores = new();

    public SlimSemaphore Handle;
    
    public Device Device => _semaphores[Handle];
    
#region Creation & Non-API stuff

    public Semaphore(
        SemaphoreCreateFlags flags = 0,
        Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimSemaphore(used_device, flags);

        _semaphores.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        if(_semaphores.TryRemove(Handle, out Device device))
        {
            Handle.Destroy(device);
        }
    }

#endregion
}