using Silk.NET.Maths;
using Atom.Engine.Vulkan;
using Silk.NET.Vulkan;

namespace Atom.Engine;

// ReSharper disable once InconsistentNaming
public unsafe class GBufferDrawer : IDisposable
{
    
#region Static Configuration

    private static readonly byte* _pName = LowLevel.GetPointer("main");

    private static readonly Pin<Silk.NET.Vulkan.Viewport> _dummyViewport = new Silk.NET.Vulkan.Viewport(
        x: 0, y: 0,
        width: 1.0F, height: 1.0F,
        minDepth: 0.0F, maxDepth: 0.0F
    );
    
    private static readonly Pin<vk.Rect2D> _dummyScissor = new vk.Rect2D(default, new vk.Extent2D(1, 1));

    private static readonly Pin<vk.PipelineColorBlendAttachmentState> _colorBlendAttachment = 
        new vk.PipelineColorBlendAttachmentState();

    private struct NDCDynamicStates
    {
        public vk.DynamicState viewport = vk.DynamicState.Viewport, scissor = vk.DynamicState.Scissor;
        
        public NDCDynamicStates() { }
    }
    private static readonly Pin<NDCDynamicStates> _dynamicStates = new NDCDynamicStates();



    private static readonly Pin<vk.PipelineVertexInputStateCreateInfo> _vertexInput = 
        new vk.PipelineVertexInputStateCreateInfo(vk.StructureType.PipelineVertexInputStateCreateInfo);

    private static readonly Pin<vk.PipelineInputAssemblyStateCreateInfo> _inputAssembly = 
        new vk.PipelineInputAssemblyStateCreateInfo(topology: vk.PrimitiveTopology.TriangleList);
    
    private static readonly Pin<vk.PipelineTessellationStateCreateInfo> _tessellation = 
        new vk.PipelineTessellationStateCreateInfo();

    private static readonly Pin<vk.PipelineViewportStateCreateInfo> _viewport = new 
        vk.PipelineViewportStateCreateInfo(
        viewportCount: 1, pViewports: _dummyViewport,
        scissorCount: 1, pScissors: _dummyScissor
    );

    private static readonly Pin<vk.PipelineRasterizationStateCreateInfo> _rasterization = 
        new vk.PipelineRasterizationStateCreateInfo(
        lineWidth: 1.0F,
        cullMode: vk.CullModeFlags.CullModeBackBit
    );

    private static readonly Pin<vk.PipelineMultisampleStateCreateInfo> _multisample = 
        new vk.PipelineMultisampleStateCreateInfo(rasterizationSamples: vk.SampleCountFlags.SampleCount1Bit);

    private static readonly Pin<vk.PipelineDepthStencilStateCreateInfo> _depthStencil = 
        new vk.PipelineDepthStencilStateCreateInfo();

    private static readonly Pin<vk.PipelineColorBlendStateCreateInfo> _colorBlend = 
        new vk.PipelineColorBlendStateCreateInfo(attachmentCount: 1 /* other properties set in static constructor */);

    private static readonly Pin<vk.PipelineDynamicStateCreateInfo> _dynamicState = 
        new vk.PipelineDynamicStateCreateInfo(
            dynamicStateCount: 2,  pDynamicStates: (Silk.NET.Vulkan.DynamicState*)(NDCDynamicStates*)_dynamicStates
    );

#endregion

    private static readonly Pin<vk.DescriptorPoolSize> _descriptorImageCount = 
        new vk.DescriptorPoolSize(vk.DescriptorType.InputAttachment,  descriptorCount: 4U);


    
    private vk.Device _device;
    private uint _maxImages;
    private Vector2D<uint> _extent;

    // destroyable resources
    private SlimPipelineLayout _layout;
    private vk.WriteDescriptorSet[] _writeDescriptorSets;
    private SlimDescriptorPool _descriptorPool;
    private vk.Pipeline _pipeline;
    private SlimDescriptorSetLayout _inTextureDescriptor;
    
    private vk.DescriptorSet[] _descriptorSets;
    
    static GBufferDrawer() // Setup constant objects
    {
        /* set non compile dependant properties for the PipelineColorBlendStateCreateInfo */
        _colorBlend.Data.BlendConstants[0] = 
        _colorBlend.Data.BlendConstants[1] =
        _colorBlend.Data.BlendConstants[2] = 
        _colorBlend.Data.BlendConstants[3] = 1.0F;

        _colorBlendAttachment.Data.ColorWriteMask = vk.ColorComponentFlags.ColorComponentRBit |
                                                    vk.ColorComponentFlags.ColorComponentGBit |
                                                    vk.ColorComponentFlags.ColorComponentBBit |
                                                    vk.ColorComponentFlags.ColorComponentABit;
        
        _colorBlend.Data.PAttachments = _colorBlendAttachment;
    }

    public GBufferDrawer(uint maxImages, vk.Device device)
    {
        _device = device;
        _maxImages = maxImages;
        
        // Load Pipeline Stages
        Span<vk.PipelineShaderStageCreateInfo> modules = stackalloc vk.PipelineShaderStageCreateInfo[2];
        
        void make_shader(string path, ShaderStageFlags stage, out vk.PipelineShaderStageCreateInfo info)
        {
            using FileStream stream = File.OpenRead(path);

            // very small shaders, don't need to allocate on heap
            Span<byte> spv = stackalloc byte[4096];
            int spv_size = stream.Read(spv);
            
            fixed (byte* p_spv = spv)
            {
                vk.ShaderModuleCreateInfo vertex_info = new(
                    codeSize: (uint)spv_size,
                    pCode: (uint*)p_spv
                );

                info = new vk.PipelineShaderStageCreateInfo(
                    pName: _pName,
                    stage: (vk.ShaderStageFlags)stage
                );
                VK.API.CreateShaderModule(device, in vertex_info, null, out info.Module);
            }
        }
        
        make_shader("Assets/Shaders/Engine/Deferred/Modules/deferred.frag.spv", 
            ShaderStageFlags.Fragment, out modules[0]); /* Fragment module */
        make_shader("Assets/Shaders/Engine/Deferred/Modules/deferred.vert.spv", 
            ShaderStageFlags.Vertex, out modules[1]);   /* Vertex   module */
        
        _descriptorPool = new SlimDescriptorPool(device, maxImages, new(_descriptorImageCount, 1));

        Span<vk.DescriptorSetLayoutBinding> set_layout_bindings = stackalloc vk.DescriptorSetLayoutBinding[4];
        for (int i = 0; i < 4; i++)
        {
            set_layout_bindings[i] = new(
                binding: (uint)i,
                descriptorCount: 1,
                descriptorType: vk.DescriptorType.InputAttachment,
                stageFlags: (vk.ShaderStageFlags)ShaderStageFlags.Fragment
            );
        }

        _inTextureDescriptor = new SlimDescriptorSetLayout(_device, set_layout_bindings);
        
        
        Span<SlimDescriptorSetLayout> descriptor_set_layouts = stackalloc SlimDescriptorSetLayout[(int)maxImages];
        for (int i = 0; i < maxImages; i++)
        {
            descriptor_set_layouts[i] = _inTextureDescriptor;
        }

        _descriptorSets = new vk.DescriptorSet[maxImages];
        fixed (SlimDescriptorSetLayout* p_descriptor_set_layouts = descriptor_set_layouts)
        {
            vk.DescriptorSetAllocateInfo set_allocate_info = new(
                descriptorPool: _descriptorPool,
                descriptorSetCount: maxImages,
                pSetLayouts: (vk.DescriptorSetLayout*)p_descriptor_set_layouts
            );
            VK.API.AllocateDescriptorSets(device, &set_allocate_info, _descriptorSets.AsSpan());
        }

        _writeDescriptorSets = new vk.WriteDescriptorSet[maxImages*4];

        for (u32 i = 0; i < maxImages; i++)
        {
            for (u32 j = 0; j < 4; j++)
            {
                u32 index = i * 4 + j;
                
                _writeDescriptorSets[index] = new vk.WriteDescriptorSet(
                    dstSet         : _descriptorSets[i]               ,
                    dstBinding     : j                                ,
                    dstArrayElement: 0                                ,
                    descriptorType : vk.DescriptorType.InputAttachment,
                    descriptorCount: 1
                
                    // set ImageInfo later, in CmdDrawView() 
                );
            }
        }

        SlimDescriptorSetLayout gbuffer_layout = _inTextureDescriptor;


        _layout = new SlimPipelineLayout(_device, new(&gbuffer_layout, 1), 
            ReadOnlySpan<vk.PushConstantRange>.Empty);
        
        CreatePipeline(modules);
        
        for (int i = 0; i < 2; i++)
        {
            VK.API.DestroyShaderModule(device, modules[i].Module, null);
        }
    }

    private void CreatePipeline(ReadOnlySpan<vk.PipelineShaderStageCreateInfo> stages)
    {
        fixed(vk.PipelineShaderStageCreateInfo* p_stages = stages)
        {
            vk.GraphicsPipelineCreateInfo info = new(
                flags: 0,
                stageCount: 2,
                pStages: p_stages,
                pVertexInputState: _vertexInput,
                pInputAssemblyState: _inputAssembly,
                pTessellationState: _tessellation,
                pViewportState: _viewport,
                pRasterizationState: _rasterization,
                pMultisampleState: _multisample,
                pDepthStencilState: _depthStencil,
                pColorBlendState: _colorBlend,
                pDynamicState: _dynamicState,
                layout: _layout,
                renderPass: Graphics.MainRenderPass,
                subpass: 1U,
                basePipelineHandle: null,
                basePipelineIndex: 0
            );
            
            vk.Pipeline pipeline = default;
            VK.API.CreateGraphicsPipelines(_device, default, 1, &info, null, &pipeline);
            _pipeline = pipeline;
        }
    }
    
    public void Resize(Vector2D<uint> extent) => _extent = extent;

    public void CmdComputeGBuffer(SlimCommandBuffer cmd, uint swapImageIndex, ReadOnlySpan<SlimImageView> gbuffer)
    {
        vk.DescriptorSet set = _descriptorSets[swapImageIndex];

        int write_index = (int)swapImageIndex * 4; 
        Span<vk.WriteDescriptorSet> sets = _writeDescriptorSets.AsSpan()[write_index..(write_index + 4)];
        vk.DescriptorImageInfo* descriptor_infos = stackalloc vk.DescriptorImageInfo[4];
        for (int i = 0; i < 4; i++)
        {
            ref vk.WriteDescriptorSet img_set = ref sets[i];
            descriptor_infos[i] = new vk.DescriptorImageInfo(
                imageView: gbuffer[i],
                imageLayout: vk.ImageLayout.ShaderReadOnlyOptimal
            );
            img_set.PImageInfo = &descriptor_infos[i];
        }

        VK.API.UpdateDescriptorSets(_device, 4U, sets, 0, ReadOnlySpan<vk.CopyDescriptorSet>.Empty);

        Silk.NET.Vulkan.Viewport viewport = new(width: _extent.X, height: _extent.Y);

        vk.Rect2D scissor = new(extent: new vk.Extent2D(_extent.X, _extent.Y));
        
        VK.API.CmdBindPipeline(cmd, vk.PipelineBindPoint.Graphics, _pipeline);
        
        VK.API.CmdBindDescriptorSets(cmd, vk.PipelineBindPoint.Graphics,
            _layout, 
            firstSet: 0U, 
            1U, 
            in set,
            0, 
            null);
        VK.API.CmdSetViewport(cmd, 0U, 1U, in viewport);
        VK.API.CmdSetScissor(cmd, 0U, 1U, in scissor);
        
        VK.API.CmdDraw(cmd, 3U, 1U, 0U, 0U);
    }

    public void Dispose()
    {
        _layout.Destroy(_device);
        
        _descriptorPool.Destroy(_device);
        
        VK.API.DestroyPipeline(_device, _pipeline, null);

        _inTextureDescriptor.Destroy(_device);
        
        GC.SuppressFinalize(this);
    }

    ~GBufferDrawer() => Dispose();
}