using System.Runtime.CompilerServices;

namespace Atom.Engine;

public struct MemorySegment
{
    public VulkanMemory Memory { get; }

    public ulong Offset { get; }

    public ulong Size { get; }

    public ulong End => Offset + Size;
    

    internal MemorySegment(VulkanMemory memory, ulong offset, ulong size)
    {
        if (offset + size > memory.Size)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(size), 
                message: "Memory segment offset + size (End) index is outside of Memory size"
            );
        }
        
        Memory = memory;
        Offset = offset;
        Size = size;
    }

    
    public u64 Length<T>() => Size / (u64)Unsafe.SizeOf<T>();
    
    public MemoryMap<T> Map<T>() where T : unmanaged => Memory.Map<T>(this);
    public MemoryMap<byte> Map() => Memory.Map(this);

    public void Unmap(IMemoryMap map) => Memory.Unmap(map);

    public static bool operator ==(MemorySegment lhs, MemorySegment rhs)
    {
        return lhs.Memory == rhs.Memory && lhs.Size == rhs.Size && lhs.Offset == rhs.Offset;
    }

    public static bool operator !=(MemorySegment lhs, MemorySegment rhs) => !(lhs == rhs);
}