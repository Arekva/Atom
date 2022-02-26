using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable] 
public sealed class InvalidExternalHandleException : VulkanException
{
    public InvalidExternalHandleException() { }
    public InvalidExternalHandleException(string message) : base(message) { }
    public InvalidExternalHandleException(string message, int code) : base(message, code) { }
    public InvalidExternalHandleException(string message, Exception inner) : base(message, inner) { }
    private InvalidExternalHandleException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}