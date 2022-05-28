using Atom.Engine.RenderPass;

namespace Atom.Engine;

/// <summary>
/// Deferred rendering capable render pass. Everything is automatically configured.
/// </summary>
/// <p>
/// <b>Buffers configuration</b>
/// There are two passes for now: the G-Buffer draw and the light calculations
/// </p>
/// <p>
/// <b>G-Buffer</b>
/// The G-Buffer is composed of two images:
/// * [0] => R32G32B32A32_SFLOAT with 3 @layers:
/// 	* @0: RGB(Albedo)A(Luminance)
/// 	* @1: RG(Normal)B(Roughness + Z Normal sign [Z value is determined by
/// 		  cross(normal.x, normal.y), and the cross result sign is overwritten by the Roughness
/// 		  sign; therefore the roughness always should be retrieved as abs()])A(Metalness)
/// 	* @2: RGB(Position)A(Translucency)
/// * [1] => Depth format [preferably D32_SFLOAT if available*] with 1 @layers
/// 	* @0: R(Depth), possibly S(Unused)
/// </p>
internal static class DeferredRenderPass
{
    /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
     * 1st pass: draw each object in the world, the UI is drawn  *
     *           after. Store the position, normals, albedo and  *
     *           other material properties in different buffers. *
     *                                                           *
     * 2nd pass: reuse the previously drawn subpass to compute   *
     *           the lighting on a fullscreen triangle/quad.     *
     *           The result is a lit and displayable image.      *
     *                                                           *
     * 3rd pass: draw world + post fx onto the final image.      *
     *                                                           *
     * 4th pass: draw UI onto the final image                    *
     * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
    
#region Configuration
    
    // G-Buffer
    private const ImageFormat ALBEDO_LUMINANCE_FORMAT = ImageFormat.R32G32B32A32_SFloat;
    private const ImageFormat NORMAL_ROUGHNESS_METALNESS_FORMAT = ImageFormat.R32G32B32A32_SFloat;
    private const ImageFormat POSITION_TRANSLUCENCY_FORMAT = ImageFormat.R32G32B32A32_SFloat;

#endregion


    public static vk.Result CreateRenderPass(
        vk.Device device, ImageFormat colorFormat, ImageFormat depthFormat,
        out Silk.NET.Vulkan.RenderPass renderPass)
    {
#region Attachments
        
    #region G-Buffer
        Attachment g_albedo_luminance_attachment = new(
            format: ALBEDO_LUMINANCE_FORMAT,
            operators: new AttachmentOperator(
                load: vk.AttachmentLoadOp.Clear, 
                store: vk.AttachmentStoreOp.Store),
            layouts: new LayoutTransition(
                initial: vk.ImageLayout.Undefined,
                final: vk.ImageLayout.ColorAttachmentOptimal
            )
        );
        Attachment g_normal_roughness_metalness_attachment = new(
            format: NORMAL_ROUGHNESS_METALNESS_FORMAT,
            operators: new AttachmentOperator(
                load: vk.AttachmentLoadOp.Clear, 
                store: vk.AttachmentStoreOp.Store),
            layouts: new LayoutTransition(
                initial: vk.ImageLayout.Undefined,
                final: vk.ImageLayout.ColorAttachmentOptimal
            )
        );
        Attachment g_position_translucency_attachment = new(
            format: POSITION_TRANSLUCENCY_FORMAT,
            operators: new AttachmentOperator(
                load: vk.AttachmentLoadOp.Clear, 
                store: vk.AttachmentStoreOp.Store),
            layouts: new LayoutTransition(
                initial: vk.ImageLayout.Undefined,
                final: vk.ImageLayout.ColorAttachmentOptimal
            )
        );
        Attachment g_depth_attachment = new(
            format: depthFormat,
            operators: new AttachmentOperator(
                load: vk.AttachmentLoadOp.Clear, 
                store: vk.AttachmentStoreOp.Store),
            layouts: new LayoutTransition(
                initial: vk.ImageLayout.Undefined,
                final: vk.ImageLayout.DepthStencilAttachmentOptimal
            )
        );
    #endregion
        
    #region Lit
        Attachment lit_attachment = new(
            format: colorFormat,
            operators: new AttachmentOperator(
                load: vk.AttachmentLoadOp.DontCare, 
                store: vk.AttachmentStoreOp.Store),
            layouts: new LayoutTransition(
                initial: vk.ImageLayout.Undefined,
                final: vk.ImageLayout.TransferSrcOptimal
            )
        );
    #endregion
        
#endregion

#region Subpasses
        Subpass g_buffer_subpass = new(
            colors: new[]
            {
                g_albedo_luminance_attachment,
                g_normal_roughness_metalness_attachment,
                g_position_translucency_attachment
            },
            depthStencils: new[]
            {
                g_depth_attachment
            }
        );
        Subpass lit_subpass = new(
            colors: new[]
            {
                lit_attachment
            },
            inputs: new[]
            {
                g_albedo_luminance_attachment,
                g_normal_roughness_metalness_attachment,
                g_position_translucency_attachment,
                g_depth_attachment
            }
        );
        Subpass append_subpass = new(
            colors: new[]
            {
                lit_attachment
            },
            depthStencils: new[]
            {
                g_depth_attachment
            }
        );
#endregion

#region Dependencies
        DependencyInfo external_info = new(
            subpass   : Subpass.External                         ,
            stageMask : PipelineStageFlags.ColorAttachmentOutput | 
                        PipelineStageFlags.EarlyFragmentTests    ,
            accessMask: vk.AccessFlags.AccessNoneKhr
        );
        DependencyInfo g_buffer_info = new(
            subpass   : g_buffer_subpass                         ,
            stageMask : PipelineStageFlags.ColorAttachmentOutput | 
                        PipelineStageFlags.EarlyFragmentTests    ,
            accessMask: vk.AccessFlags.AccessColorAttachmentWriteBit | 
                        vk.AccessFlags.AccessDepthStencilAttachmentWriteBit
        );
        DependencyInfo lit_info = new(
            subpass   : lit_subpass                              ,
            stageMask : PipelineStageFlags.ColorAttachmentOutput | 
                        PipelineStageFlags.EarlyFragmentTests    ,
            accessMask: vk.AccessFlags.AccessColorAttachmentWriteBit
        );
        DependencyInfo append_info = new(
            subpass   : append_subpass                           ,
            stageMask : PipelineStageFlags.ColorAttachmentOutput | 
                        PipelineStageFlags.EarlyFragmentTests    ,
            accessMask: vk.AccessFlags.AccessColorAttachmentWriteBit
        );
        
        Dependency g_buffer_dependency = new(
            source     : external_info, 
            destination: g_buffer_info
        );
        Dependency lit_dependency = new(
            source     : g_buffer_info, 
            destination: lit_info
        );
        Dependency append_dependency = new(
            source     : lit_info, 
            destination: append_info
        );
#endregion

        RenderPassBuilder builder = new(
            subpasses: new []
            {
                g_buffer_subpass,
                lit_subpass,
                append_subpass
            },
            dependencies: new []
            {
                g_buffer_dependency,
                lit_dependency,
                append_dependency
            }
        );

        return builder.Build(device, out renderPass, 0);
    }
}