using System.Runtime.Serialization;

namespace Atom.Engine;

[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
public sealed class EditorExposedAttribute : Attribute
{
    public string? Label { get; }
    public Type? Formatter { get; }
    public string? Format { get; }

    public EditorExposedAttribute(string? label = null, Type? formatter = null, string? format = null)
    {
        Label = label;

        if (formatter is not null && !formatter.IsAssignableTo(typeof(IFormatter)))
        {
            throw new ArgumentException($"Tried to assign a {formatter.Name} as a UI variable formatter, but it is not an IFormatter.", nameof(formatter));
        }
        
        Formatter = formatter;
        Format = format;
    }
}