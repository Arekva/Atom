using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable] 
public sealed class InvalidOpaqueCaptureAddressException : VulkanException
{
    public InvalidOpaqueCaptureAddressException() { }
    public InvalidOpaqueCaptureAddressException(string message) : base(message) { }
    public InvalidOpaqueCaptureAddressException(string message, int code) : base(message, code) { }
    public InvalidOpaqueCaptureAddressException(string message, Exception inner) : base(message, inner) { }
    private InvalidOpaqueCaptureAddressException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}