using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable] public sealed class OutOfPoolMemoryException : VulkanException
{
    public OutOfPoolMemoryException() { }
    public OutOfPoolMemoryException(string message) : base(message) { }
    public OutOfPoolMemoryException(string message, int code) : base(message, code) { }
    public OutOfPoolMemoryException(string message, Exception inner) : base(message, inner) { }
    private OutOfPoolMemoryException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
