using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable] 
public sealed class FeatureNotPresentException : VulkanException
{
    public FeatureNotPresentException() { }
    public FeatureNotPresentException(string message) : base(message) { }
    public FeatureNotPresentException(string message, int code) : base(message, code) { }
    public FeatureNotPresentException(string message, Exception inner) : base(message, inner) { }
    private FeatureNotPresentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}