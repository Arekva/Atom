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
    
    public void Delete()
    {
        Buffer.Destroy(Device);
        Segment.Memory.Delete();
    }
    
    public BufferSubresource Subresource(u64 start, u64 length)
    {
        return new BufferSubresource(Buffer, Segment.Memory.Segment(Segment.Offset + start, length));
    }
}