using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace Atom.Game.Config;

public interface IGenerator { }

public class Generator : IGenerator
{
    [Bind("Radius", DataType.Length)]
    public double Radius { get; set; }


    [Bind("Ocean")]
    public Ocean Ocean { get; set; }

    [Bind("Atmosphere")]
    public Atmosphere Atmosphere { get; set; }
}

public class Ocean
{
    [Bind("Composition", DataType.Percentage)]
    public Dictionary<Element, double> Composition { get; set; }
    
    [Bind("Coverage", DataType.Percentage)]
    public double Coverage { get; set; }
}

public class Atmosphere
{
    [Bind("Composition", DataType.Percentage)]
    public Dictionary<Element, double> Composition { get; set; }
    
    [Bind("Height", DataType.Length)]
    public double Height { get; set; }
    
    [Bind("Pressure", DataType.Pressure)]
    public double Pressure { get; set; }
}

[DebuggerDisplay("{Name}")]
public class Element
{
    private static Dictionary<string, Element> _map;

    static Element()
    {
        _map = new Element[]
        {
            new("Water"),
            new("Nitrogen"),
            new("Dioxygen"),
            new("Argon"),
            new("CarbonDioxide"),
            new("Neon"),
            new("Helium"),
            new("Methane"),
            new("Krypton"),
            new("Hydrogen")
        }.ToDictionary(e => e.Name);
    }

    private static Element GetMapValue(string name) => _map[name];
    
    public string Name { get; }

    public Element(string name) => Name = name;

    public override String ToString() => Name;
}