using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable]
public sealed class OutOfHostMemoryException : VulkanException
{
    public OutOfHostMemoryException() { }
    public OutOfHostMemoryException(string message) : base(message) { }
    public OutOfHostMemoryException(string message, int code) : base(message, code) { }
    public OutOfHostMemoryException(string message, Exception inner) : base(message, inner) { }
    private OutOfHostMemoryException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}