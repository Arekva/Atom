﻿using Silk.NET.Maths;
using Atom.Engine.Vulkan;

namespace Atom.Engine;

public abstract class Material : AtomObject, IMaterial
{
    
#region Handles

    public vk.Pipeline Pipeline { get; protected set; }
    
    public Dictionary<ShaderStageFlags, vk.DescriptorSet>[] DescriptorSets { get; protected set; }

    public vk.Device Device { get; }

#endregion

    public Material(vk.Device? device = null)
    {
        Device = device ?? VK.Device;

        MakeReady();
    }

    public override void Delete()
    {
        base.Delete();

        // Handle destroying
        vk.VkOverloads.DestroyPipeline(VK.API, Device, Pipeline, ReadOnlySpan<vk.AllocationCallbacks>.Empty);
    }
    
#region Commands

    public abstract void CmdBindMaterial(SlimCommandBuffer cmd, Vector2D<u32> extent, u32 cameraIndex, u32 frameIndex);

    public abstract void CmdBindLightMaterial(SlimCommandBuffer cmd, Vector2D<u32> extent, u32 cameraIndex, u32 frameIndex);

#endregion
}