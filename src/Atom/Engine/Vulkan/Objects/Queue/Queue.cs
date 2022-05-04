using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct Queue
{
    private static ConcurrentDictionary<SlimQueue, Device> _queues = new();

    public SlimQueue Handle;
    
    public Device Device => _queues[Handle];
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 



#endregion
}