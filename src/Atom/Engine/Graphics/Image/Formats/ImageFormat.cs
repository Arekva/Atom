// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;

namespace Atom.Engine;

public enum ImageFormat : uint
{
    // Special
    Undefined                   = vk.Format.Undefined               ,
    None                        = Undefined                         ,
    
    
    // R/G/B/A
    // -- RGB
    // ---- 5x6x5
    R5G6B5_UNorm_Pack16         = vk.Format.R5G6B5UnormPack16       ,
    // -- BGR
    // ---- 5x6x5
    B5G6R5_UNorm_Pack16         = vk.Format.B5G6R5UnormPack16       ,
    // ---- 10x11x11
    B10G11R11_UFloat_Pack32     = vk.Format.B10G11R11UfloatPack32   ,
    // -- BGRA
    // ---- 5x5x5x1
    B5G5R5A1_UNorm_Pack16       = vk.Format.B5G5R5A1UnormPack16     ,
    // -- ARGB
    // ---- 2x10x10x10
    A2R10G10B10_UNORM_PACK32    = vk.Format.A2R10G10B10UnormPack32  ,
    // ---- 1x5x5x5
    A1R5G5B5_UNorm_Pack16       = vk.Format.A1R5G5B5UnormPack16     ,
    // -- ABGR
    A2B10G10R10_UNorm_Pack32    = vk.Format.A2B10G10R10UnormPack32  ,
    
    // -- RGBA
    // ---- 5x5x5x1
    R5G5B5A1_UNorm_Pack16       = vk.Format.R5G5B5A1UnormPack16     ,
    // ---- 4x4x4x4
    R4G4B4A4_UNorm_Pack16       = vk.Format.R4G4B4A4UnormPack16     ,
    // ---- 16x16x16x16
    R16G16B16A16_UNorm          = vk.Format.R16G16B16A16Unorm       , 
    R16G16B16A16_SNorm          = vk.Format.R16G16B16A16SNorm       , 
    R16G16B16A16_SFloat         = vk.Format.R16G16B16A16Sfloat      , 
    R16G16B16A16_SInt           = vk.Format.R16G16B16A16Sint        ,
    R16G16B16A16_UInt           = vk.Format.R16G16B16A16Uint        ,
    R16G16B16A16_SScaled        = vk.Format.R16G16B16A16Sscaled     ,
    R16G16B16A16_UScaled        = vk.Format.R16G16B16A16Uscaled     ,
    // ---- 8x8x8x8
    R8G8B8A8_UInt               = vk.Format.R8G8B8A8Uint            ,
    R8G8B8A8_SInt               = vk.Format.R8G8B8A8Sint            ,
    R8G8B8A8_UNorm              = vk.Format.R8G8B8A8Unorm           ,
    R8G8B8A8_sRGB               = vk.Format.R8G8B8A8Srgb            ,
    // ---- 32x32x32x32
    R32G32B32A32_SFloat         = vk.Format.R32G32B32A32Sfloat      ,
    R32G32B32A32_UInt           = vk.Format.R32G32B32A32Uint        ,
    R32G32B32A32_SInt           = vk.Format.R32G32B32A32Sint        ,

    // -- BGRA
    // ---- 8x8x8x8
    B8G8R8A8_UNorm              = vk.Format.B8G8R8A8Unorm           , 
    B8G8R8A8_SNorm              = vk.Format.B8G8R8A8SNorm           , 
    B8G8R8A8_UScaled            = vk.Format.B8G8R8A8Uscaled         , 
    B8G8R8A8_SScaled            = vk.Format.B8G8R8A8Sscaled         , 
    B8G8R8A8_UInt               = vk.Format.B8G8R8A8Uint            , 
    B8G8R8A8_SInt               = vk.Format.B8G8R8A8Sint            , 
    B8G8R8A8_sRGB               = vk.Format.B8G8R8A8Srgb            ,
    // ---- 4x4x4x4
    B4G4R4A4_UNorm_Pack16       = vk.Format.B4G4R4A4UnormPack16     ,
    
    
    // -- ABGR
    // ---- 8x8x8x8
    A8B8G8R8_UNorm_Pack32       = vk.Format.A8B8G8R8UnormPack32     ,
    A8B8G8R8_SNorm_Pack32       = vk.Format.A8B8G8R8SNormPack32     ,
    A8B8G8R8_sRGB_Pack32        = vk.Format.A8B8G8R8SrgbPack32      ,

    
    
    // BCn
    // -- BC1
    BC1_RGB_UNorm_Block         = vk.Format.BC1RgbUnormBlock        ,
    BC1_RGB_sRGB_Block          = vk.Format.BC1RgbSrgbBlock         ,
    BC1_RGBA_UNorm_Block        = vk.Format.BC1RgbaUnormBlock       ,
    BC1_RGBA_sRGB_Block         = vk.Format.BC1RgbaSrgbBlock        ,
    // -- BC2
    BC2_UNorm_Block             = vk.Format.BC2UnormBlock           ,
    BC2_sRGB_Block              = vk.Format.BC2SrgbBlock            ,
    // -- BC3
    BC3_UNorm_Block             = vk.Format.BC3UnormBlock           ,
    BC3_sRGB_Block              = vk.Format.BC3SrgbBlock            ,
    // -- BC4
    BC4_UNorm_Block             = vk.Format.BC4UnormBlock           ,
    BC4_SNorm_Block             = vk.Format.BC4SNormBlock           ,
    // -- BC5
    BC5_UNorm_Block             = vk.Format.BC5UnormBlock           ,
    BC5_SNorm_Block             = vk.Format.BC5SNormBlock           ,
    // -- BC6H
    BC6H_UFloat_Block           = vk.Format.BC6HUfloatBlock         ,
    BC6H_SFloat_Block           = vk.Format.BC6HSfloatBlock         ,
    // -- BC7
    BC7_UNorm_Block             = vk.Format.BC7UnormBlock           ,
    BC7_sRGB_Block              = vk.Format.BC7SrgbBlock            ,
    
    
    
    // Depth (D) + Stencil (S)
    // -- Stencil-less 
    D16_UNorm                   = vk.Format.D16Unorm                ,
    D32_SFloat                  = vk.Format.D32Sfloat               ,
    // -- 8bit Stencil
    D16_UNorm_S8_UInt           = vk.Format.D16UnormS8Uint          ,
    D24_UNorm_S8_UInt           = vk.Format.D24UnormS8Uint          ,
    D32_SFloat_S8_UInt          = vk.Format.D32SfloatS8Uint         ,
}

public static class ImageFormatConversion
{
    public static vk.Format ToVk(this ImageFormat imageFormat) =>
        Unsafe.As<ImageFormat, vk.Format>(ref imageFormat);

    public static ImageFormat ToAtom(this vk.Format vkFormat) =>
        Unsafe.As<vk.Format, ImageFormat>(ref vkFormat);
}