using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable]
public sealed class FormatNotSupportedException : VulkanException
{
    public FormatNotSupportedException() { }
    public FormatNotSupportedException(string message) : base(message) { }
    public FormatNotSupportedException(string message, int code) : base(message, code) { }
    public FormatNotSupportedException(string message, Exception inner) : base(message, inner) { }
    private FormatNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}