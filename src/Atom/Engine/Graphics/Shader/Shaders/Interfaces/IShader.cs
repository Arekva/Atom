using Atom.Engine.Vulkan;

namespace Atom.Engine.Shader;

/// <summary> A global shader interface. It is able to represent all Rasterized, Raytraced and Compute shaders. </summary>
public interface IShader : IDisposable
{
    
#region Handles

    /// <summary> The Vulkan pipeline layout used by the shader. </summary>
    public SlimPipelineLayout PipelineLayout { get; }

    /// <summary> The Vulkan Device handle on which this shader has been created. </summary>
    public vk.Device Device { get; }
    
    /// <summary> The Vulkan descriptor allocator. </summary>
    public SlimDescriptorPool DescriptorPool { get; }

#endregion

#region Other Vulkan

    public vk.DescriptorPoolSize[] PoolSizes { get; }
    
#endregion

#region General Properties
    
    /// <summary> The name of this shader. </summary>
    public string? Name { get; }
    
    /// <summary> The namespace (~ folder) of this shader. </summary>
    public string Namespace { get; }
    
    /// <summary> The description of the shader. </summary>
    public string? Description { get; }
    
    /// <summary> The version of the shader. </summary>
    public Version Version { get; }
    
    /// <summary> the shader's object Global Unique IDentifier. </summary>
    public Guid GUID { get; }
    
#endregion
    
    
#region Modules

    /// <summary> Gets a module by a module interface type. </summary>
    /// <param name="type"> The module type to find (i.e. IVertexModule for the vertex module.) </param>
    public IShaderModule? this[Type type] { get; }
    
    /// <summary> All the modules the shader uses. </summary>
    public IEnumerable<IShaderModule> Modules { get; }
    
#endregion

    public void Delete();
}

