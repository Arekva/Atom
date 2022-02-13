using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable]
public class LayerNotFoundException : Exception
{
    public LayerNotFoundException() { }

    public LayerNotFoundException(string message) : base(message) { }

    public LayerNotFoundException(string message, Exception inner) : base(message, inner) { }

    protected LayerNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}