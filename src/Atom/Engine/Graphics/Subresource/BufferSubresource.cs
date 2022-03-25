using Atom.Engine.Vulkan;

namespace Atom.Engine;

public class BufferSubresource : ISubresource
{
    public vk.Device Device => Segment.Memory.Device;
    
    public SlimBuffer Buffer { get; }
    public MemorySegment Segment { get; }

    
    public BufferSubresource(SlimBuffer buffer, MemorySegment segment)
    {
        Buffer = buffer;
        Segment = segment;
    }
}