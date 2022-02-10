using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable] public class ObjectDeletedException : Exception
{
    public ObjectDeletedException() {}
    public ObjectDeletedException(string message) : base(message) { }
    public ObjectDeletedException(string message, Exception inner) : base(message, inner) { }
    protected ObjectDeletedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}