// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;

using vki = Silk.NET.Vulkan.ImageUsageFlags;



namespace Atom.Engine;



[Flags]
public enum ImageUsageFlags : uint
{
    TransferSource                      = vki.ImageUsageTransferSrcBit                      ,
    TransferDestination                 = vki.ImageUsageTransferDstBit                      ,
    Sampled                             = vki.ImageUsageSampledBit                          ,
    Storage                             = vki.ImageUsageStorageBit                          ,
    ColorAttachment                     = vki.ImageUsageColorAttachmentBit                  ,
    DepthStencilAttachment              = vki.ImageUsageDepthStencilAttachmentBit           ,
    TransientAttachment                 = vki.ImageUsageTransientAttachmentBit              ,
    InputAttachment                     = vki.ImageUsageInputAttachmentBit                  ,
    FragmentShadingRateAttachment_KHR   = vki.ImageUsageFragmentShadingRateAttachmentBitKhr ,
    FragmentDensityMap_EXT              = vki.ImageUsageFragmentDensityMapBitExt            ,
    VideoDecodeDestination_KHR          = vki.ImageUsageVideoDecodeDstBitKhr                ,
    VideoDecodeSource_KHR               = vki.ImageUsageVideoDecodeSrcBitKhr                ,
    VideoDecodeDPB_KHR                  = vki.ImageUsageVideoDecodeDpbBitKhr                ,
    VideoEncodeDestination_KHR          = vki.ImageUsageVideoEncodeDstBitKhr                ,
    VideoEncodeSource_KHR               = vki.ImageUsageVideoEncodeSrcBitKhr                ,
    VideoEncodeDPB_KHR                  = vki.ImageUsageVideoEncodeDpbBitKhr                ,
    Reserved16_QCom                     = vki.ImageUsageReserved16BitQCom                   ,
    Reserved17_QCom                     = vki.ImageUsageReserved17BitQCom                   ,
    InvocationMask_Huawei               = vki.ImageUsageInvocationMaskBitHuawei             ,
    Reserved19_EXT                      = vki.ImageUsageReserved19BitExt                    ,
    Reserved20_QCom                     = vki.ImageUsageReserved20BitQCom                   ,
    Reserved21_QCom                     = vki.ImageUsageReserved21BitQCom                   ,
    Reserved22_EXT                      = vki.ImageUsageReserved22BitExt                    ,
}

public static class ImageUsageFlagsConversion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static vk.ImageUsageFlags ToVk(this ImageUsageFlags imageUsageFlags) =>
        Unsafe.As<ImageUsageFlags, vk.ImageUsageFlags>(ref imageUsageFlags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImageUsageFlags ToAtom(this vk.ImageUsageFlags vkImageUsageFlags) =>
        Unsafe.As<vk.ImageUsageFlags, ImageUsageFlags>(ref vkImageUsageFlags);
}