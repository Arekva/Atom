using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable]
public sealed class NotPermittedException : VulkanException
{
    public NotPermittedException() { }
    public NotPermittedException(string message) : base(message) { }
    public NotPermittedException(string message, int code) : base(message, code) { }
    public NotPermittedException(string message, Exception inner) : base(message, inner) { }
    private NotPermittedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}