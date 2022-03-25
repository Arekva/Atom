using System.Reflection;

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


    public static readonly Dictionary<Type, ShaderStageFlags> InterfaceStageMap;

    static ModuleAttribute()
    {
        InterfaceStageMap = new Dictionary<Type, ShaderStageFlags>(capacity: 5);

        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsInterface))
        {
            ModuleAttribute? attribute = type.GetCustomAttribute<ModuleAttribute>();
            if (attribute != null)
            {
                InterfaceStageMap.Add(type, attribute.Stage);
            }
        }
    }
}

