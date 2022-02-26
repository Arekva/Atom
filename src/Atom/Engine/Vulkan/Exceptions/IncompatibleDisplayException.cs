using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable] 
public sealed class IncompatibleDisplayException : VulkanException
{
    public IncompatibleDisplayException() { }
    public IncompatibleDisplayException(string message) : base(message) { }
    public IncompatibleDisplayException(string message, int code) : base(message, code) { }
    public IncompatibleDisplayException(string message, Exception inner) : base(message, inner) { }
    private IncompatibleDisplayException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
