using System.Runtime.Serialization;

namespace Atom.Engine;

[Serializable]
public sealed class InvalidDRMFormatModifierPlaneLayout : VulkanException
{
    public InvalidDRMFormatModifierPlaneLayout() { }
    public InvalidDRMFormatModifierPlaneLayout(string message) : base(message) { }
    public InvalidDRMFormatModifierPlaneLayout(string message, int code) : base(message, code) { }
    public InvalidDRMFormatModifierPlaneLayout(string message, Exception inner) : base(message, inner) { }
    private InvalidDRMFormatModifierPlaneLayout(SerializationInfo info, StreamingContext context) : base(info, context) { }
}