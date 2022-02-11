using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable] 
public sealed class FragmentationException : VulkanException
{
    public FragmentationException() { }
    public FragmentationException(string message) : base(message) { }
    public FragmentationException(string message, int code) : base(message, code) { }
    public FragmentationException(string message, Exception inner) : base(message, inner) { }
    private FragmentationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
