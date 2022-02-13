using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable]
public sealed class NotReadyException : VulkanException
{
    public NotReadyException() { }
    public NotReadyException(string message) : base(message) { }
    public NotReadyException(string message, int code) : base(message, code) { }
    public NotReadyException(string message, Exception inner) : base(message, inner) { }
    private NotReadyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}