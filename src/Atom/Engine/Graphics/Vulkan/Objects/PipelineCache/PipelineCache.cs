using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public struct PipelineCache
{
    private static ConcurrentDictionary<SlimPipelineCache, Device> _pipelineCaches = new();

    public SlimPipelineCache Handle;
    
    public Device Device => _pipelineCaches[Handle];
    
#region Creation & Non-API stuff

    public PipelineCache(nuint initialDataSize, nint initialData, 
        PipelineCacheCreateFlags flags = 0, Device? device = null)
    {
        Device used_device = device ?? VK.Device;

        Handle = new SlimPipelineCache(used_device, initialDataSize, initialData, flags);

        _pipelineCaches.TryAdd(Handle, used_device);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();

#endregion

#region Standard API Proxying 
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        if(_pipelineCaches.TryRemove(Handle, out Device device))
        {
            Handle.Destroy(device);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Result Merge(ReadOnlySpan<PipelineCache> sourcePipelines)
    {
        fixed (PipelineCache* p_pipeline_caches = sourcePipelines)
        {
            return Handle.Merge(_pipelineCaches[this], new (p_pipeline_caches, sourcePipelines.Length));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Result GetData(ref nuint dataSize, nint data) =>
        Handle.GetData(_pipelineCaches[this], ref dataSize, data);

#endregion
}