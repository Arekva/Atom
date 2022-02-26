using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable]
public sealed class DeviceLostException : VulkanException
{
    public DeviceLostException() { }
    public DeviceLostException(string message) : base(message) { }
    public DeviceLostException(string message, int code) : base(message, code) { }
    public DeviceLostException(string message, Exception inner) : base(message, inner) { }
    private DeviceLostException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}