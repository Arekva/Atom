namespace Atom.Game;

public class ImprovedPerlinGenerator
{
    private ImprovedPerlin _perlin;

    public int Seed
    {
        get => _perlin.Seed;
        set => _perlin.Seed = value;
    }
    public NoiseQuality Quality
    {
        get => _perlin.Quality;
        set => _perlin.Quality = value;
    }
    public double Frequency = 1.0D;


    public ImprovedPerlinGenerator() : this("🌍") {}
    public ImprovedPerlinGenerator(string seed) : this(seed.GetHashCode()) {}
    public ImprovedPerlinGenerator(int seed) => _perlin = new ImprovedPerlin(seed, NoiseQuality.Standard);

    public double SampleDeformation(double x, double y, double z)
    {
        double freq = Frequency;
        return _perlin.Sample(x * freq, y * freq, z * freq);
    }
}