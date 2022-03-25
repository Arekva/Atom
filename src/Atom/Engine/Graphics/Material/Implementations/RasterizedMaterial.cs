using System.Runtime.CompilerServices;
using Silk.NET.Maths;

using Atom.Engine.Vulkan;
using Atom.Engine.Shader;

namespace Atom.Engine;

public class RasterizedMaterial : Material, IRasterizedMaterial
{
    public IRasterShader Shader { get; private set; }
    
#region Settings

    public Topology Topology { get; set; } = Topology.Default;
    
    public Tessellation Tessellation { get; set; } = Tessellation.Default;
    
    public Viewport Viewport { get; set; } = Viewport.Default;
    
    public Rasterizer Rasterizer { get; set; } = Rasterizer.Default;
    
    public Multisampling Multisampling { get; set; } = Multisampling.Default;
    
    public DepthStencil DepthStencil { get; set; } = DepthStencil.Default;
    
    public ColorBlending ColorBlending { get; set; } = ColorBlending.Default;
    
#endregion

#region Dynamic State Configuration

    private static readonly Pin<int> DynamicStates 
        = new int[] { (int)vk.DynamicState.Viewport, (int)vk.DynamicState.Scissor };

    private static readonly unsafe vk.PipelineDynamicStateCreateInfo DynamicStateInfo = new(
        flags: 0,
        dynamicStateCount: (uint)DynamicStates.Size,
        pDynamicStates: (vk.DynamicState*)DynamicStates.Pointer
    );

#endregion

    private readonly uint _moduleCount;

    private readonly Queue<(vk.DescriptorBufferInfo buffer, vk.WriteDescriptorSet write)>[] _writeEdits;
    

    public RasterizedMaterial(IRasterShader shader, vk.Device? device = null) : this(shader, default, device) { }
 
    private RasterizedMaterial(IRasterShader shader, vk.Pipeline basePipeline, vk.Device? device = null) : base(device)
    {
        Shader = shader ?? throw new ArgumentNullException(nameof(shader));

        DescriptorSets = new Dictionary<ShaderStageFlags, vk.DescriptorSet>[Graphics.MaxFramesCount];
        _writeEdits = new Queue<(vk.DescriptorBufferInfo, vk.WriteDescriptorSet)>[Graphics.MaxFramesCount];
        for (int i = 0; i < Graphics.MaxFramesCount; i++)
        {
            _writeEdits[i] = new Queue<(vk.DescriptorBufferInfo, vk.WriteDescriptorSet)>(capacity: 1024);
        }

        Span<ShaderStageFlags> descriptor_set_stages = stackalloc ShaderStageFlags[7];
        Span<SlimDescriptorSetLayout> descriptor_set_layouts = stackalloc SlimDescriptorSetLayout[7];
        
        int module_count = 0;
        foreach (IShaderModule module in shader.Modules)
        {
            descriptor_set_layouts[module_count] = module.DescriptorSetLayout;
            descriptor_set_stages[module_count] = module.Stage;
            module_count++;
        }
        _moduleCount = (uint)module_count;

        unsafe
        {
            fixed (SlimDescriptorSetLayout* p_desc_layouts = descriptor_set_layouts)
            {
                vk.DescriptorSetAllocateInfo desc_alloc_info = new(
                    descriptorPool: shader.DescriptorPool,
                    descriptorSetCount: (uint)module_count,
                    pSetLayouts: (vk.DescriptorSetLayout*)p_desc_layouts
                );

                for (int i = 0; i < Graphics.MaxFramesCount; i++)
                {
                    Dictionary<ShaderStageFlags, vk.DescriptorSet> stage_sets = new (capacity: module_count);
                    DescriptorSets[i] = stage_sets;

                    vk.DescriptorSet* p_sets = stackalloc vk.DescriptorSet[module_count];
                    
                    vk.Result result = VK.API.AllocateDescriptorSets(shader.Device, in desc_alloc_info, p_sets);

                    for (int j = 0; j < module_count; j++)
                    {
                        stage_sets.Add(descriptor_set_stages[j], p_sets[j]);
                    }
                }
            }
        }

        CreatePipeline(shader.Device, basePipeline);
    }

    public override unsafe void Delete()
    {
        base.Delete();

        uint total_sets_count = Graphics.MaxFramesCount * _moduleCount;
        
        Span<vk.DescriptorSet> sets = stackalloc vk.DescriptorSet[(int)total_sets_count];
        for (uint i = 0; i < Graphics.MaxFramesCount; i++)
        {
            uint j = 0;
            foreach (vk.DescriptorSet set in DescriptorSets[i].Values)
            {
                uint index = AMath.To1D(j, i, _moduleCount);
                sets[(int) index] = set;
                ++j;
            }
        }
        
        vk.VkOverloads.FreeDescriptorSets(VK.API, Device, Shader.DescriptorPool, total_sets_count, sets);

        Shader = null!;
    }

    public RasterizedMaterial Clone() => new (Shader, Pipeline);
    
    public override unsafe void CmdBindMaterial(SlimCommandBuffer cmd, Vector2D<uint> extent, uint cameraIndex, uint frameIndex)
    {
        vk.Viewport viewport = new(width: extent.X, height: extent.Y, minDepth: 0.0F, maxDepth: 1.0F);
        vk.Rect2D scissor = new(extent: new vk.Extent2D(extent.X, extent.Y));

        VK.API.CmdBindPipeline(cmd, vk.PipelineBindPoint.Graphics, Pipeline);
        VK.API.CmdSetViewport(cmd, 0U, 1U, in viewport);
        VK.API.CmdSetScissor(cmd, 0U, 1U, in scissor);

        Span<uint> pushs = stackalloc uint[2];
        pushs[0] = cameraIndex;
        pushs[1] = frameIndex;
        // set camera & frame index
        VK.API.CmdPushConstants(cmd, Shader.PipelineLayout, (vk.ShaderStageFlags)ShaderStageFlags.Vertex, 0U, pushs);

        Span<vk.DescriptorSet> sets = stackalloc vk.DescriptorSet[(int)_moduleCount];

        int desc_index = 0;
        foreach (vk.DescriptorSet set in DescriptorSets[frameIndex].Values)
        {
            sets[desc_index] = set;
            ++desc_index;
        }
        
        VK.API.CmdBindDescriptorSets(
            cmd, 
            vk.PipelineBindPoint.Graphics, 
            Shader.PipelineLayout,
            firstSet: 0U,
            sets, 
            dynamicOffsetCount: 0U, 
            pDynamicOffsets: null);
    }
    
    

    private unsafe void CreatePipeline(vk.Device device, vk.Pipeline basePipeline)
    {
        vk.PipelineShaderStageCreateInfo[] stages = Shader.Modules.Select(module => module.StageInfo).ToArray();


        List<vk.VertexInputAttributeDescription> attribs_list = new();
        List<vk.VertexInputBindingDescription> bindings_list = new();
        foreach ((uint _, VertexInput input) in Shader.VertexModule.VertexInputs)
        {
            bindings_list.Add(input.Binding);
            attribs_list.AddRange(input.Attributes.Values
                .Select(a => a.Description)
                .OrderBy(d => d.Location));
        }

        vk.VertexInputAttributeDescription[] attribs = attribs_list.ToArray();
        vk.VertexInputBindingDescription[] bindings = bindings_list.ToArray();

        vk.PipelineDynamicStateCreateInfo dynamic_state = DynamicStateInfo;
        
        fixed (vk.VertexInputAttributeDescription* p_attribs = attribs)
        fixed (vk.VertexInputBindingDescription* p_bindings = bindings)
        {
            vk.PipelineVertexInputStateCreateInfo input_state_info = new(
                flags: 0,
                vertexAttributeDescriptionCount: (uint)attribs.Length,
                pVertexAttributeDescriptions: p_attribs,

                vertexBindingDescriptionCount: (uint)bindings.Length,
                pVertexBindingDescriptions: p_bindings
            );

            fixed (vk.PipelineShaderStageCreateInfo* p_stages = stages)
            {
                vk.GraphicsPipelineCreateInfo info = new(
                    flags:               0,
                    stageCount:          (uint)stages.Length,
                    pStages:             p_stages,
                    pVertexInputState:   &input_state_info,
                    pInputAssemblyState: (vk.PipelineInputAssemblyStateCreateInfo*)Unsafe.AsPointer(ref Topology.State),
                    pTessellationState:  (vk.PipelineTessellationStateCreateInfo*)Unsafe.AsPointer(ref Tessellation.State),
                    pViewportState:      (vk.PipelineViewportStateCreateInfo*)Unsafe.AsPointer(ref Viewport.State),
                    pRasterizationState: (vk.PipelineRasterizationStateCreateInfo*)Unsafe.AsPointer(ref Rasterizer.State),
                    pMultisampleState:   (vk.PipelineMultisampleStateCreateInfo*)Unsafe.AsPointer(ref Multisampling.State),
                    pDepthStencilState:  (vk.PipelineDepthStencilStateCreateInfo*)Unsafe.AsPointer(ref DepthStencil.State),
                    pColorBlendState:    (vk.PipelineColorBlendStateCreateInfo*)Unsafe.AsPointer(ref ColorBlending.State),
                    pDynamicState:       &dynamic_state,
                    layout:              Shader.PipelineLayout,
                    renderPass:          Graphics.MainRenderPass,
                    subpass:             Graphics.MainSubpass,
                    basePipelineHandle:  basePipeline,
                    basePipelineIndex:   0
                );

                vk.Result result = VK.API.CreateGraphicsPipelines(
                   device, default,
                   1U, in info,
                   null, out vk.Pipeline pipeline);

                Pipeline = pipeline;
            }
        }
    }

    /*public void WriteData<TStage, TData>(string name, in TData data)
        where TStage : IRasterModule
        where TData  : unmanaged
    {
        ShaderStageFlags module_type = ModuleAttribute.InterfaceStageMap[typeof(TStage)];

        for (i32 i = 0; i < Graphics.MaxFramesCount; i++)
        {
            _writeEdits[i].Enqueue(());
        }
    }*/

    public unsafe void WriteBuffer<TStage>(string name, BufferSubresource subresource, 
        vk.DescriptorType descriptorType = vk.DescriptorType.StorageBuffer)
        where TStage : IRasterModule
    {
        ShaderStageFlags module_type = ModuleAttribute.InterfaceStageMap[typeof(TStage)];
        
        Descriptor desc = Shader[typeof(TStage)]!.NamedDescriptors[name];

        /*Pin<*/vk.DescriptorBufferInfo/*>*/ buffer_info = new (
            buffer: subresource.Buffer,
            offset: subresource.Segment.Offset,
            range : subresource.Segment.Size 
        );
        
        for (i32 frame_index = 0; frame_index < Graphics.MaxFramesCount; frame_index++)
        {
            

            vk.WriteDescriptorSet write_descriptor = new(
                dstSet         : DescriptorSets[frame_index][module_type],
                dstBinding     : desc.Binding,
                dstArrayElement: 0,
                descriptorCount: 1,
                descriptorType : descriptorType,
                pBufferInfo    : &buffer_info
            );
            
            vk.VkOverloads.UpdateDescriptorSets(VK.API, Device, 
                1U, &write_descriptor, 
                0U, ReadOnlySpan<vk.CopyDescriptorSet>.Empty);
        }
    }

    public unsafe void WriteImage<TStage>(string name, Texture texture,
        vk.DescriptorType descriptorType = vk.DescriptorType.CombinedImageSampler) where TStage : IRasterModule
    {
        ShaderStageFlags module_type = ModuleAttribute.InterfaceStageMap[typeof(TStage)];
        
        Descriptor desc = Shader[typeof(TStage)]!.NamedDescriptors[name];
        
        vk.DescriptorImageInfo image_info = new(
            sampler  : texture.Sampler.Sampler,
            imageView: texture.Subresource.View,
            imageLayout: texture.Subresource.Image.Layout
        );
        
        for (i32 frame_index = 0; frame_index < Graphics.MaxFramesCount; frame_index++)
        {
            vk.WriteDescriptorSet write_descriptor = new(
                dstSet         : DescriptorSets[frame_index][module_type],
                dstBinding     : desc.Binding,
                dstArrayElement: 0,
                descriptorCount: 1,
                descriptorType : descriptorType,
                pImageInfo     : &image_info
            );
            
            vk.VkOverloads.UpdateDescriptorSets(VK.API, Device, 
                1U, &write_descriptor, 
                0U, ReadOnlySpan<vk.CopyDescriptorSet>.Empty);
        }
    }
}