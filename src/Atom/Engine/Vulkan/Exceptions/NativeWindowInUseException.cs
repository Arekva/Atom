using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable]
public sealed class NativeWindowInUseException : VulkanException
{
    public NativeWindowInUseException() { }
    public NativeWindowInUseException(string message) : base(message) { }
    public NativeWindowInUseException(string message, int code) : base(message, code) { }
    public NativeWindowInUseException(string message, Exception inner) : base(message, inner) { }
    private NativeWindowInUseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}