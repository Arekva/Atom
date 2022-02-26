using System.Text.Json;
using System.Text.Json.Serialization;
using SPIRVCross;

using Atom.Engine.Vulkan;

namespace Atom.Engine.Shader;

public abstract partial class Shader
{
    /// <summary> Where the game shaders are located. </summary>
    public const string ShaderPath = "Assets/Shaders/";
    /// <summary> The name of the shader descriptor JSON </summary>
    public const string DescriptorName = "shader.json";

    /// <summary> Arbitrary set maximum materials per shader. Used for descriptor pool allocation. </summary>
    public const uint MaxMaterialsPerShader = 1024; 
    
    
    private static readonly Version _maxSupportedFormatVersion = new (0,1,0);
    
    
    private static readonly JsonSerializerOptions _serializerOptions 
        = new() { Converters = { new JsonStringEnumConverter() } };
        
    
    public static T Load<T>(string @namespace, string name, vk.Device? device = null) where T : class, IShader
    {
        string namespace_path = Path.Combine(@namespace.Split('.'));
        string full_path = Path.Combine(ShaderPath, namespace_path, name, DescriptorName);

        string txt = File.ReadAllText(full_path);
        ShaderDescriptor descriptor = JsonSerializer.Deserialize<ShaderDescriptor>(txt, _serializerOptions)!;
        
        if (descriptor.FormatVersion > _maxSupportedFormatVersion)
        {
            throw new Exception("File format version is not supported.");
        }
        
        if (typeof(T) == typeof(RasterShader) || typeof(T) == typeof(IRasterShader))
        {
            return (LoadRasterShader(full_path, @namespace, descriptor, device ?? VK.Device) as T)!;
        }
        
        throw new NotImplementedException("Other shader types than RasterShader are not supported.");
    }
    
    
    private static RasterShader LoadRasterShader(string path, string @namespace, ShaderDescriptor descriptor, vk.Device device)
    {
        if (descriptor.Stages.Count > 2)
        {
            throw new NotImplementedException("Other shaders than fragment and vertex aren't implemented yet.");
        }

        RasterShader shader = new (
            @namespace, descriptor.Name, descriptor.Description, descriptor.Version,
            vertex: new VertexModule(new Program(Path.Combine(path, ".." , descriptor.Stages[ShaderStageFlags.Vertex])), device), 
            fragment: new FragmentModule(new Program(Path.Combine(path, ".." , descriptor.Stages[ShaderStageFlags.Fragment])), device)
        )
        {
            Namespace = @namespace,
            Name = descriptor.Name,
            Description = descriptor.Description,
            Version = descriptor.Version
        };

        // Cache[guid] = shader;

        return shader;
    }
}