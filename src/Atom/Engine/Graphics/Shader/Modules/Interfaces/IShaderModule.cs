using Silk.NET.Vulkan;
using SPIRVCross;

namespace Atom.Engine.Shader;

public interface IShaderModule : IDisposable 
{
    
#region Handles

    /// <summary> The Vulkan handle for this ShaderModule. </summary>
    public SlimShaderModule Handle { get; }
    
    /// <summary> The Vulkan handle for the device where this ShaderModule has been created. </summary>
    public Device Device { get; }
    
#endregion

#region Stage Information

    /// <summary> The module entry function name, often called "main". </summary>
    public string EntryPoint { get; }
    
    /// <summary> The stage where this module operates. </summary>
    public ShaderStageFlags Stage { get; }

    /// <summary> The Vulkan information to create  </summary>
    public PipelineShaderStageCreateInfo StageInfo { get; }
    
#endregion

#region Descriptors

    public Dictionary<ResourceType, Descriptor[]> Descriptors { get; }
    
#endregion

}