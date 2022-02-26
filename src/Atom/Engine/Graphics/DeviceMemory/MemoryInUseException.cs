using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable]
public class MemoryInUseException : Exception
{
    public MemoryInUseException() { }

    public MemoryInUseException(string message) : base(message) { }

    public MemoryInUseException(string message, Exception inner) : base(message, inner) { }

    protected MemoryInUseException(
        SerializationInfo info,
        StreamingContext context) : base(info, context) { }
}