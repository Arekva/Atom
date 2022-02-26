// ReSharper disable InconsistentNaming
namespace Atom.Engine;

[Flags] 
public enum ImageAspectFlags
{
    Color               = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectColorBit,
    Depth               = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectDepthBit,
    Stencil             = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectStencilBit,
    Metadata            = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectMetadataBit,
    MemoryPlane0_EXT    = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectMemoryPlane0BitExt,
    MemoryPlane1_EXT    = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectMemoryPlane1BitExt,
    MemoryPlane2_EXT    = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectMemoryPlane2BitExt,
    MemoryPlane3_EXT    = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectMemoryPlane3BitExt,
    None_KHR            = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectNoneKhr,
    Plane0              = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectPlane0Bit,
    Plane1              = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectPlane1Bit,
    Plane2              = Silk.NET.Vulkan.ImageAspectFlags.ImageAspectPlane2Bit,
}

public static class ImageAspectFlagsConvertion
{
    public static vk.ImageAspectFlags ToVk(this ImageAspectFlags @enum) =>
        (vk.ImageAspectFlags)@enum;
    public static ImageAspectFlags ToAtom(this vk.ImageAspectFlags @enum) =>
        (ImageAspectFlags)@enum;
}