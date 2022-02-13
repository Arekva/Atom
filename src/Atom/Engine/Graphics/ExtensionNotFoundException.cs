using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable]
public class ExtensionNotFoundException : Exception
{
    public ExtensionNotFoundException() { }
    
    public ExtensionNotFoundException(string message) : base(message) { }

    public ExtensionNotFoundException(string message, Exception inner) : base(message, inner) { }

    protected ExtensionNotFoundException(
        SerializationInfo info,
        StreamingContext context) : base(info, context) { }
}