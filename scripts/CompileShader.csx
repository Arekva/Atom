using System.Text.Json;
using System.Text.Json.Serialization;

[Flags]
public enum ShaderStage
{
    Vertex = 1,
    TessellationControl = 2,
    TessellationEvaluation = 4,
    Geometry = 8,
    Fragment = 16,
    Compute = 32,
    AllGraphics = Fragment | Geometry | TessellationEvaluation | TessellationControl | Vertex,
    All = 2147483647,
    Raygen/*_KHR*/ = 256,
    AnyHit/*_KHR*/ = 512,
    ClosestHit/*_KHR*/ = 1024,
    MissBit/*_KHR*/ = 2048,
    Intersection/*_KHR*/ = 4096,
    Callable/*_KHR*/ = 8192,
    SubpassShading/*_Huawei*/ = 16384
}

public class ShaderDescriptor
{
    [JsonPropertyName("Format Version")] 
    public Version FormatVersion { get; set; }
    
    public string Name { get; set; }
    public string Description { get; set; }
    
    public Version Version { get; set; }

    public Dictionary<ShaderStage, string> Stages { get; set; }
}

const string SHADER_PATH = "assets/Shaders/";
const string SHADER_DESCRIPTOR = "shader.json";
const string SOURCE_PATH = "Source";
const string COMPILE_PATH = "Modules";
const string INCLUDE = "Include";

string namespaced_path = Path.Combine(Args[0].Split('.'));

string path_to_shader_folder = Path.Combine(SHADER_PATH, namespaced_path);
string path_to_modules_sources = Path.Combine(path_to_shader_folder, SOURCE_PATH);
string path_to_modules_compiles = Path.Combine(path_to_shader_folder, COMPILE_PATH);
string path_to_include = Path.Combine(SHADER_PATH, INCLUDE);

Directory.CreateDirectory(path_to_modules_compiles);

foreach(string source_file_path in Directory.GetFiles(path_to_modules_sources))
{
    string compiled_file_name = Path.GetFileName(source_file_path);

    string load_path = source_file_path;
    string save_path = Path.Combine(path_to_modules_compiles, compiled_file_name + ".spv");

    ProcessStartInfo info = new ()
    {
        FileName = "glslc",
        Arguments = $"--target-env=vulkan1.2 -I {path_to_include} -o {save_path} {load_path}",
        UseShellExecute = false
    };

    Process.Start(info);
}