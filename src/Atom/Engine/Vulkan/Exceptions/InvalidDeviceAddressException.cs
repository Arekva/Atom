using System.Runtime.Serialization;

namespace Atom.Engine;
[Serializable] 
public sealed class InvalidDeviceAddressException : VulkanException
{
    public InvalidDeviceAddressException() { }
    public InvalidDeviceAddressException(string message) : base(message) { }
    public InvalidDeviceAddressException(string message, int code) : base(message, code) { }
    public InvalidDeviceAddressException(string message, Exception inner) : base(message, inner) { }
    private InvalidDeviceAddressException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}