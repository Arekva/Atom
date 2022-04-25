// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;

using vki = Silk.NET.Vulkan.ImageViewType;



namespace Atom.Engine;



[Flags]
public enum ImageViewType : uint
{
    Dim1D       = vki.ImageViewType1D       ,
    Dim2D       = vki.ImageViewType2D       ,
    Dim3D       = vki.ImageViewType3D       ,
    Cube        = vki.Cube                  ,
    Dim1DArray  = vki.ImageViewType1DArray  ,
    Dim2DArray  = vki.ImageViewType2DArray  ,
    CubeArray   = vki.CubeArray             ,
}

public static class ImageViewTypeConversion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static vk.ImageViewType ToVk(this ImageViewType imageViewType) =>
        Unsafe.As<ImageViewType, vk.ImageViewType>(ref imageViewType);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImageViewType ToAtom(this vk.ImageViewType vkimageViewType) =>
        Unsafe.As<vk.ImageViewType, ImageViewType>(ref vkimageViewType);
}