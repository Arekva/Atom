﻿using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

using Atom.Engine.Vulkan;

namespace Atom.Engine;

public static class ImageExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BindMemory(this SlimImage Handle, vk.Device device, MemorySegment segment)
        => VK.API.BindImageMemory(device, Handle, segment.Memory, segment.Offset);
    
    public static MemorySegment CreateDedicatedMemory(this SlimImage Handle, vk.Device device, MemoryPropertyFlags properties, bool autoBind = true)
    {
        Handle.GetMemoryRequirements(device, out vk.MemoryRequirements reqs);

        VulkanMemory memory = new (
            device,
            reqs.Size,
            VK.GPU.PhysicalDevice.FindMemoryType(
                reqs.MemoryTypeBits,
                properties
            )
        );

        MemorySegment segment = memory.Whole;
        
        if (autoBind) Handle.BindMemory(device, segment);

        return memory.Whole;
    }
}