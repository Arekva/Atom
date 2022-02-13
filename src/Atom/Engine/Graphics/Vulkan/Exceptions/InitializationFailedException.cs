using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable]
public sealed class InitializationFailedException : VulkanException
{
    public InitializationFailedException() { }
    public InitializationFailedException(string message) : base(message) { }
    public InitializationFailedException(string message, int code) : base(message, code) { }
    public InitializationFailedException(string message, Exception inner) : base(message, inner) { }
    private InitializationFailedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
