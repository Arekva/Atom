using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable] 
public sealed class OutOfDateException : VulkanException
{
    public OutOfDateException() { }
    public OutOfDateException(string message) : base(message) { }
    public OutOfDateException(string message, int code) : base(message, code) { }
    public OutOfDateException(string message, Exception inner) : base(message, inner) { }
    private OutOfDateException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
