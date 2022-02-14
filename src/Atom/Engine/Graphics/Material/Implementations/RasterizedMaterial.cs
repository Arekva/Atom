using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Atom.Engine.Global;
using Atom.Engine.Shader;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public class RasterizedMaterial : Material, IRasterizedMaterial
{
    
    public IRasterShader Shader { get; }
    
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
        = new int[] { (int)DynamicState.Viewport, (int)DynamicState.Scissor };

    private static readonly unsafe PipelineDynamicStateCreateInfo DynamicStateInfo = new(
        flags: 0,
        dynamicStateCount: (uint)DynamicStates.Size,
        pDynamicStates: (DynamicState*)DynamicStates.Pointer
    );

#endregion

    public RasterizedMaterial(IRasterShader shader, Device? device = null) : this(shader, default, device) { }
 
    private RasterizedMaterial(IRasterShader shader, Pipeline basePipeline, Device? device = null) : base(device)
    {
        Shader = shader ?? throw new ArgumentNullException(nameof(shader));
        CreatePipeline(shader.Device, basePipeline);
    }
    
    public RasterizedMaterial Clone() => new (Shader, Pipeline);
    
    public override void CmdBindMaterial(SlimCommandBuffer cmd, Vector2D<uint> extent, uint cameraIndex)
    {
        vk.Viewport viewport = new(width: extent.X, height: extent.Y, minDepth: 0.0F, maxDepth: 1.0F);
        vk.Rect2D scissor = new(extent: new Extent2D(extent.X, extent.Y));

        VK.API.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, Pipeline);
        VK.API.CmdSetViewport(cmd, 0U, 1U, in viewport);
        VK.API.CmdSetScissor(cmd, 0U, 1U, in scissor);
        VK.API.CmdPushConstants(cmd, Shader.PipelineLayout, (vk.ShaderStageFlags)ShaderStageFlags.Vertex, 0U, cameraIndex.AsSpan());
    }
    







    private unsafe void CreatePipeline(Device device, Pipeline basePipeline)
    {
        PipelineShaderStageCreateInfo[] stages = Shader.Modules.Select(module => module.StageInfo).ToArray();


        List<VertexInputAttributeDescription> attribs_list = new();
        List<VertexInputBindingDescription> bindings_list = new();
        foreach ((uint _, VertexInput input) in Shader.VertexModule.VertexInputs)
        {
            bindings_list.Add(input.Binding);
            attribs_list.AddRange(input.Attributes.Values.Select(a => a.Description));
        }

        VertexInputAttributeDescription[] attribs = attribs_list.ToArray();
        VertexInputBindingDescription[] bindings = bindings_list.ToArray();

        PipelineDynamicStateCreateInfo dynamic_state = DynamicStateInfo;
        
        fixed (VertexInputAttributeDescription* p_attribs = attribs)
        fixed (VertexInputBindingDescription* p_bindings = bindings)
        {
            PipelineVertexInputStateCreateInfo input_state_info = new(
                flags: 0,
                vertexAttributeDescriptionCount: (uint)attribs.Length,
                pVertexAttributeDescriptions: p_attribs,

                vertexBindingDescriptionCount: (uint)bindings.Length,
                pVertexBindingDescriptions: p_bindings
            );

            fixed (PipelineShaderStageCreateInfo* p_stages = stages)
            {
                GraphicsPipelineCreateInfo info = new(
                    flags:               0,
                    stageCount:          (uint)stages.Length,
                    pStages:             p_stages,
                    pVertexInputState:   &input_state_info,
                    pInputAssemblyState: (PipelineInputAssemblyStateCreateInfo*)Unsafe.AsPointer(ref Topology.State),
                    pTessellationState:  (PipelineTessellationStateCreateInfo*)Unsafe.AsPointer(ref Tessellation.State),
                    pViewportState:      (PipelineViewportStateCreateInfo*)Unsafe.AsPointer(ref Viewport.State),
                    pRasterizationState: (PipelineRasterizationStateCreateInfo*)Unsafe.AsPointer(ref Rasterizer.State),
                    pMultisampleState:   (PipelineMultisampleStateCreateInfo*)Unsafe.AsPointer(ref Multisampling.State),
                    pDepthStencilState:  (PipelineDepthStencilStateCreateInfo*)Unsafe.AsPointer(ref DepthStencil.State),
                    pColorBlendState:    (PipelineColorBlendStateCreateInfo*)Unsafe.AsPointer(ref ColorBlending.State),
                    pDynamicState:       &dynamic_state,
                    layout:              Shader.PipelineLayout,
                    renderPass:          Graphics.MainRenderPass,
                    subpass:             Graphics.MainSubpass,
                    basePipelineHandle:  basePipeline,
                    basePipelineIndex:   0
                );

                Result result = VK.API.CreateGraphicsPipelines(
                   device, default,
                   1U, in info,
                   null, out Pipeline pipeline);

                Pipeline = pipeline;
            }
        }
    }
}