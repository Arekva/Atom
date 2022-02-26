namespace Atom.Engine;

public interface IImageLocationSpecifier : IImageSpecifier { }

public interface IImageHost : IImageLocationSpecifier { }

public interface IImageDevice : IImageLocationSpecifier { }