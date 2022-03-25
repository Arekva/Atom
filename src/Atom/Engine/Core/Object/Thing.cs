namespace Atom.Engine;

/// <summary> A thing is the base object class for anything that lives in space. </summary>
public abstract class Thing : AtomObject
{
    private Location _location;
    
    private List<Space> _spaces;
    
    
    public ref Location Location => ref _location;

    public IEnumerable<Space> Spaces => _spaces;
    

    public Thing(Location location = default, string? name = "Thing") : base(name)
    {
        _location = location;
        _spaces = new List<Space>();
    }
    

    internal void AddSpace(Space space) => _spaces.Add(space);
    
    internal void RemoveSpace(Space space) => _spaces.Remove(space);
}