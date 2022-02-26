using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable] 
public sealed class LayerNotPresentException : VulkanException
{
    public LayerNotPresentException() { }
    public LayerNotPresentException(string message) : base(message) { }
    public LayerNotPresentException(string message, int code) : base(message, code) { }
    public LayerNotPresentException(string message, Exception inner) : base(message, inner) { }
    private LayerNotPresentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}