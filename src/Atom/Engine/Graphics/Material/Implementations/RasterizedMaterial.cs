using System.Runtime.CompilerServices;
using Silk.NET.Maths;

using Atom.Engine.Vulkan;
using Atom.Engine.Shader;
using Atom.Engine.GraphicsPipeline;

namespace Atom.Engine;

public class RasterizedMaterial : Material, IRasterizedMaterial
{
    public IRasterShader Shader { get; private set; }
    
#region Settings

    public Topology Topology { get; set; } = Topology.Default;
    
    public Tessellation Tessellation { get; set; } = Tessellation.Default;
    
    public Atom.Engine.GraphicsPipeline.Viewport Viewport { get; set; } = Atom.Engine.GraphicsPipeline.Viewport.Default;
    
    public Rasterizer Rasterizer { get; set; } = Rasterizer.Default;
    
    public Multisampling Multisampling { get; set; } = Multisampling.Default;
    
    public DepthStencil DepthStencil { get; set; } = DepthStencil.Default;
    
    public ColorBlending ColorBlending { get; set; } = ColorBlending.Default;

    public bool CastShadows { get; set; } = true;
    
#endregion

#region Dynamic State Configuration

    private static readonly Pin<i32> DynamicStates 
        = new [] { (i32)vk.DynamicState.Viewport, (i32)vk.DynamicState.Scissor };

    private static readonly unsafe vk.PipelineDynamicStateCreateInfo DynamicStateInfo = new(
        flags: 0,
        dynamicStateCount: (u32)DynamicStates.Size,
        pDynamicStates: (vk.DynamicState*)DynamicStates.Pointer
    );

#endregion

    private vk.Pipeline _lightPipeline;

    private readonly u32 _moduleCount;

    private readonly bool _hasLightShader;

    private readonly Queue<(vk.DescriptorBufferInfo buffer, vk.WriteDescriptorSet write)>[] _writeEdits;

    private vk.Pipeline _basePipeline;

    private readonly vk.RenderPass _renderPass;

    private readonly u32 _subpass;
    

    public RasterizedMaterial(IRasterShader shader, vk.RenderPass? renderPass = null, u32? subpass = null, bool createPipeline = true) : this(shader, default, renderPass, subpass, createPipeline) { }
 
    private RasterizedMaterial(IRasterShader shader, vk.Pipeline basePipeline, vk.RenderPass? renderPass = null, u32? subpass = null, bool createPipeline = true)
    {
        Shader = shader ?? throw new ArgumentNullException(nameof(shader));
        _hasLightShader = shader.LightShader != null;
        
        DescriptorSets = new Dictionary<ShaderStageFlags, vk.DescriptorSet>[Graphics.MaxFramesCount];
        _writeEdits = new Queue<(vk.DescriptorBufferInfo, vk.WriteDescriptorSet)>[Graphics.MaxFramesCount];
        for (i32 i = 0; i < Graphics.MaxFramesCount; i++)
        {
            _writeEdits[i] = new Queue<(vk.DescriptorBufferInfo, vk.WriteDescriptorSet)>(capacity: 1024);
        }

        Span<ShaderStageFlags> descriptor_set_stages = stackalloc ShaderStageFlags[7];
        Span<SlimDescriptorSetLayout> descriptor_set_layouts = stackalloc SlimDescriptorSetLayout[7];
        
        int module_count = 0;
        foreach (IShaderModule module in shader.Modules)
        {
            if (module.DescriptorSetLayout.Handle.Handle == 0U) continue;
            
            descriptor_set_layouts[module_count] = module.DescriptorSetLayout;
            descriptor_set_stages[module_count] = module.Stage;
            module_count++;
        }
        _moduleCount = (u32)module_count;

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

        _renderPass   = renderPass ?? Graphics.MainRenderPass;
        _subpass      = subpass    ?? Graphics.MainSubpass;
        _basePipeline = basePipeline;
        
        if (createPipeline)
        {
            CreatePipeline();
        }

        if (_hasLightShader && CastShadows)
        {
            CreateLightPipeline(shader.LightShader!.Device);
        }
    }

    public override unsafe void Delete()
    {
        base.Delete();

        if (_hasLightShader)
        {
            VK.API.DestroyPipeline(Shader.LightShader!.Device, _lightPipeline, null);
        }

        u32 total_sets_count = Graphics.MaxFramesCount * _moduleCount;
        
        Span<vk.DescriptorSet> sets = stackalloc vk.DescriptorSet[(int)total_sets_count];
        for (u32 i = 0; i < Graphics.MaxFramesCount; i++)
        {
            u32 j = 0;
            foreach (vk.DescriptorSet set in DescriptorSets[i].Values)
            {
                u32 index = AMath.To1D(j, i, _moduleCount);
                sets[(i32) index] = set;
                ++j;
            }
        }
        
        vk.VkOverloads.FreeDescriptorSets(VK.API, Device, Shader.DescriptorPool, total_sets_count, sets);

        Shader = null!;
    }

    public RasterizedMaterial Clone() => new (Shader, Pipeline);

    public unsafe void CmdBindMaterial(SlimCommandBuffer cmd, Vector2D<uint> extent, uint frameIndex)
    {
        vk.Viewport viewport = new(width: extent.X, height: extent.Y, minDepth: 0.0F, maxDepth: 1.0F);
        vk.Rect2D scissor = new(extent: new vk.Extent2D(extent.X, extent.Y));

        VK.API.CmdBindPipeline(cmd, vk.PipelineBindPoint.Graphics, Pipeline);
        VK.API.CmdSetViewport(cmd, 0U, 1U, in viewport);
        VK.API.CmdSetScissor(cmd, 0U, 1U, in scissor);
        
        Span<vk.DescriptorSet> sets = stackalloc vk.DescriptorSet[(i32)_moduleCount];

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
    
    public override unsafe void CmdBindLightMaterial(SlimCommandBuffer cmd, Vector2D<u32> extent, u32 cameraIndex, u32 frameIndex)
    {
        if (!_hasLightShader || !CastShadows) return;
        
        vk.Viewport viewport = new(width: extent.X, height: extent.Y, minDepth: 0.0F, maxDepth: 1.0F);
        vk.Rect2D scissor = new(extent: new vk.Extent2D(extent.X, extent.Y));

        VK.API.CmdBindPipeline(cmd, vk.PipelineBindPoint.Graphics, _lightPipeline);
        VK.API.CmdSetViewport(cmd, 0U, 1U, in viewport);
        VK.API.CmdSetScissor(cmd, 0U, 1U, in scissor);

        Span<u32> pushs = stackalloc u32[2];
        pushs[0] = cameraIndex;
        pushs[1] = frameIndex;
        // set camera & frame index
        VK.API.CmdPushConstants(cmd, Shader.LightShader!.PipelineLayout, (vk.ShaderStageFlags)ShaderStageFlags.Vertex, 0U, pushs);

        Span<vk.DescriptorSet> sets = stackalloc vk.DescriptorSet[(i32)_moduleCount];

        i32 desc_index = 0;
        foreach (vk.DescriptorSet set in DescriptorSets[frameIndex].Values)
        {
            sets[desc_index] = set;
            ++desc_index;
        }
        
        VK.API.CmdBindDescriptorSets(
            cmd, 
            vk.PipelineBindPoint.Graphics, 
            Shader.LightShader!.PipelineLayout,
            firstSet: 0U,
            sets, 
            dynamicOffsetCount: 0U, 
            pDynamicOffsets: null);
    }

    private unsafe void CreateLightPipeline(vk.Device device)
    {
        IRasterShader light_shader = Shader.LightShader!;
        
        vk.PipelineShaderStageCreateInfo[] stages = light_shader.Modules.Select(module => module.StageInfo).ToArray();
        
        List<vk.VertexInputAttributeDescription> attribs_list = new();
        List<vk.VertexInputBindingDescription> bindings_list = new();
        foreach ((u32 _, VertexInput input) in light_shader.VertexModule.VertexInputs)
        {
            bindings_list.Add(input.Binding);
            attribs_list.AddRange(input.Attributes.Values
                .Select(a => a.Description)
                .OrderBy(d => d.Location));
        }

        vk.VertexInputAttributeDescription[] attribs = attribs_list.ToArray();
        vk.VertexInputBindingDescription[] bindings = bindings_list.ToArray();

        vk.PipelineDynamicStateCreateInfo dynamic_state = DynamicStateInfo;

        vk.Pipeline base_pipeline = new (0);
        
        fixed (vk.VertexInputAttributeDescription* p_attribs = attribs)
        fixed (vk.VertexInputBindingDescription* p_bindings = bindings)
        {
            vk.PipelineVertexInputStateCreateInfo input_state_info = new(
                flags: 0,
                vertexAttributeDescriptionCount: (u32)attribs.Length,
                pVertexAttributeDescriptions: p_attribs,

                vertexBindingDescriptionCount: (u32)bindings.Length,
                pVertexBindingDescriptions: p_bindings
            );
            
            Rasterizer rasterizer = new()
            {
                CullMode = vk.CullModeFlags.CullModeFrontBit, LineWidth = 1.0F
            };
            Multisampling multisampling = new()
            {
                DoMultisampling = false,
                Count = vk.SampleCountFlags.SampleCount1Bit
            };
            DepthStencil depth_stencil = new()
            {
                DoDepthTest = true,
                DepthCompareOp = vk.CompareOp.Greater,
                DepthWriteEnable = true,
                DepthBounds = new Clamp<float>(0.0F, 1.0F)
            };
            ColorBlending color_blending = new() { DoLogicOperator = false };

            fixed (vk.PipelineShaderStageCreateInfo* p_stages = stages)
            {
                vk.GraphicsPipelineCreateInfo info = new(
                    flags:               0,
                    stageCount:          (u32)stages.Length,
                    pStages:             p_stages,
                    pVertexInputState:   &input_state_info,
                    pInputAssemblyState: (vk.PipelineInputAssemblyStateCreateInfo*)Unsafe.AsPointer(ref Topology.State),
                    pTessellationState:  (vk.PipelineTessellationStateCreateInfo*)Unsafe.AsPointer(ref Tessellation.State),
                    pViewportState:      (vk.PipelineViewportStateCreateInfo*)Unsafe.AsPointer(ref Viewport.State),
                    pRasterizationState: (vk.PipelineRasterizationStateCreateInfo*)Unsafe.AsPointer(ref rasterizer.State),
                    pMultisampleState:   (vk.PipelineMultisampleStateCreateInfo*)Unsafe.AsPointer(ref multisampling.State),
                    pDepthStencilState:  (vk.PipelineDepthStencilStateCreateInfo*)Unsafe.AsPointer(ref depth_stencil.State),
                    pColorBlendState:    (vk.PipelineColorBlendStateCreateInfo*)Unsafe.AsPointer(ref color_blending.State),
                    pDynamicState:       &dynamic_state,
                    layout:              light_shader.PipelineLayout,
                    renderPass:          Graphics.MainRenderPass,
                    subpass:             Graphics.MainSubpass,
                    basePipelineHandle:  base_pipeline,
                    basePipelineIndex:   0
                );

                vk.Result result = VK.API.CreateGraphicsPipelines(
                   light_shader.Device, default,
                   1U, in info,
                   null, out vk.Pipeline pipeline);

                _lightPipeline = pipeline;
            }
        }
    }

    public void CreatePipeline()
    {
        CreatePipeline(Shader.Device, _basePipeline, _renderPass, _subpass);
        _basePipeline = default;
        
        MakeReady();
    }

    private unsafe void CreatePipeline(vk.Device device, vk.Pipeline basePipeline, vk.RenderPass? renderPass = null, u32? subpass = null)
    {
        vk.PipelineShaderStageCreateInfo[] stages = Shader.Modules.Select(module => module.StageInfo).ToArray();


        List<vk.VertexInputAttributeDescription> attribs_list = new();
        List<vk.VertexInputBindingDescription> bindings_list = new();
        foreach (VertexInput input in Shader.VertexModule.VertexInputs.Values)
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
                vk.PipelineRasterizationStateCreateInfo rasterization_state = Rasterizer.State;
                vk.GraphicsPipelineCreateInfo info = new(
                    flags:               0,
                    stageCount:          (uint)stages.Length,
                    pStages:             p_stages,
                    pVertexInputState:   &input_state_info,
                    pInputAssemblyState: (vk.PipelineInputAssemblyStateCreateInfo*)Unsafe.AsPointer(ref Topology.State),
                    pTessellationState:  (vk.PipelineTessellationStateCreateInfo*)Unsafe.AsPointer(ref Tessellation.State),
                    pViewportState:      (vk.PipelineViewportStateCreateInfo*)Unsafe.AsPointer(ref Viewport.State),
                    pRasterizationState: (vk.PipelineRasterizationStateCreateInfo*)Unsafe.AsPointer(ref rasterization_state),
                    pMultisampleState:   (vk.PipelineMultisampleStateCreateInfo*)Unsafe.AsPointer(ref Multisampling.State),
                    pDepthStencilState:  (vk.PipelineDepthStencilStateCreateInfo*)Unsafe.AsPointer(ref DepthStencil.State),
                    pColorBlendState:    (vk.PipelineColorBlendStateCreateInfo*)Unsafe.AsPointer(ref ColorBlending.State),
                    pDynamicState:       &dynamic_state,
                    layout:              Shader.PipelineLayout,
                    renderPass:          renderPass ?? Graphics.MainRenderPass,
                    subpass:             subpass    ?? Graphics.MainSubpass   ,
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
        vk.DescriptorType descriptorType = vk.DescriptorType.CombinedImageSampler, u32? frameIndex = null) where TStage : IRasterModule
    {
        ShaderStageFlags module_type = ModuleAttribute.InterfaceStageMap[typeof(TStage)];
        
        Descriptor desc = Shader[typeof(TStage)]!.NamedDescriptors[name];
        
        vk.DescriptorImageInfo image_info = new(
            sampler    : texture.Sampler.Sampler       ,
            imageView  : texture.Subresource.View      ,
            imageLayout: texture.Subresource.Image.Layout
        );

        if (frameIndex == null)
        {
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
        else
        {
            vk.WriteDescriptorSet write_descriptor = new(
                 dstSet         : DescriptorSets[frameIndex.Value][module_type],
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

    public unsafe void WriteInput(string name, ImageSubresource image, u32 frameIndex, vk.ImageLayout layout)
    {
        if (Shader[typeof(IFragmentModule)] == null)
        {
            throw new Exception(
                "Material's shader does not contain any fragment module. Shader inputs can only be used on fragment modules.");
        }
        
        Descriptor desc = Shader[typeof(IFragmentModule)]!.NamedDescriptors[name];

        vk.DescriptorImageInfo image_info = new(
            imageView  : image.View       ,
            sampler    : null               , // Inputs don't have samplers
            imageLayout: layout
        );
        
        vk.WriteDescriptorSet write_descriptor = new(
            dstSet         : DescriptorSets[frameIndex][ShaderStageFlags.Fragment],
            dstBinding     : desc.Binding,
            dstArrayElement: 0,
            descriptorCount: 1,
            descriptorType : vk.DescriptorType.InputAttachment,
            
            pImageInfo: &image_info
        );
        
        vk.VkOverloads.UpdateDescriptorSets(VK.API, Device, 
            1U, write_descriptor.AsSpan(), 
            0U, ReadOnlySpan<vk.CopyDescriptorSet>.Empty);
    }
}