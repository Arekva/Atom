using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable] 
public sealed class IncompatibleDriverException : VulkanException
{
    public IncompatibleDriverException() { }
    public IncompatibleDriverException(string message) : base(message) { }
    public IncompatibleDriverException(string message, int code) : base(message, code) { }
    public IncompatibleDriverException(string message, Exception inner) : base(message, inner) { }
    private IncompatibleDriverException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}