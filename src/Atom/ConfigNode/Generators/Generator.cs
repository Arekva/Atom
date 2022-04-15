using System.Reflection;

namespace Atom.Game.Config;

public interface IGenerator { }

public class Generator : IGenerator
{
    [Bind("Radius")]
    public string Radius { get; set; }
    /*public static bool TryGetBind(string binding,
        out PropertyInfo? property,
        out BindType bindType,
        out string? dynamicBinding) { }*/
}