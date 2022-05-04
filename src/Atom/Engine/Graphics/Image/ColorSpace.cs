// ReSharper disable InconsistentNaming



using vkc = Silk.NET.Vulkan.ColorSpaceKHR;



namespace Atom.Engine;



public enum ColorSpace : uint
{
    sRGB_NonLinear          = vkc.ColorSpaceSrgbNonlinearKhr        ,
    Display_P3_NonLinear    = vkc.ColorSpaceDisplayP3NonlinearExt   ,
    Extended_sRGB_Linear    = vkc.ColorSpaceExtendedSrgbLinearExt   ,
    DCI_P3_Linear           = vkc.ColorSpaceDciP3LinearExt          ,
    DCI_P3_NonLinear        = vkc.ColorSpaceDciP3NonlinearExt       ,
    BT709_Linear            = vkc.ColorSpaceBT709LinearExt          ,
    BT709_NonLinear         = vkc.ColorSpaceBT709NonlinearExt       ,
    BT2020_Linear           = vkc.ColorSpaceBT2020LinearExt         ,
    HDR10_ST2084            = vkc.ColorSpaceHdr10ST2084Ext          ,
    DolbyVision             = vkc.ColorSpaceDolbyvisionExt          ,
    HDR10_HLG               = vkc.ColorSpaceHdr10HlgExt             ,
    AdobeRGB_Linear         = vkc.ColorSpaceAdobergbLinearExt       ,
    AdobeRGB_NonLinear      = vkc.ColorSpaceAdobergbNonlinearExt    ,
    PassThrough             = vkc.ColorSpacePassThroughExt          ,
    Extended_sRGB_NonLinear = vkc.ColorSpaceExtendedSrgbNonlinearExt,
    DisplayNative           = vkc.ColorSpaceDisplayNativeAmd        ,
}

public static class ColorSpaceConversion
{
    public static vk.ColorSpaceKHR ToVk(this ColorSpace atom) =>
        (vk.ColorSpaceKHR)atom;
    public static ColorSpace ToAtom(this vk.ColorSpaceKHR vk) =>
        (ColorSpace)vk;
}