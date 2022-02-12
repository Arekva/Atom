namespace Atom.Engine;

public static class Interpolate
{
    /// <summary>
    /// Cubic interpolation.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <param name="c">Third value.</param>
    /// <param name="d">Fourth value.</param>
    /// <param name="t">Time (%age of lerp).</param>
    /// <returns>The interpolated value.</returns>
    public static double Cubic(double a, double b, double c, double d, double t)
    {
        double x = d - c - (a - b);
        double y = a - b - x;
        double z = c - a;
        double w = b;
        return x*t*t*t + y*t*t + z*t + w;
    }

    /// <summary>
    /// Linear interpolation.
    /// </summary>
    /// <param name="a">From value.</param>
    /// <param name="b">To value.</param>
    /// <param name="t">%age of lerp</param>
    /// <returns>The lerped value</returns>
    public static double Linear(double a, double b, double t) => (1.0D - t) * a + t * b;
        
    public static double SCurve3(double a) => a*a*(3.0 - 2.0*a);
    public static double SCurve5(double a) => a*a*a*(a*(a*6.0 - 15.0) + 10.0);
}