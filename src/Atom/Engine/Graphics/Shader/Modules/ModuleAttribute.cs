namespace Atom.Engine;

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
internal sealed class ModuleAttribute : Attribute
{
    public ShaderStageFlags Stage { get; }
    public string Extension { get; }

    public ModuleAttribute(ShaderStageFlags stage, string extension)
    {
        Stage = stage;
        Extension = extension;
    }
}

