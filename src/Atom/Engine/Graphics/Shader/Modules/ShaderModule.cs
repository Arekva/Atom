using Silk.NET.Vulkan;
using SPIRVCross;

namespace Atom.Engine.Shader;

public abstract class ShaderModule : IShaderModule
{
    
#region Handles

    public SlimShaderModule Handle { get; }

    public Device Device { get; }
    
#endregion

#region Stage Information

    public string EntryPoint { get; protected set; }
    
    public abstract ShaderStageFlags Stage { get; }
    
    public PipelineShaderStageCreateInfo StageInfo { get; private set; }
    
#endregion

#region Descriptors

    public Dictionary<ResourceType, Descriptor[]> Descriptors { get; private set; }

#endregion

    public unsafe ShaderModule(Program program, Device? device = null)
    {
        this.Device = device ?? VK.Device;

        fixed (byte* p_code = program.Source)
        {
            this.Handle = new SlimShaderModule(
                device: this.Device, 
                code:   new ReadOnlySpan<uint>(p_code,
                length: program.Source.Length/sizeof(uint))
            );
        }
        
        Reflect(program);
    }

    public unsafe void Dispose()
    {
        LowLevel.Free(StageInfo.PName);
        Handle.Destroy(Device);
        GC.SuppressFinalize(this);
    }

    ~ShaderModule() => Dispose();

    protected virtual void Reflect(Program program)
    {
        SetStageInfo(program);

        Descriptors = new Dictionary<ResourceType, Descriptor[]>(capacity: 12);
        
        void get_resources(ResourceType resourceType)
        {
            Descriptors.Add(
                key:   resourceType, 
                value: program.GetResources(resourceType).Select(type => new Descriptor(type)).ToArray()
            );
        }

        get_resources(ResourceType.UniformBuffer);
        get_resources(ResourceType.StorageBuffer);
        get_resources(ResourceType.StageInput);
        get_resources(ResourceType.StageOutput);
        get_resources(ResourceType.SubpassInput);
        get_resources(ResourceType.SampledImage);
        get_resources(ResourceType.AtomicCounter);
        get_resources(ResourceType.PushConstant);
        get_resources(ResourceType.SeparateImage);
        get_resources(ResourceType.SeparateSamplers);
        get_resources(ResourceType.AccelerationStructure);
        get_resources(ResourceType.RayQuery);
    }

    private unsafe void SetStageInfo(Program program)
    {
        // assume there is an entry point, and that it is the right execution model.
        // this may not be true, but enough for now (one file per module design) 
        EntryPoint = program.GetEntryPoint().Name; 
        StageInfo = new PipelineShaderStageCreateInfo(
            stage: (vk.ShaderStageFlags)Stage, 
            module: this.Handle,
            pName: LowLevel.GetPointer(EntryPoint)
        );
    }
}