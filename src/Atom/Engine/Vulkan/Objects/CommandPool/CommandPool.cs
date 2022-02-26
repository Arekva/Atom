using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Atom.Engine.Vulkan;

public struct CommandPool
{ 
    private static ConcurrentDictionary<SlimCommandPool, vk.Device> _commandPools = new();
    
    public SlimCommandPool Handle;

    public vk.Device Device => _commandPools[Handle];

#region Creation & Non-API stuff

    public CommandPool(
        uint queueFamilyIndex,
        CommandPoolCreateFlags flags = 0, 
        vk.Device? device = null)
    {
        vk.Device used_device = device ?? VK.Device;

        Handle = new SlimCommandPool(used_device, queueFamilyIndex, flags);

        _commandPools.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy() => Handle.Destroy(_commandPools[Handle]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public vk.Result Reset(CommandPoolResetFlags flags = 0)
        => Handle.Reset(_commandPools[Handle], flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Trim() => Handle.Trim(_commandPools[Handle]);

#endregion

#region User defined

#endregion

}