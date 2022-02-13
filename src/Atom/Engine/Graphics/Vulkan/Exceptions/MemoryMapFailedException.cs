using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable]
public sealed class MemoryMapFailedException : VulkanException
{
    public MemoryMapFailedException() { }
    public MemoryMapFailedException(string message) : base(message) { }
    public MemoryMapFailedException(string message, int code) : base(message, code) { }
    public MemoryMapFailedException(string message, Exception inner) : base(message, inner) { }
    private MemoryMapFailedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}