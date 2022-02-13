using Silk.NET.Maths;
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
    
    private static readonly Pin<Rect2D> _dummyScissor = new Rect2D(default, new Extent2D(1, 1));

    private static readonly Pin<PipelineColorBlendAttachmentState> _colorBlendAttachment = 
        new PipelineColorBlendAttachmentState();

    private struct NDCDynamicStates
    {
        public DynamicState viewport = DynamicState.Viewport, scissor = DynamicState.Scissor;
    }
    private static readonly Pin<NDCDynamicStates> _dynamicStates = new NDCDynamicStates();



    private static readonly Pin<PipelineVertexInputStateCreateInfo> _vertexInput = 
        new PipelineVertexInputStateCreateInfo(StructureType.PipelineVertexInputStateCreateInfo);

    private static readonly Pin<PipelineInputAssemblyStateCreateInfo> _inputAssembly = 
        new PipelineInputAssemblyStateCreateInfo(topology: PrimitiveTopology.TriangleList);
    
    private static readonly Pin<PipelineTessellationStateCreateInfo> _tessellation = 
        new PipelineTessellationStateCreateInfo();

    private static readonly Pin<PipelineViewportStateCreateInfo> _viewport = new 
        PipelineViewportStateCreateInfo(
        viewportCount: 1, pViewports: _dummyViewport,
        scissorCount: 1, pScissors: _dummyScissor
    );

    private static readonly Pin<PipelineRasterizationStateCreateInfo> _rasterization = 
        new PipelineRasterizationStateCreateInfo(
        lineWidth: 1.0F,
        cullMode: CullModeFlags.CullModeBackBit
    );

    private static readonly Pin<PipelineMultisampleStateCreateInfo> _multisample = 
        new PipelineMultisampleStateCreateInfo(rasterizationSamples: SampleCountFlags.SampleCount1Bit);

    private static readonly Pin<PipelineDepthStencilStateCreateInfo> _depthStencil = 
        new PipelineDepthStencilStateCreateInfo();

    private static readonly Pin<PipelineColorBlendStateCreateInfo> _colorBlend = 
        new PipelineColorBlendStateCreateInfo(attachmentCount: 1 /* other properties set in static constructor */);

    private static readonly Pin<PipelineDynamicStateCreateInfo> _dynamicState = 
        new PipelineDynamicStateCreateInfo(
            dynamicStateCount: 2,  pDynamicStates: (Silk.NET.Vulkan.DynamicState*)(NDCDynamicStates*)_dynamicStates
    );

#endregion

    private static readonly Pin<DescriptorPoolSize> _descriptorImageCount = 
        new DescriptorPoolSize(DescriptorType.InputAttachment,  descriptorCount: 4U);


    
    private Device _device;
    private uint _maxImages;
    private Vector2D<uint> _extent;

    // destroyable resources
    private PipelineLayout _layout;
    private WriteDescriptorSet[] _writeDescriptorSets;
    private DescriptorPool _descriptorPool;
    private Pipeline _pipeline;
    private DescriptorSetLayout _inTextureDescriptor;
    
    private DescriptorSet[] _descriptorSets;
    
    static GBufferDrawer() // Setup constant objects
    {
        /* set non compile dependant properties for the PipelineColorBlendStateCreateInfo */
        _colorBlend.Data.BlendConstants[0] = 
        _colorBlend.Data.BlendConstants[1] =
        _colorBlend.Data.BlendConstants[2] = 
        _colorBlend.Data.BlendConstants[3] = 1.0F;

        _colorBlendAttachment.Data.ColorWriteMask = ColorComponentFlags.ColorComponentRBit |
                                                    ColorComponentFlags.ColorComponentGBit |
                                                    ColorComponentFlags.ColorComponentBBit |
                                                    ColorComponentFlags.ColorComponentABit;
        
        _colorBlend.Data.PAttachments = _colorBlendAttachment;
    }

    public unsafe GBufferDrawer(uint maxImages, Device device)
    {
        _device = device;
        _maxImages = maxImages;
        
        // Load Pipeline Stages
        Span<PipelineShaderStageCreateInfo> modules = stackalloc PipelineShaderStageCreateInfo[2];
        
        void make_shader(string path, ShaderStageFlags stage, out PipelineShaderStageCreateInfo info)
        {
            using FileStream stream = File.OpenRead(path);

            // very small shaders, don't need to allocate on heap
            Span<byte> spv = stackalloc byte[4096];
            int spv_size = stream.Read(spv);
            
            fixed (byte* p_spv = spv)
            {
                ShaderModuleCreateInfo vertex_info = new(
                    codeSize: (uint)spv_size,
                    pCode: (uint*)p_spv
                );

                info = new PipelineShaderStageCreateInfo(
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

        DescriptorPoolCreateInfo pool_create_info = new(
            pPoolSizes: _descriptorImageCount,
            poolSizeCount: 1,
            maxSets: maxImages
        );
        VK.API.CreateDescriptorPool(device, in pool_create_info, null, out _descriptorPool);

        DescriptorSetLayoutBinding* set_layout_bindings = stackalloc DescriptorSetLayoutBinding[4];
        for (int i = 0; i < 4; i++)
        {
            set_layout_bindings[i] = new(
                binding: (uint)i,
                descriptorCount: 1,
                descriptorType: DescriptorType.InputAttachment,
                stageFlags: (vk.ShaderStageFlags)ShaderStageFlags.Fragment
            );
        }
        
        DescriptorSetLayoutCreateInfo set_layout_create_info = new(
            bindingCount: 4U,
            pBindings: set_layout_bindings
        );

        VK.API.CreateDescriptorSetLayout(device, in set_layout_create_info, null, out _inTextureDescriptor);
        
        Span<DescriptorSetLayout> descriptor_set_layouts = stackalloc DescriptorSetLayout[(int)maxImages];
        for (int i = 0; i < maxImages; i++)
        {
            descriptor_set_layouts[i] = _inTextureDescriptor;
        }

        _descriptorSets = new DescriptorSet[maxImages];
        fixed (DescriptorSetLayout* p_descriptor_set_layouts = descriptor_set_layouts)
        {
            DescriptorSetAllocateInfo set_allocate_info = new(
                descriptorPool: _descriptorPool,
                descriptorSetCount: maxImages,
                pSetLayouts: p_descriptor_set_layouts
            );
            VK.API.AllocateDescriptorSets(device, &set_allocate_info, _descriptorSets.AsSpan());
        }

        _writeDescriptorSets = new WriteDescriptorSet[maxImages*4];

        for (int i = 0; i < maxImages; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int index = i * 4 + j;
                
                _writeDescriptorSets[index] = new WriteDescriptorSet(
                    dstSet: _descriptorSets[i],
                    dstBinding: (uint)j,
                    dstArrayElement: 0,
                    descriptorType: DescriptorType.InputAttachment,
                    descriptorCount: 1
                
                    // set ImageInfo later, in CmdDrawView() 
                );
            }
        }

        DescriptorSetLayout gbuffer_layout = _inTextureDescriptor;

        // CreatePipelineLayout()
        PipelineLayoutCreateInfo pipeline_layout_create_info = new(
            pSetLayouts: &gbuffer_layout,
            setLayoutCount: 1
        );
        VK.API.CreatePipelineLayout(device, pipeline_layout_create_info, null, out _layout);
        
        CreatePipeline(modules);
        
        for (int i = 0; i < 2; i++)
        {
            VK.API.DestroyShaderModule(device, modules[i].Module, null);
        }
    }

    private void CreatePipeline(ReadOnlySpan<PipelineShaderStageCreateInfo> stages)
    {
        fixed(PipelineShaderStageCreateInfo* p_stages = stages)
        {
            GraphicsPipelineCreateInfo info = new(
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
            
            Pipeline pipeline = default;
            VK.API.CreateGraphicsPipelines(_device, default, 1, &info, null, &pipeline);
            _pipeline = pipeline;
        }
    }
    
    public void Resize(Vector2D<uint> extent) => _extent = extent;

    public void CmdComputeGBuffer(SlimCommandBuffer cmd, uint swapImageIndex, ReadOnlySpan<ImageView> gbuffer)
    {
        DescriptorSet set = _descriptorSets[swapImageIndex];
        
        Span<WriteDescriptorSet> sets = _writeDescriptorSets.AsSpan()[((int)swapImageIndex * 4)..((int)swapImageIndex * 4 + 4)];
        for (int i = 0; i < 4; i++)
        {
            
            ref WriteDescriptorSet img_set = ref sets[i];
            DescriptorImageInfo descriptor_info = new(
                imageView: gbuffer[i],
                imageLayout: ImageLayout.ShaderReadOnlyOptimal
            );
            img_set.PImageInfo = &descriptor_info;
        }

        VK.API.UpdateDescriptorSets(_device, 4U, sets, 0, ReadOnlySpan<CopyDescriptorSet>.Empty);

        Silk.NET.Vulkan.Viewport viewport = new(width: _extent.X, height: _extent.Y);

        Rect2D scissor = new(extent: new Extent2D(_extent.X, _extent.Y));
        
        VK.API.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, _pipeline);
        
        VK.API.CmdBindDescriptorSets(cmd, PipelineBindPoint.Graphics,
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
        VK.API.DestroyPipelineLayout(_device, _layout, null);
        
        VK.API.DestroyDescriptorPool(_device, _descriptorPool, null);
        
        VK.API.DestroyPipeline(_device, _pipeline, null);
        
        VK.API.DestroyDescriptorSetLayout(_device, _inTextureDescriptor, null);
        
        GC.SuppressFinalize(this);
    }

    ~GBufferDrawer() => Dispose();
}