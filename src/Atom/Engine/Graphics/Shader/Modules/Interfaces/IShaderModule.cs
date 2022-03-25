using SPIRVCross;
using Atom.Engine.Vulkan;

namespace Atom.Engine.Shader;

public interface IShaderModule : IDisposable 
{
    
#region Handles

    /// <summary> The Vulkan handle for this ShaderModule. </summary>
    public SlimShaderModule Handle { get; }
    
    /// <summary> The Vulkan handle for the device where this ShaderModule has been created. </summary>
    public vk.Device Device { get; }
    
    /// <summary> The Vulkan handle for the module descriptor layout this ShaderModule's API contains. </summary>
    /// This is basically: "for x binding, how many resource of y type is there?" 
    public SlimDescriptorSetLayout DescriptorSetLayout { get; }
    
#endregion

#region Stage Information

    /// <summary> The module entry function name, often called "main". </summary>
    public string EntryPoint { get; }
    
    /// <summary> The stage where this module operates. </summary>
    public ShaderStageFlags Stage { get; }

    /// <summary> The Vulkan information to create  </summary>
    public vk.PipelineShaderStageCreateInfo StageInfo { get; }
    
#endregion

#region Descriptors

    public Dictionary<ResourceType, Descriptor[]> Descriptors { get; }
    public Dictionary<string, Descriptor> NamedDescriptors { get; }
    
    public Dictionary<string, vk.PushConstantRange> PushConstants { get; }

#endregion

}