using System.Runtime.Serialization;

namespace Atom.Engine.Vulkan;

[Serializable]
public sealed class SurfaceLostException : VulkanException
{
    public SurfaceLostException() { }
    public SurfaceLostException(string message) : base(message) { }
    public SurfaceLostException(string message, int code) : base(message, code) { }
    public SurfaceLostException(string message, Exception inner) : base(message, inner) { }
    private SurfaceLostException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}