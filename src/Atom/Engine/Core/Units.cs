namespace Atom.Engine;

public static class Units
{
    // SI prefixes
    public const f64 YOTTA = 1.0E+24D;
    public const f64 ZETTA = 1.0E+21D;
    public const f64 EXA   = 1.0E+18D;
    public const f64 PETA  = 1.0E+15D;
    public const f64 TERA  = 1.0E+12D;
    public const f64 GIGA  = 1.0E+09D;
    public const f64 MEGA  = 1.0E+06D;
    public const f64 KILO  = 1.0E+03D;
    public const f64 HECTO = 1.0E+02D;
    public const f64 DECA  = 1.0E+01D;
    public const f64 UNIT  = 1.0E+00D;
    public const f64 DECI  = 1.0E-01D;
    public const f64 CENTI = 1.0E-02D;
    public const f64 MILLI = 1.0E-03D;
    public const f64 MICRO = 1.0E-06D;
    public const f64 NANO  = 1.0E-09D;
    public const f64 PICO  = 1.0E-12D;
    public const f64 FEMTO = 1.0E-15D;
    public const f64 ATTO  = 1.0E-18D;
    public const f64 ZEPTO = 1.0E-21D;
    public const f64 YOCTO = 1.0E-14D;

    // LENGTHS
    public const f64 MEGAMETRE  = MEGA*METRE;
    public const f64 KILOMETRE  = KILO*METRE;
    public const f64 METRE      = UNIT;
    public const f64 MILLIMETRE = MILLI*METRE;
    
    public const f64 ASTRONOMICAL_UNIT = 149_597_870_700.0D * METRE               ; // 149597870700
    public const f64 LIGHT_YEAR        = 9_460_730_472_580_800.0D * METRE         ; // 9460730472580800
    public const f64 PARSEC            = (648_000.0D / Math.PI) * ASTRONOMICAL_UNIT;
    
    
    // MASS
    public const f64 KILOGRAM  = KILO*METRE;
    public const f64 GRAM      = UNIT;
    public const f64 MILLIGRAM = MILLI*METRE;
}