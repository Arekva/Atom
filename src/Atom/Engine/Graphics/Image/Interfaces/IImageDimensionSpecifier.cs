namespace Atom.Engine;

public interface IImageDimensionSpecifier : IImageSpecifier { }

public interface IImage1D : IImageDimensionSpecifier { }

public interface IImage1D<T> : IImage1D where T : unmanaged, IImageFormat { }

public interface IImage2D : IImageDimensionSpecifier { }

public interface IImage2D<T> : IImage2D  where T : unmanaged, IImageFormat { }

public interface IImage3D : IImageDimensionSpecifier { }

public interface IImage3D<T> : IImage3D where T : unmanaged, IImageFormat { }