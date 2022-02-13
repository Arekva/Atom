using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public struct ShaderModule
{
    private static ConcurrentDictionary<SlimShaderModule, Device> _shaderModules = new();

    public SlimShaderModule Handle;
    
    public Device Device => _shaderModules[Handle];
    
#region Creation & Non-API stuff

    public ShaderModule(ReadOnlySpan<uint> code, Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimShaderModule(used_device, code);

        _shaderModules.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        if(_shaderModules.TryRemove(Handle, out Device device))
        {
            Handle.Destroy(device);
        }
    }

#endregion
}