namespace Atom.Engine;

public sealed class SpeedFormatter : INumericFormatter
{
    public string Format(object? data, string? format = ".00D")
    {
        if (data is null) throw new FormatException("Data must be a numeric type.");
        if (data is IFormattable form
            and (byte or sbyte or short or ushort or int or uint or long or ulong or Half or float or double))
        {
            return form.ToString(format, Thread.CurrentThread.CurrentUICulture) + " u/s";
        }

        throw new FormatException($"Unable to format value of type \"{data.GetType().Name}\"");
    }

    public object Parse(string? input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input), "No input string has been set.");
        string normalized = input.ToLowerInvariant();

        string toParse = normalized.Split("u/s", StringSplitOptions.TrimEntries).First();


        // force use doubles
        double parsed = double.Parse(toParse ?? normalized);
        return parsed;
    }
}