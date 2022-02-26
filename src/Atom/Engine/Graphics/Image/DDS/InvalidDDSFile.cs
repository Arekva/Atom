namespace Atom.Engine.DDS;

public class InvalidDDSFile : Exception
{
    public InvalidDDSFile() : base() { }
    
    public InvalidDDSFile(string message) : base(message) { }
}