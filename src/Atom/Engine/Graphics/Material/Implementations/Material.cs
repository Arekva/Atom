using Atom.Engine.Shader;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public abstract class Material : AtomObject, IMaterial
{
    
#region Handles

    public Pipeline Pipeline { get; protected set; }
    
    //public Dictionary<string, DescriptorSetInfo> DescriptorSets { get; protected set; }
    
    

    public Dictionary<ShaderStageFlags, DescriptorSet>[] DescriptorSets { get; protected set; }

    public Device Device { get; }

#endregion

    public Material(Device? device = null)
    {
        Device = device ?? VK.Device;
    }

    public override void Delete()
    {
        base.Delete();
        
        // Handle destroying
        VK.API.DestroyPipeline(Device, Pipeline, ReadOnlySpan<AllocationCallbacks>.Empty);
    }
    
#region Commands

    public abstract void CmdBindMaterial(SlimCommandBuffer cmd, Vector2D<uint> extent, uint cameraIndex, uint frameIndex);
    
#endregion
}