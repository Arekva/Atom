using System.Text;
using Silk.NET.Vulkan;

using SPIRVCross;
using Type = SPIRVCross.Type;

namespace Atom.Engine.Shader;

public class VertexModule : ShaderModule, IVertexModule
{
    
#region Descriptors

    public Dictionary<uint, VertexInput> VertexInputs { get; private set; }
    
#endregion
    
    
    public override ShaderStageFlags Stage { get; } = ShaderStageFlags.Vertex;
    
    public VertexModule(Program program, Device device) : base(program, device) { }

    protected override void Reflect(Program program)
    {
        base.Reflect(program);

        VertexInputs = new Dictionary<uint, VertexInput>();

        foreach (Descriptor descriptor in Descriptors[ResourceType.StageInput])
        {
            Format format = descriptor.GetDefaultVkFormat();


            uint binding = descriptor.Binding;
            uint location = descriptor.Location;
            

            uint total_resource_width = (descriptor.BitWidth / 8) + descriptor.Offset;
            
            
            if (VertexInputs.TryGetValue(binding, out VertexInput input))
            { // group vertex inputs with each other, by binding.
                input.Binding.Stride += total_resource_width;
                input.Attributes.Add(location, 
                    new VertexInputAttribute
                    {
                        Name = descriptor.Name,
                        Description = new  VertexInputAttributeDescription(
                            location: location, 
                            binding: binding, 
                            format: format
                        )
                    }
                );

                VertexInputs[binding] = input;
            }
            else
            {
                VertexInputs.Add(binding, new VertexInput
                {
                    Attributes = new Dictionary<uint, VertexInputAttribute>
                    {
                        { 
                            location, 
                            
                            new VertexInputAttribute
                            {
                                Name = descriptor.Name,
                                Description = new  VertexInputAttributeDescription(
                                    location: location, 
                                    binding: binding, 
                                    format: format
                                )
                            }
                        }
                    },
                    Binding = new VertexInputBindingDescription(
                        binding: binding,
                        stride: total_resource_width,
                        inputRate: VertexInputRate.Vertex // defaults by Vertex rate
                    )
                });
            }
        }
    }
}