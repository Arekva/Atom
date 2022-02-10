namespace Atom.Engine;

public interface IEnablable
{
    public bool IsEnabled { get; } 
    public void Enable();
    public void Disable();
}
