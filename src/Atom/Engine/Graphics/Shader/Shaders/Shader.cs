﻿using Atom.Engine.Vulkan;

namespace Atom.Engine.Shader;

public abstract partial class Shader : IShader
{
    
#region Handles

    public SlimPipelineLayout PipelineLayout { get; protected init; }
    
    public vk.Device Device { get; }

    public SlimDescriptorPool DescriptorPool { get; protected init; }

    public vk.DescriptorPoolSize[] PoolSizes { get; protected init; }

#endregion
    
#region General Properties

    /// <summary> The namespace of this shader. Allows multiple shaders with different namespaces to share the same
    /// name. </summary>
    public string Namespace { get; protected init; }
    
    /// <summary> Description of this shader. </summary>
    public string? Description { get; protected init; }
    
    /// <summary> The version of this shader. </summary>
    public Version Version { get; protected init; }
    
    public string Name { get; protected init; }

    public Guid GUID { get; protected init; }

#endregion

#region Modules

    /// <summary> Gets a module by a module interface type. </summary>
    /// <param name="type"> The module type to find (i.e. IVertexModule for the vertex module.) </param>
    public abstract IShaderModule? this[Type type] { get; }
    
    /// <summary> All the modules the shader uses. </summary>
    public abstract IEnumerable<IShaderModule> Modules { get; }
    
#endregion

    private bool _disposed = false;

    public Shader(
        string @namespace, string name, string? description, Version version,
        vk.Device? device = null) 
        => (Device, Namespace, Name, Description, Version, GUID) 
        =  (device ?? VK.Device, @namespace, name, description, version, Guid.NewGuid());

    public virtual void Delete()
    {
        if (_disposed) return;
        _disposed = true;
        
        // Dispose internal handles
        DescriptorPool.Destroy(Device);
        
        PipelineLayout.Destroy(Device);
        
        // Dispose all the sub modules
        foreach (IShaderModule module in Modules)
        {
            module.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }

    public void Dispose() => Delete();
    ~Shader() => Dispose();
}