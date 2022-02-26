using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable] 
public sealed class OutOfDeviceMemoryException : VulkanException
{
    public OutOfDeviceMemoryException() { }
    public OutOfDeviceMemoryException(string message) : base(message) { }
    public OutOfDeviceMemoryException(string message, int code) : base(message, code) { }
    public OutOfDeviceMemoryException(string message, Exception inner) : base(message, inner) { }
    private OutOfDeviceMemoryException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}