using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine;



public struct SlimPipelineCache
{
    public vk.PipelineCache Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimPipelineCache(Device device, 
        nuint initialDataSize, nint initialData, 
        PipelineCacheCreateFlags flags = 0)
    {
        PipelineCacheCreateInfo create_info = new(
            flags: flags,
            initialDataSize: initialDataSize,
            pInitialData: (void*)initialData
        );
            
        Result result = VK.API.CreatePipelineCache(device, in create_info, null, out Handle);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator PipelineCache(in SlimPipelineCache pipelineCache)
        => Unsafe.As<SlimPipelineCache, PipelineCache>(ref Unsafe.AsRef(in pipelineCache));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimPipelineCache(in PipelineCache pipelineCache)
        => Unsafe.As<PipelineCache, SlimPipelineCache>(ref Unsafe.AsRef(in pipelineCache));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.PipelineCache(in SlimPipelineCache pipelineCache)
        => Unsafe.As<SlimPipelineCache, Silk.NET.Vulkan.PipelineCache>(ref Unsafe.AsRef(in pipelineCache));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy(Device device) => VK.API.DestroyPipelineCache(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Result Merge(Device device, ReadOnlySpan<SlimPipelineCache> sourcePipelines)
    {
        fixed (SlimPipelineCache* p_source_pipelines = sourcePipelines)
        {
            return VK.API.MergePipelineCaches(device, Handle, new(p_source_pipelines, sourcePipelines.Length));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Result GetData(Device device, ref nuint dataSize, nint data) =>
        VK.API.GetPipelineCacheData(device, Handle, ref dataSize, (void*)data);

#endregion

#region User defined



#endregion
}