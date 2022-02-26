using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable]
public class MemoryMapException : Exception
{
    public MemoryMapException() { }

    public MemoryMapException(string message) : base(message) { }

    public MemoryMapException(string message, Exception inner) : base(message, inner) { }

    protected MemoryMapException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}