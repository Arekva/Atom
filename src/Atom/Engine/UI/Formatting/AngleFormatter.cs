namespace Atom.Engine;

public sealed class AngleFormatter : INumericFormatter
{
    public const string DegreeUnit = "°";
    public const string RadianUnit = "Rad";
    private static string[] _degreeUnits = { "°", /* English */ "degree", "degrees", /* French */ "degre", "degres" };
    private static string[] _radianUnits = { "rad", "rads", "radian", "radians" };

    public string Format(object? data, string? format = ".00D")
    {
        if (data is null) throw new FormatException("Data must be a numeric type.");
        if (data is IFormattable form
            and (byte or sbyte or short or ushort or int or uint or long or ulong or Half or float or double))
        {
            if (format.Contains("R"))
            {
                return form.ToString(format, Thread.CurrentThread.CurrentCulture).Replace("R", RadianUnit);
            }
            else
            {
                if (format.Contains("D"))
                {
                    return form.ToString(format, Thread.CurrentThread.CurrentCulture).Replace("D", DegreeUnit);
                }
                else
                {
                    return form.ToString(format, Thread.CurrentThread.CurrentCulture) + DegreeUnit;
                }
            }
        }

        throw new FormatException($"Unable to format value of type \"{data.GetType().Name}\"");
    }

    public object Parse(string? input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input), "No input string has been set.");
        string normalized = input.ToLowerInvariant();

        string? toParse = null;
        bool isRadUnit = false;
        for (int i = 0; i < _radianUnits.Length; i++)
        {
            if (normalized.Contains(_radianUnits[i]))
            {
                isRadUnit = true;
                toParse = normalized.Replace(_radianUnits[i], "");
                break;
            }
        }

        if (toParse is null)
        {
            for (int i = 0; i < _degreeUnits.Length; i++)
            {
                if (normalized.Contains(_degreeUnits[i]))
                {
                    toParse = normalized.Replace(_degreeUnits[i], "");
                    break;
                }
            }
        }

        // force use doubles
        double parsed = double.Parse(toParse ?? normalized);
        if (!isRadUnit) parsed *= AMath.RadToDeg;
        return parsed;
    }
}