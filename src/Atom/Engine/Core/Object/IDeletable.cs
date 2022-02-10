namespace Atom.Engine;

public interface IDeletable : IDisposable
{
    public bool IsDeleted { get; }

    public void Delete();
}