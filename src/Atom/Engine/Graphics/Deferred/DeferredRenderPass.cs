using Atom.Engine.RenderPass;
using Silk.NET.Vulkan;
using DependencyInfo = Atom.Engine.RenderPass.DependencyInfo;

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
    private const Format ALBEDO_LUMINANCE_FORMAT = Format.R32G32B32A32Sfloat;
    private const Format NORMAL_ROUGHNESS_METALNESS_FORMAT = Format.R32G32B32A32Sfloat;
    private const Format POSITION_TRANSLUCENCY_FORMAT = Format.R32G32B32A32Sfloat;
    private static readonly Format[] DEPTH_FORMATS =
    {
        Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint
    };
    
    // Lit
    private const Format LIT_FORMAT = Format.R32G32B32A32Sfloat; 
    
#endregion


    public static Result CreateRenderPass(
        Device device,
        PhysicalDevice physicalDevice, 
        out Silk.NET.Vulkan.RenderPass renderPass, out Format depthFormat)
    {
        depthFormat = VK.FirstSupportedFormat(
            physicalDevice,
            candidates: DEPTH_FORMATS,
            ImageTiling.Optimal,
            features: FormatFeatureFlags.FormatFeatureDepthStencilAttachmentBit
        );


#region Attachments
        
    #region G-Buffer
        Attachment gAlbedoLuminanceAttachment = new(
            format: ALBEDO_LUMINANCE_FORMAT,
            operators: new AttachmentOperator(
                load: AttachmentLoadOp.Clear, 
                store: AttachmentStoreOp.Store),
            layouts: new LayoutTransition(
                initial: ImageLayout.Undefined,
                final: ImageLayout.ColorAttachmentOptimal
            )
        );
        Attachment gNormalRoughnessMetalnessAttachment = new(
            format: NORMAL_ROUGHNESS_METALNESS_FORMAT,
            operators: new AttachmentOperator(
                load: AttachmentLoadOp.Clear, 
                store: AttachmentStoreOp.Store),
            layouts: new LayoutTransition(
                initial: ImageLayout.Undefined,
                final: ImageLayout.ColorAttachmentOptimal
            )
        );
        Attachment gPositionTranslucencyAttachment = new(
            format: POSITION_TRANSLUCENCY_FORMAT,
            operators: new AttachmentOperator(
                load: AttachmentLoadOp.Clear, 
                store: AttachmentStoreOp.Store),
            layouts: new LayoutTransition(
                initial: ImageLayout.Undefined,
                final: ImageLayout.ColorAttachmentOptimal
            )
        );
        Attachment gDepthAttachment = new(
            format: depthFormat,
            operators: new AttachmentOperator(
                load: AttachmentLoadOp.Clear, 
                store: AttachmentStoreOp.Store),
            layouts: new LayoutTransition(
                initial: ImageLayout.Undefined,
                final: ImageLayout.DepthStencilAttachmentOptimal
            )
        );
    #endregion
        
    #region Lit
        Attachment litAttachment = new(
            format: LIT_FORMAT,
            operators: new AttachmentOperator(
                load: AttachmentLoadOp.DontCare, 
                store: AttachmentStoreOp.Store),
            layouts: new LayoutTransition(
                initial: ImageLayout.Undefined,
                final: ImageLayout.ShaderReadOnlyOptimal // read from the fullscreen shader
            )
        );
    #endregion
        
#endregion

#region Subpasses
        Subpass gBufferSubpass = new(
            colors: new[]
            {
                gAlbedoLuminanceAttachment,
                gNormalRoughnessMetalnessAttachment,
                gPositionTranslucencyAttachment
            },
            depthStencils: new[]
            {
                gDepthAttachment
            }
        );
        Subpass litSubpass = new(
            colors: new[]
            {
                litAttachment
            },
            inputs: new[]
            {
                gAlbedoLuminanceAttachment,
                gNormalRoughnessMetalnessAttachment,
                gPositionTranslucencyAttachment,
                gDepthAttachment
            }
        );
#endregion

#region Dependencies
        DependencyInfo externalInfo = new(
            subpass: Subpass.External,
            stageMask: PipelineStageFlags.PipelineStageColorAttachmentOutputBit | 
                       PipelineStageFlags.PipelineStageEarlyFragmentTestsBit,
            accessMask: AccessFlags.AccessNoneKhr
        );
        DependencyInfo gBufferInfo = new(
            subpass: gBufferSubpass,
            stageMask: PipelineStageFlags.PipelineStageColorAttachmentOutputBit | 
                       PipelineStageFlags.PipelineStageEarlyFragmentTestsBit,
            accessMask: AccessFlags.AccessColorAttachmentWriteBit | 
                        AccessFlags.AccessDepthStencilAttachmentWriteBit
        );
        DependencyInfo litInfo = new(
            subpass: litSubpass,
            stageMask: PipelineStageFlags.PipelineStageColorAttachmentOutputBit | 
                       PipelineStageFlags.PipelineStageEarlyFragmentTestsBit,
            accessMask: AccessFlags.AccessColorAttachmentWriteBit
        );
        
        Dependency gBufferDependency = new(
            source: externalInfo, 
            destination: gBufferInfo
        );
        Dependency litDependency = new(
            source: gBufferInfo, 
            destination: litInfo
        );
#endregion

        RenderPassBuilder builder = new RenderPassBuilder(
            subpasses: new []
            {
                gBufferSubpass,
                litSubpass
            },
            dependencies: new []
            {
                gBufferDependency,
                litDependency
            }
        );

        return builder.Build(device, out renderPass, 0);
    }
}