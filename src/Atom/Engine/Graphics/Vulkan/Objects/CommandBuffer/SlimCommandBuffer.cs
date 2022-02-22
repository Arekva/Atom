using System.Runtime.CompilerServices;

namespace Atom.Engine;

public struct SlimCommandBuffer
{
    public vk.CommandBuffer Handle; // Opaque type for GPU command buffer

#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator CommandBuffer(in SlimCommandBuffer commandBuffer)
        => Unsafe.As<SlimCommandBuffer, CommandBuffer>(ref Unsafe.AsRef(in commandBuffer));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimCommandBuffer(in CommandBuffer commandBuffer)
        => Unsafe.As<CommandBuffer, SlimCommandBuffer>(ref Unsafe.AsRef(in commandBuffer));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator vk.CommandBuffer(in SlimCommandBuffer commandBuffer)
        => Unsafe.As<SlimCommandBuffer, vk.CommandBuffer>(ref Unsafe.AsRef(in commandBuffer));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimCommandBuffer(in vk.CommandBuffer commandBuffer)
        => Unsafe.As<vk.CommandBuffer, SlimCommandBuffer>(ref Unsafe.AsRef(in commandBuffer));

#endregion

#region Standard API Proxying

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly vk.Result Reset(vk.CommandBufferResetFlags flags = 0) => VK.API.ResetCommandBuffer(Handle, flags);

#endregion

#region User defined

    #endregion

}