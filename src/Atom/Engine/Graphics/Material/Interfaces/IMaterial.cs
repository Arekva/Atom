using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public interface IMaterial : IDisposable
{
    
#region Handle

    /// <summary> The Vulkan graphic pipeline handle of this material. </summary>
    public Pipeline Pipeline { get; }
    
    /// <summary> The Vulkan device handle owning this material. </summary>
    public Device Device { get; }

    public Dictionary<ShaderStageFlags, DescriptorSet>[] DescriptorSets { get; }
    
    //public Dictionary<string, DescriptorSetInfo> DescriptorSets { get; }
    
#endregion

#region Commands

    public void CmdBindMaterial(SlimCommandBuffer cmd, Vector2D<uint> extent, uint cameraIndex, uint frameIndex);

#endregion
    
}