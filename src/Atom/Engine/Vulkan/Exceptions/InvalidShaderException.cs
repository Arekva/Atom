using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable] 
public sealed class InvalidShaderException : VulkanException
{
    public InvalidShaderException() { }
    public InvalidShaderException(string message) : base(message) { }
    public InvalidShaderException(string message, int code) : base(message, code) { }
    public InvalidShaderException(string message, Exception inner) : base(message, inner) { }
    private InvalidShaderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}