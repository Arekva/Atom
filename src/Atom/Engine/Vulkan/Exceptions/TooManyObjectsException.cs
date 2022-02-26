using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable]
public sealed class TooManyObjectsException : VulkanException
{
    public TooManyObjectsException() { }
    public TooManyObjectsException(string message) : base(message) { }
    public TooManyObjectsException(string message, int code) : base(message, code) { }
    public TooManyObjectsException(string message, Exception inner) : base(message, inner) { }
    private TooManyObjectsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}