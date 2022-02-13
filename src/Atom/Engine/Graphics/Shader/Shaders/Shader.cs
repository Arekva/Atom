using Silk.NET.Vulkan;

namespace Atom.Engine.Shader;

public abstract partial class Shader : AtomObject, IShader
{
    
#region Handles

    /// <summary> The Vulkan pipeline layout handle this shader represents. </summary>
    public SlimPipelineLayout PipelineLayout { get; protected init; }
    
    

    /// <summary> Vulkan handle for the device owning this Shader. </summary>
    public Device Device { get; }
    

#endregion
    
#region General Properties

    /// <summary> The namespace of this shader. Allows multiple shaders with different namespaces to share the same
    /// name. </summary>
    public string Namespace { get; protected init; }
    
    /// <summary> Description of this shader. </summary>
    public string? Description { get; protected init; }
    
    /// <summary> The version of this shader. </summary>
    public Version Version { get; protected init; }
    
#endregion

#region Modules

    /// <summary> Gets a module by a module interface type. </summary>
    /// <param name="type"> The module type to find (i.e. IVertexModule for the vertex module.) </param>
    public abstract IShaderModule? this[Type type] { get; }
    
    /// <summary> All the modules the shader uses. </summary>
    public abstract IEnumerable<IShaderModule> Modules { get; }
    
#endregion

    public Shader(
        string @namespace, string name, string? description, Version version,
        Device? device = null) 
        => (Device, Namespace, Name, Description, Version) 
        =  (device ?? VK.Device, @namespace, name, description, version);

    public override void Delete()
    {
        // Dispose internal handles
        PipelineLayout.Destroy(Device);
        
        // Dispose all the sub modules
        foreach (IShaderModule module in Modules)
        {
            module.Dispose();
        }
    }
}