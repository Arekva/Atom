using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable] 
public sealed class UnexpectedResultException : VulkanException
{
    public UnexpectedResultException() { }
    public UnexpectedResultException(string message) : base(message) { }
    public UnexpectedResultException(string message, int code) : base(message, code) { }
    public UnexpectedResultException(string message, Exception inner) : base(message, inner) { }
    private UnexpectedResultException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}