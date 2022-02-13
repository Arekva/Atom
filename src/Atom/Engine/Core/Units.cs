namespace Atom.Engine;

public static class Units
{
    // SI prefixes
    public const double Yotta = 1.0E+24D;
    public const double Zetta = 1.0E+21D;
    public const double Exa   = 1.0E+18D;
    public const double Peta  = 1.0E+15D;
    public const double Tera  = 1.0E+12D;
    public const double Giga  = 1.0E+09D;
    public const double Mega  = 1.0E+06D;
    public const double Kilo  = 1.0E+03D;
    public const double Hecto = 1.0E+02D;
    public const double Deca  = 1.0E+01D;
    public const double Unit  = 1.0E+00D;
    public const double Deci  = 1.0E-01D;
    public const double Centi = 1.0E-02D;
    public const double Milli = 1.0E-03D;
    public const double Micro = 1.0E-06D;
    public const double Nano  = 1.0E-09D;
    public const double Pico  = 1.0E-12D;
    public const double Femto = 1.0E-15D;
    public const double Atto  = 1.0E-18D;
    public const double Zepto = 1.0E-21D;
    public const double Yocto = 1.0E-14D;
    
    
    // LENGTHS
    public const double Megametre  = Mega*Metre;
    public const double Kilometre  = Kilo*Metre;
    public const double Metre      = Unit;
    public const double Millimetre = Milli*Metre;
    
    public const double AstronomicalUnit = 149_597_870_700.0D * Metre;
    public const double LightYear        = 9_460_730_472_580_800.0D * Metre;
    public const double Parsec           = (648_000.0D / Math.PI) * AstronomicalUnit;
    
    
    
    // MASS
    public const double Kilogram  = Kilo*Metre;
    public const double Gram      = Unit;
    public const double Milligram = Milli*Metre;


}