using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable] 
public sealed class ValidationFailedException : VulkanException
{
    public ValidationFailedException() { }
    public ValidationFailedException(string message) : base(message) { }
    public ValidationFailedException(string message, int code) : base(message, code) { }
    public ValidationFailedException(string message, Exception inner) : base(message, inner) { }
    private ValidationFailedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}