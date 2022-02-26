using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace Atom.Engine.Vulkan;

public struct SlimShaderModule
{
    public vk.ShaderModule Handle;
    
#region Creation & Non-API stuff

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe SlimShaderModule(Device device, ReadOnlySpan<uint> code)
    {
        fixed (uint* p_code = code)
        {
            ShaderModuleCreateInfo create_info = new(
                pCode: p_code,
                codeSize: (uint)code.Length * sizeof(uint)
            );
            
            Result result = VK.API.CreateShaderModule(device, in create_info, null, out Handle);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Handle.GetHashCode();
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ShaderModule(in SlimShaderModule shaderModule)
        => Unsafe.As<SlimShaderModule, ShaderModule>(ref Unsafe.AsRef(in shaderModule));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SlimShaderModule(in ShaderModule shaderModule)
        => Unsafe.As<ShaderModule, SlimShaderModule>(ref Unsafe.AsRef(in shaderModule));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Silk.NET.Vulkan.ShaderModule(in SlimShaderModule shaderModule)
        => Unsafe.As<SlimShaderModule, Silk.NET.Vulkan.ShaderModule>(ref Unsafe.AsRef(in shaderModule));

#endregion
    
#region Standard API Proxying 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy(Device device) => VK.API.DestroyShaderModule(device, Handle, ReadOnlySpan<AllocationCallbacks>.Empty);

#endregion

#region User defined


    
#endregion
}