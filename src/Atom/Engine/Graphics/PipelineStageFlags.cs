// ReSharper disable InconsistentNaming



using System.Runtime.CompilerServices;

using vki = Silk.NET.Vulkan.PipelineStageFlags;



namespace Atom.Engine;



[Flags]
public enum PipelineStageFlags : uint
{
    None                                = vki.PipelineStageNone                                 ,
    TopOfPipe                           = vki.PipelineStageTopOfPipeBit                         ,
    DrawIndirect                        = vki.PipelineStageDrawIndirectBit                      ,
    VertexInput                         = vki.PipelineStageVertexInputBit                       ,
    VertexShader                        = vki.PipelineStageVertexShaderBit                      ,
    TessellationControlShader           = vki.PipelineStageTessellationControlShaderBit         ,
    TessellationEvaluationShader        = vki.PipelineStageTessellationEvaluationShaderBit      ,
    GeometryShader                      = vki.PipelineStageGeometryShaderBit                    ,
    FragmentShader                      = vki.PipelineStageFragmentShaderBit                    ,
    EarlyFragmentTests                  = vki.PipelineStageEarlyFragmentTestsBit                ,
    LateFragmentTests                   = vki.PipelineStageLateFragmentTestsBit                 ,
    ColorAttachmentOutput               = vki.PipelineStageColorAttachmentOutputBit             ,
    ComputeShader                       = vki.PipelineStageComputeShaderBit                     ,
    Transfer                            = vki.PipelineStageTransferBit                          ,
    BottomOfPipe                        = vki.PipelineStageBottomOfPipeBit                      ,
    Host                                = vki.PipelineStageHostBit                              ,
    AllGraphics                         = vki.PipelineStageAllGraphicsBit                       ,
    AllCommands                         = vki.PipelineStageAllCommandsBit                       ,
    TransformFeedback_EXT               = vki.PipelineStageTransformFeedbackBitExt              ,
    ConditionalRendering_EXT            = vki.PipelineStageConditionalRenderingBitExt           ,
    AccelerationStructureBuild_KHR      = vki.PipelineStageAccelerationStructureBuildBitKhr     ,
    RayTracingShader_KHR                = vki.PipelineStageRayTracingShaderBitKhr               ,
    TaskShader_NV                       = vki.PipelineStageTaskShaderBitNV                      ,
    MeshShader_NV                       = vki.PipelineStageMeshShaderBitNV                      ,
    FragmentDensityProcess_EXT          = vki.PipelineStageFragmentDensityProcessBitExt         ,
    FragmentShadingRateAttachment_KHR   = vki.PipelineStageFragmentShadingRateAttachmentBitKhr  ,
    CommandPreprocess_NV                = vki.PipelineStageCommandPreprocessBitNV               ,
}

public static class PipelineStageFlagsConversion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static vk.PipelineStageFlags ToVk(this PipelineStageFlags atom) =>
        Unsafe.As<PipelineStageFlags, vk.PipelineStageFlags>(ref atom);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PipelineStageFlags ToAtom(this vk.PipelineStageFlags vk) =>
        Unsafe.As<vk.PipelineStageFlags, PipelineStageFlags>(ref vk);
}