namespace Atom.Engine;

public interface IMemoryMap
{
    public MemorySegment Segment { get; }
}