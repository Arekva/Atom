namespace Atom.Engine;

public interface ISky : IDrawer, IDisposable { }

public interface ISkySphere : ISky { }
public interface ISkybox : ISky { }