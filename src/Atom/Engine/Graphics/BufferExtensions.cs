using System.Runtime.CompilerServices;
using Atom.Engine.Vulkan;


namespace Atom.Engine;

public static class BufferExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static vk.Result BindMemory(this SlimBuffer Handle, MemorySegment memory)
        => Handle.BindMemory(memory.Memory.Device, memory.Memory, memory.Offset);
}