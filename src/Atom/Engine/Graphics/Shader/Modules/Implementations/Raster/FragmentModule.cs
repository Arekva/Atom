using Silk.NET.Vulkan;
using SPIRVCross;

namespace Atom.Engine.Shader;

public class FragmentModule : ShaderModule, IFragmentModule
{
    public override ShaderStageFlags Stage { get; } = ShaderStageFlags.Fragment;
    
    public FragmentModule(Program program, Device device) : base(program, device) { }
}