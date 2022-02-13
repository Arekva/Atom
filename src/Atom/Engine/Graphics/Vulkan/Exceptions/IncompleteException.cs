using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable] 
public sealed class IncompleteException : VulkanException
{
    public IncompleteException() { }
    public IncompleteException(string message) : base(message) { }
    public IncompleteException(string message, int code) : base(message, code) { }
    public IncompleteException(string message, Exception inner) : base(message, inner) { }
    private IncompleteException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}