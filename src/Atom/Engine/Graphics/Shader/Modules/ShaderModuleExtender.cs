using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public static class ShaderModuleExtender
{
    private const string DEFAULT_ENTRY_POINT = "main";
    private const PipelineShaderStageCreateFlags DEFAULT_FLAGS = 0;

    public static unsafe PipelineShaderStageCreateInfo CreateStage(this Silk.NET.Vulkan.ShaderModule module, ShaderStageFlags stage, string entryPoint = DEFAULT_ENTRY_POINT, PipelineShaderStageCreateFlags flags = DEFAULT_FLAGS)
    {
        if (entryPoint == null) throw new ArgumentNullException(nameof(entryPoint));
        
        PipelineShaderStageCreateInfo info = new(
            flags: flags,
            module: module,
            stage: (vk.ShaderStageFlags)stage,
            pName: LowLevel.GetPointer(entryPoint)
        );

        return info;
    }
}