using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable] public sealed class TimeoutException : VulkanException
{
    public TimeoutException() { }
    public TimeoutException(string message) : base(message) { }
    public TimeoutException(string message, int code) : base(message, code) { }
    public TimeoutException(string message, Exception inner) : base(message, inner) { }
    private TimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}