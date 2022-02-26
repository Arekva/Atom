using Atom.Engine;

namespace Atom.Game;

public class ImprovedPerlin
{
    private const int RandomSize = 256;

    private static readonly int[] Permutations = new int[RandomSize]
    {
        151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225,
        140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148,
        247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32,
        57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175,
        74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122,
        60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54,
        65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169,
        200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64,
        52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212,
        207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213,
        119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
        129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104,
        218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241,
        81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157,
        184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93,
        222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180,
    };

    private int[] _random;

    private int _seed;

    public int Seed
    {
        get => _seed;
        set
        {
            if (value == _seed) return; // don't recalculate
            _seed = value;
            Randomize();
        }
    }

    public NoiseQuality Quality { get; set; }

    public ImprovedPerlin(int seed, NoiseQuality quality) => (Seed, Quality) = (seed, quality);

    public ImprovedPerlin() : this(0, NoiseQuality.Standard)
    {
    }


    /// <summary>Initializes the random values</summary>
    /// <param name="seed">Seed of the noise</param>
    private void Randomize()
    {
        int seed = Seed;
        _random = new int[512];
        if (seed != 0)
        {
            Span<byte> buffer = stackalloc byte[4];
            ANoise.UnpackLittleInt32(seed, buffer);
            for (int index = 0; index < Permutations.Length; ++index)
            {
                _random[index] = Permutations[index] ^ buffer[0];
                _random[index] ^= buffer[1];
                _random[index] ^= buffer[2];
                _random[index] ^= buffer[3];
                _random[index + 256] = _random[index];
            }
        }
        else
        {
            for (int index = 0; index < RandomSize; ++index)
                _random[index + RandomSize] = _random[index] = Permutations[index];
        }
    }

    /// <summary>
    /// 1D sample (-1.0..1.0)
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <returns>Sampled value</returns>
    public double Sample(double x)
    {
        int n = x > 0.0 ? (int) x : (int) x - 1;
        int index = n & 255; // ceil to 255
        x -= n;
        return AMath.LinearInterpolation(Grad(_random[index], x), Grad(_random[index + 1], x - 1f), Quality switch
        {
            NoiseQuality.Fast => x,
            NoiseQuality.Standard => AMath.SCurve3(x),
            NoiseQuality.Best => AMath.SCurve5(x),
            _ => 0.0f
        });
    }

    public double Sample(double x, double y)
    {
        int num1 = x > 0.0 ? (int) x : (int) x - 1;
        int num2 = y > 0.0 ? (int) y : (int) y - 1;
        int index1 = num1 & 255;
        int num3 = num2 & 255;
        x -= num1;
        x -= num2;
        double a1 = 0.0f;
        double a2 = 0.0f;
        switch (Quality)
        {
            case NoiseQuality.Fast:
            {
                a1 = x;
                a2 = y;
            }
                break;
            case NoiseQuality.Standard:
            {
                a1 = AMath.SCurve3(x);
                a2 = AMath.SCurve3(y);
            }
                break;
            case NoiseQuality.Best:
            {
                a1 = AMath.SCurve5(x);
                a2 = AMath.SCurve5(y);
            }
                break;
        }

        int index2 = _random[index1] + num3;
        int index3 = _random[index1 + 1] + num3;
        return AMath.LinearInterpolation(AMath.LinearInterpolation(Grad(_random[index2], x, y), Grad(_random[index3], x - 1.0, y), a1),
            AMath.LinearInterpolation(Grad(_random[index2 + 1], x, y - 1.0), Grad(_random[index3 + 1], x - 1.0, y - 1.0), a1),
            a2);
    }

    public double Sample(double x, double y, double z)
    {
        int num1 = x > 0.0 ? (int) x : (int) x - 1;
        int num2 = y > 0.0 ? (int) y : (int) y - 1;
        int num3 = z > 0.0 ? (int) z : (int) z - 1;
        int index1 = num1 & 255;
        int num4 = num2 & 255;
        int num5 = num3 & 255;
        x -= num1;
        y -= num2;
        z -= num3;
        double a1 = 0.0;
        double a2 = 0.0;
        double a3 = 0.0;
        switch (Quality)
        {
            case NoiseQuality.Fast:
            {
                a1 = x;
                a2 = y;
                a3 = z;
            }
                break;
            case NoiseQuality.Standard:
            {
                a1 = AMath.SCurve3(x);
                a2 = AMath.SCurve3(y);
                a3 = AMath.SCurve3(z);
            }
                break;
            case NoiseQuality.Best:
            {
                a1 = AMath.SCurve5(x);
                a2 = AMath.SCurve5(y);
                a3 = AMath.SCurve5(z);
            }
                break;
        }

        int index2 = _random[index1] + num4;
        int index3 = _random[index2] + num5;
        int index4 = _random[index2 + 1] + num5;
        int index5 = _random[index1 + 1] + num4;
        int index6 = _random[index5] + num5;
        int index7 = _random[index5 + 1] + num5;
        return AMath.LinearInterpolation(
            AMath.LinearInterpolation(AMath.LinearInterpolation(Grad(_random[index3], x, y, z), Grad(_random[index6], x - 1.0, y, z), a1),
                AMath.LinearInterpolation(this.Grad(this._random[index4], x, y - 1.0, z),
                    this.Grad(this._random[index7], x - 1.0, y - 1.0, z), a1), a2),
            AMath.LinearInterpolation(
                AMath.LinearInterpolation(Grad(_random[index3 + 1], x, y, z - 1.0),
                    Grad(_random[index6 + 1], x - 1.0, y, z - 1.0), a1),
                AMath.LinearInterpolation(Grad(_random[index4 + 1], x, y - 1.0, z - 1f),
                    Grad(_random[index7 + 1], x - 1.0, y - 1.0, z - 1.0), a1), a2), a3);
    }


    /// <summary>Modifies the result by adding a directional bias</summary>
    /// <param name="hash">The random value telling in which direction the bias will occur</param>
    /// <param name="x">The amount of the bias on the X axis</param>
    /// <returns>The directional bias strength</returns>
    private double Grad(int hash, double x) => (hash & 1) != 0 ? -x : x;

    /// <summary>Modifies the result by adding a directional bias</summary>
    /// <param name="hash">The random value telling in which direction the bias will occur</param>
    /// <param name="x">The amount of the bias on the X axis</param>
    /// <param name="y">The amount of the bias on the Y axis</param>
    /// <returns>The directional bias strength</returns>
    private double Grad(int hash, double x, double y)
    {
        int num = hash & 3;
        return ((num & 2) == 0 ? x : -x) + ((num & 1) == 0 ? y : -y);
    }

    /// <summary>Modifies the result by adding a directional bias</summary>
    /// <param name="hash">The random value telling in which direction the bias will occur</param>
    /// <param name="x">The amount of the bias on the X axis</param>
    /// <param name="y">The amount of the bias on the Y axis</param>
    /// <param name="z">The amount of the bias on the Z axis</param>
    /// <returns>The directional bias strength</returns>
    private double Grad(int hash, double x, double y, double z)
    {
        int num1 = hash & 15;
        double num2 = num1 < 8 ? x : y;
        double num3 = num1 < 4 ? y : (num1 is 12 or 14 ? x : z);
        return ((num1 & 1) == 0 ? num2 : -num2) + ((num1 & 2) == 0 ? num3 : -num3);
    }
}