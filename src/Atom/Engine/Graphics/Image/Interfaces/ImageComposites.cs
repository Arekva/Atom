namespace Atom.Engine;


public interface IImageOptimalDevice : IImageOptimal, IImageDevice, IImage { }

public interface IImageLinearDevice : IImageLinear, IImageDevice, IImage { }


public interface IImageOptimalDevice1D : IImageOptimalDevice, IImage1D { }

public interface IImageOptimalDevice2D : IImageOptimalDevice, IImage2D { }

public interface IImageOptimalDevice3D : IImageOptimalDevice, IImage3D { }


public interface IImageLinearDevice1D : IImageLinearDevice, IImage1D { }

public interface IImageLinearDevice2D : IImageLinearDevice, IImage2D { }

public interface IImageLinearDevice3D : IImageLinearDevice, IImage3D { }






public interface IImageOptimalHost : IImageOptimal, IImageHost, IImage { }

public interface IImageLinearHost : IImageLinear, IImageHost, IImage { }


public interface IImageOptimalHost1D : IImageOptimalHost, IImage1D { }

public interface IImageOptimalHost2D : IImageOptimalHost, IImage2D { }

public interface IImageOptimalHost3D : IImageOptimalHost, IImage3D { }


public interface IImageLinearHost1D : IImageLinearHost, IImage1D { }

public interface IImageLinearHost2D : IImageLinearHost, IImage2D { }

public interface IImageLinearHost3D : IImageLinearHost, IImage3D { }