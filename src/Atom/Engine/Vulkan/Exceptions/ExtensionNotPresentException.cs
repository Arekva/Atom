using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable] 
public sealed class ExtensionNotPresentException : VulkanException
{
    public ExtensionNotPresentException() { }
    public ExtensionNotPresentException(string message) : base(message) { }
    public ExtensionNotPresentException(string message, int code) : base(message, code) { }
    public ExtensionNotPresentException(string message, Exception inner) : base(message, inner) { }
    private ExtensionNotPresentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}