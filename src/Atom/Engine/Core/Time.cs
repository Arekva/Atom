using System.Diagnostics;

namespace Atom.Engine;

public static class Time
{
    private static Stopwatch _watch = null!;
    public static double Elapsed => _watch.Elapsed.TotalSeconds;
    
    private static double _deltaTime = PhysicsDeltaTime;
    public static double DeltaTime => _deltaTime;
    
    public static double PhysicsDeltaTime => 1 / 60.0D; // todo: faire en sorte de modifier ça.
    internal static void Start() => _watch = Stopwatch.StartNew();

    internal static void NextUpdate(double deltaTime) => _deltaTime = deltaTime;
}
