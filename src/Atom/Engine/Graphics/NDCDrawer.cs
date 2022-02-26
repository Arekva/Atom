using Atom.Engine.Global;
using Atom.Engine.RenderPass;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using DependencyInfo = Atom.Engine.RenderPass.DependencyInfo;

using Atom.Engine.Vulkan;

namespace Atom.Engine;

// ReSharper disable once InconsistentNaming
public unsafe class NDCDrawer : IDisposable
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
        
        public NDCDynamicStates() { }
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

    private static readonly Pin<DescriptorPoolSize> _combinedImageCount = 
        new DescriptorPoolSize(DescriptorType.CombinedImageSampler,  descriptorCount: 1U);


    
    private Device _device;
    private uint _imageCount;
    private Vector2D<uint> _extent;
    private uint _maxImages;

    // destroyable resources
    private SlimPipelineLayout _layout;
    private WriteDescriptorSet[] _writeDescriptorSets;
    private SlimDescriptorPool _descriptorPool;
    private Sampler _textureSampler;
    private Pipeline _pipeline;
    private Silk.NET.Vulkan.RenderPass _renderPass;
    private SlimDescriptorSetLayout _inTextureDescriptor;
    private SlimFramebuffer[] _framebuffers;
    
    private DescriptorSet[] _descriptorSets;
    
    static NDCDrawer() // Setup constant objects
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

    public unsafe NDCDrawer(uint maxImages, Device device)
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
        
        make_shader("Assets/Shaders/Engine/NDC/Modules/ndc.frag.spv", 
            ShaderStageFlags.Fragment, out modules[0]); /* Fragment module */
        make_shader("Assets/Shaders/Engine/NDC/Modules/ndc.vert.spv", 
            ShaderStageFlags.Vertex, out modules[1]);   /* Vertex   module */

        _descriptorPool = new SlimDescriptorPool(_device, maxImages, new (_combinedImageCount, 1));

        DescriptorSetLayoutBinding set_layout_binding = new(
            binding: 0,
            descriptorCount: 1,
            descriptorType: DescriptorType.CombinedImageSampler,
            stageFlags: (vk.ShaderStageFlags)ShaderStageFlags.Fragment
        );

        SlimDescriptorSetLayout inTextureDescriptor = _inTextureDescriptor = new SlimDescriptorSetLayout(device, new (&set_layout_binding, 1));

        Span<SlimDescriptorSetLayout> descriptor_set_layouts = stackalloc SlimDescriptorSetLayout[(int)maxImages];
        for (int i = 0; i < maxImages; i++)
        {
            descriptor_set_layouts[i] = inTextureDescriptor;
        }

        _descriptorSets = new DescriptorSet[maxImages];
        fixed (SlimDescriptorSetLayout* p_descriptor_set_layouts = descriptor_set_layouts)
        {
            DescriptorSetAllocateInfo set_allocate_info = new(
                descriptorPool: _descriptorPool,
                descriptorSetCount: maxImages,
                pSetLayouts: (vk.DescriptorSetLayout*)p_descriptor_set_layouts
            );
            VK.API.AllocateDescriptorSets(device, &set_allocate_info, _descriptorSets.AsSpan());
        }

        _framebuffers = new SlimFramebuffer[maxImages];
        _writeDescriptorSets = new WriteDescriptorSet[maxImages];

        for (int i = 0; i < maxImages; i++)
        {
            _writeDescriptorSets[i] = new WriteDescriptorSet(
                dstSet: _descriptorSets[i],
                dstBinding: 0,
                dstArrayElement: 0,
                descriptorType: DescriptorType.CombinedImageSampler,
                descriptorCount: 1
                
                // set ImageInfo later, in CmdDrawView() 
            );
        }

        
        
        _layout = new SlimPipelineLayout(device, 
            new (&inTextureDescriptor, 1), 
            ReadOnlySpan<PushConstantRange>.Empty);

        SamplerCreateInfo sampler_create_info = new(
            anisotropyEnable: false, maxAnisotropy: 16.0F,
            magFilter: Filter.Linear, minFilter: Filter.Linear,
            addressModeU: SamplerAddressMode.Repeat, addressModeV: SamplerAddressMode.Repeat,
            addressModeW: SamplerAddressMode.Repeat,
            mipmapMode: SamplerMipmapMode.Linear, mipLodBias: 0.0F, minLod: 0.0F, maxLod: 0.0F,
            compareEnable: false, compareOp: CompareOp.Always,
            borderColor: BorderColor.IntOpaqueBlack, unnormalizedCoordinates: false, flags: 0
        );
        VK.API.CreateSampler(device, in sampler_create_info, null, out _textureSampler);

        CreateRenderPass();

        CreatePipeline(modules);
        
        for (int i = 0; i < 2; i++)
        {
            VK.API.DestroyShaderModule(device, modules[i].Module, null);
        }
    }

    private void CreateRenderPass()
    {
        RenderPassBuilder builder = new RenderPassBuilder();
        Subpass subpass = new Subpass(bindPoint: PipelineBindPoint.Graphics);
        builder.LinkSubpass(
            subpass: subpass,
            order: 0U
        );

        Attachment attachment = new Attachment(
            format: DeferredRenderer.DEFAULT_COLOR_FORMAT /*Format.B8G8R8A8Srgb*/,
            operators: new AttachmentOperator(
                load: AttachmentLoadOp.DontCare,
                store: AttachmentStoreOp.Store
            ),
            layouts: new LayoutTransition(
                initial: ImageLayout.Undefined,
                final: ImageLayout.PresentSrcKhr
            )
        );
        subpass.AssignColor(
            attachment: attachment,
            layout: ImageLayout.ColorAttachmentOptimal
        );
        Dependency dependency = new Dependency(
            source: new DependencyInfo(Subpass.External, 
                PipelineStageFlags.PipelineStageColorAttachmentOutputBit | 
                PipelineStageFlags.PipelineStageEarlyFragmentTestsBit, 0),
            destination: new DependencyInfo(subpass,
                PipelineStageFlags.PipelineStageColorAttachmentOutputBit |
                PipelineStageFlags.PipelineStageEarlyFragmentTestsBit,
                AccessFlags.AccessColorAttachmentWriteBit)
        );
        builder.LinkDependency(dependency);

        builder.Build(_device, out _renderPass, 0);
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
                renderPass: _renderPass,
                subpass: 0U,
                basePipelineHandle: null,
                basePipelineIndex: 0
            );
            
            Pipeline pipeline = default;
            VK.API.CreateGraphicsPipelines(_device, default, 1, &info, null, &pipeline);
            _pipeline = pipeline;
        }
    }
    
    public void Resize(Vector2D<uint> extent, ReadOnlySpan<SlimImageView> swapchainViews)
    {
        _extent = extent;

        uint old_images_count = _imageCount;
        uint new_images_count = _imageCount = (uint)swapchainViews.Length;
        
        for (uint i = 0; i < old_images_count; i++)
        {
            VK.API.DestroyFramebuffer(_device, _framebuffers[i], null);
        }
        
        for (int i = 0; i < new_images_count; i++)
        {
            SlimImageView view = swapchainViews[i];
            _framebuffers[i] = new SlimFramebuffer(_device,
                renderPass: _renderPass,
                attachments: view.AsSpan(),
                width: extent.X,
                height: extent.Y,
                layers: 1
            );
        }

        fixed (SlimImageView* p_views = swapchainViews)
        {
            
        }
    }

    public void CmdDrawView(SlimCommandBuffer cmd, uint swapImageIndex, SlimImageView source)
    {
        DescriptorSet set = _descriptorSets[swapImageIndex];
        
        WriteDescriptorSet img_set = _writeDescriptorSets[swapImageIndex];
        DescriptorImageInfo descriptor_info = new(
            sampler: _textureSampler,
            imageView: source,
            imageLayout: ImageLayout.ShaderReadOnlyOptimal
        );
        img_set.PImageInfo = &descriptor_info;
        
        VK.API.UpdateDescriptorSets(_device, 1U, in img_set, 0, null);

        Silk.NET.Vulkan.Viewport viewport = new(width: _extent.X, height: _extent.Y);

        Rect2D scissor = new(extent: new Extent2D(_extent.X, _extent.Y));

        RenderPassBeginInfo begin_info = new(
            renderPass: _renderPass, 
            framebuffer: _framebuffers[swapImageIndex], 
            renderArea: scissor
        );
        
        VK.API.CmdBeginRenderPass(cmd, in begin_info, SubpassContents.Inline);
        {
            VK.API.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, _pipeline);
            VK.API.CmdBindDescriptorSets(cmd, PipelineBindPoint.Graphics, 
                _layout, firstSet: 0U, 1U, in set, 0, null);
            VK.API.CmdSetViewport(cmd, 0U, 1U, in viewport);
            VK.API.CmdSetScissor(cmd, 0U, 1U, in scissor);
            VK.API.CmdDraw(cmd, 3U, 1U, 0U, 0U);
        }
        VK.API.CmdEndRenderPass(cmd);
    }

    public void Dispose()
    {
        _layout.Destroy(_device);
        
        _descriptorPool.Destroy(_device);
        
        for (int i = 0; i < _imageCount; i++)
        {
            VK.API.DestroyFramebuffer(_device, _framebuffers[i], null);
        }
        
        VK.API.DestroySampler(_device, _textureSampler, null);
        
        VK.API.DestroyPipeline(_device, _pipeline, null);
        
        VK.API.DestroyRenderPass(_device, _renderPass, null);
        
        _inTextureDescriptor.Destroy(_device);
        
        GC.SuppressFinalize(this);
    }

    ~NDCDrawer() => Dispose();
}