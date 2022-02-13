using Silk.NET.Vulkan;
using SPIRVCross;

namespace Atom.Engine.Shader;

public abstract class ShaderModule : IShaderModule
{
    
#region Handles

    public SlimShaderModule Handle { get; }

    public Device Device { get; }
    
    public SlimDescriptorSetLayout DescriptorLayout { get; private set; }
    
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
        DescriptorLayout.Destroy(Device);
        
        LowLevel.Free(StageInfo.PName);
        Handle.Destroy(Device);
        GC.SuppressFinalize(this);
    }

    ~ShaderModule() => Dispose();


    private static readonly ResourceType[] _validDescriptorBindingResources =
    {
        ResourceType.UniformBuffer, ResourceType.SampledImage, ResourceType.StorageImage, ResourceType.StorageBuffer,
    };

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
        get_resources(ResourceType.StorageImage);
        get_resources(ResourceType.SampledImage);
        get_resources(ResourceType.AtomicCounter);
        get_resources(ResourceType.PushConstant);
        get_resources(ResourceType.SeparateImage);
        get_resources(ResourceType.SeparateSamplers);
        get_resources(ResourceType.AccelerationStructure);
        get_resources(ResourceType.RayQuery);

        int desc_count = 0;
        for (int i = 0; i < _validDescriptorBindingResources.Length; i++)
        {
            desc_count += Descriptors[_validDescriptorBindingResources[i]].Length;
        }

        Span<DescriptorSetLayoutBinding> bindings = stackalloc DescriptorSetLayoutBinding[desc_count];

        int index = 0;
        for (int i = 0; i < _validDescriptorBindingResources.Length; i++)
        {
            ref readonly ResourceType resource_type = ref _validDescriptorBindingResources[i];
            Descriptor[] descriptors = Descriptors[resource_type];

            for (int j = 0; j < descriptors.Length; j++, index++)
            {
                ref readonly Descriptor descriptor = ref descriptors[j];
                
                unsafe
                {
                    bindings[index] =  new DescriptorSetLayoutBinding(
                        binding: descriptor.Binding,
                        descriptorType: _spirvToVkDescMap[resource_type],
                        descriptorCount: descriptor.Array.IsArray ? descriptor.Array.DimensionsLengths[0] : 1U,
                        stageFlags: (vk.ShaderStageFlags)Stage
                    );
                }
            }
        }
        
        DescriptorLayout = new SlimDescriptorSetLayout(
            device: Device,
            bindings: bindings, 
            flags:0);
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
    
    private static Dictionary<ResourceType, DescriptorType> _spirvToVkDescMap = new ()
    {
        { ResourceType.UniformBuffer, DescriptorType.UniformBuffer },
        { ResourceType.SampledImage, DescriptorType.SampledImage },
        { ResourceType.StorageImage, DescriptorType.StorageImage },
        { ResourceType.StorageBuffer, DescriptorType.StorageBuffer },
    };
}