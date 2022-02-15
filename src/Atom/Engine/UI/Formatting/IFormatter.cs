namespace Atom.Engine;

public interface IFormatter
{
    public string Format(object? data, string? format);
    public object Parse(string? input);

    public bool TryParse(string? input, out object? result)
    {
        try
        {
            result = Parse(input);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}