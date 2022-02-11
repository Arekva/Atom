namespace Atom.Engine;

public struct MemorySegment
{
    public DeviceMemory Memory { get; }

    public ulong Offset { get; }

    public ulong Size { get; }

    public ulong End => Offset + Size;
    

    internal MemorySegment(DeviceMemory memory, ulong offset, ulong size)
    {
        if (offset + size > memory.Size)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(size), 
                message: "Memory segment offset + size (To) index is outside of Memory size"
            );
        }
        
        Memory = memory;
        Offset = offset;
        Size = size;
    }
    
    
    public MemoryMap<T> Map<T>() where T : unmanaged => Memory.Map<T>(this);
    public MemoryMap<byte> Map() => Memory.Map(this);

    public void Unmap(IMemoryMap map) => Memory.Unmap(map);
}