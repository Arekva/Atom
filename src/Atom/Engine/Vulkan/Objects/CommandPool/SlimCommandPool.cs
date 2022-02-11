using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public struct SlimCommandPool
{
    public vk.CommandPool Handle; // Opaque type for GPU command buffer

#region Creation & Non-API stuff

    public unsafe SlimCommandPool(
        vk.Device device, 
        uint queueFamilyIndex,
        CommandPoolCreateFlags flags = 0)
    {
        vk.CommandPoolCreateInfo info = new(
            flags: flags.ToVk(),
            queueFamilyIndex: queueFamilyIndex
        );
        VK.API.CreateCommandPool(device, in info, null, out Handle);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator CommandPool(in SlimCommandPool commandPool)
        => Unsafe.As<SlimCommandPool, CommandPool>(ref Unsafe.AsRef(in commandPool));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimCommandPool(in CommandPool commandPool)
        => Unsafe.As<CommandPool, SlimCommandPool>(ref Unsafe.AsRef(in commandPool));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator vk.CommandPool(in SlimCommandPool commandPool)
        => Unsafe.As<SlimCommandPool, Silk.NET.Vulkan.CommandPool>(ref Unsafe.AsRef(in commandPool));

#endregion

#region Standard API Proxying

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Destroy(vk.Device device) => VK.API.DestroyCommandPool(device, Handle, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public vk.Result Reset(vk.Device device, CommandPoolResetFlags flags = 0)
        => VK.API.ResetCommandPool(device, Handle, flags.ToVk());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Trim(vk.Device device) => VK.API.TrimCommandPool(device, Handle, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe vk.Result AllocateCommandBuffers(vk.Device device, CommandBufferLevel level, uint count, SlimCommandBuffer* pCommandBuffers)
    {
        CommandBufferAllocateInfo info = new(commandPool: Handle, level: level.ToVk(), commandBufferCount: count);
        return VK.API.AllocateCommandBuffers(device, in info, (vk.CommandBuffer*)pCommandBuffers);
    }
    

#endregion

#region User defined

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe vk.Result AllocateCommandBuffer(vk.Device device, CommandBufferLevel level, out SlimCommandBuffer commandBuffer)
    {
        CommandBufferAllocateInfo info = new(commandPool: Handle, level: level.ToVk(), commandBufferCount: 1U);
        vk.Result result = VK.API.AllocateCommandBuffers(device, in info, out vk.CommandBuffer vk_cmd);
        commandBuffer = vk_cmd;
        return result;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe vk.Result AllocateCommandBuffers(vk.Device device, CommandBufferLevel level, uint count, Span<SlimCommandBuffer> commandBuffers)
    {
        CommandBufferAllocateInfo info = new(commandPool: Handle, level: level.ToVk(), commandBufferCount: count);
        fixed (SlimCommandBuffer* p_command_buffers = commandBuffers)
        {
            return VK.API.AllocateCommandBuffers(device, in info, (vk.CommandBuffer*)p_command_buffers);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe vk.Result AllocateCommandBuffers(vk.Device device, CommandBufferLevel level, uint count, out SlimCommandBuffer[] commandBuffers)
    {
        CommandBufferAllocateInfo info = new(commandPool: Handle, level: level.ToVk(), commandBufferCount: count);
        commandBuffers = new SlimCommandBuffer[(int)count];
        fixed (SlimCommandBuffer* p_command_buffers = commandBuffers)
        {
            return VK.API.AllocateCommandBuffers(device, in info, (vk.CommandBuffer*)p_command_buffers);
        }
    }

#endregion

}