using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable]
public sealed class UnknownException : VulkanException
{
    public UnknownException() { }
    public UnknownException(string message) : base(message) { }
    public UnknownException(string message, int code) : base(message, code) { }
    public UnknownException(string message, Exception inner) : base(message, inner) { }
    private UnknownException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
