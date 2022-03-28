using System.Text.Json.Serialization;
using Silk.NET.Vulkan;

namespace Atom.Engine;

public class ShaderDescriptor
{
    [JsonPropertyName("Format Version")] 
    public Version FormatVersion { get; set; }
    
    public string Name { get; set; }
    public string? Description { get; set; }
    
    public Version Version { get; set; }
    
    public string? Light { get; set; }

    public Dictionary<ShaderStageFlags, string> Stages { get; set; }
}