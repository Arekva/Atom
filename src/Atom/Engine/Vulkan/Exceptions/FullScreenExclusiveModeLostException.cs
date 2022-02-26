using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable] 
public sealed class FullScreenExclusiveModeLostException : VulkanException
{
    public FullScreenExclusiveModeLostException() { }
    public FullScreenExclusiveModeLostException(string message) : base(message) { }
    public FullScreenExclusiveModeLostException(string message, int code) : base(message, code) { }
    public FullScreenExclusiveModeLostException(string message, Exception inner) : base(message, inner) { }
    private FullScreenExclusiveModeLostException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}