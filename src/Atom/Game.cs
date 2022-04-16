using System.Diagnostics;
using Atom.Engine;
using Atom.Engine.Astro;
using Silk.NET.Vulkan;
using Atom.Engine.Astro.Transvoxel;
using Atom.Engine.Shader;
using Atom.Game.Config;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Atom.Game;

public class Game
{
    public static void Main(string[] args)
    {
        Engine.Engine.Run(gui: true, "Atom");
    }

    private static void Run()
    {
        Mouse.CursorMode = CursorMode.Raw;

        using IScene scene = Scene.Load<SpaceScene>();

        Engine.Engine.WaitForShutdown();
    }

    [Entry]
    private static void GameEntry()
    {
        try
        {
            Video.Title = $"{Engine.Game.Name} {Engine.Game.Version}";

            // make window thread
            Thread window_thread = create_window();

            Graphics.WaitRenderReady();

            try
            {
                Run();

                //PlanetConfig config = Config.Config.LoadInto<PlanetConfig>("assets/Space/Earth.planet");
                
                GC.Collect(2, GCCollectionMode.Forced, true, false);

                {
                    
                }

                /*using CelestialSystem sun_system = new (location: default);
                
                using (IRasterShader terrain_shader = Shader.Load<IRasterShader>(@namespace: "Engine", name: "Standard"))
                {
                    ImprovedPerlinGenerator noise = new()
                    {
                        Frequency = 5.0D
                    };

                    Func<double, double, double, double> generator = (x, y, z) =>
                    {
                        double sphere = x * x + y * y + z * z - 1.0;
                        
                        return sphere + noise.SampleDeformation(x, y, z) * 0.1;
                    };


                    using CelestialBody planet = new (
                        name: "Terre", 
                        radius: 6371000.0D, 
                        mass: 5.972E+24D,
                        terrain_shader,
                        sun_system,
                        generator
                    );

                    double rad = planet.Radius;
                    
                    Camera camera = new ();
                    camera.Location = new Location(-Vector3D<double>.UnitZ * rad);
                    camera.NearPlane = 0.01D;
                    camera.FarPlane = double.PositiveInfinity;

                    double speed = 1000D * rad;
                    double mouse_speed = 45.0D;
                    double rot_speed = 90.0;

                    double angle_x = 0.0D; // -45.0D;
                    double angle_y = 0.0D; // 180.0D - 45.0D;
                    double angle_z = 0.0D; // 0.0D;
                    
                    Stopwatch sw = Stopwatch.StartNew();
                    double last_time = 0.0D;
                    while (Engine.Engine.IsRunning)
                    {
                        double elapsed = sw.Elapsed.TotalSeconds;
                        double delta_time = elapsed - last_time;
                        last_time = elapsed;

                        if (Keyboard.IsPressed(Key.Escape)) Input.GameFocus = false;

                        angle_y += Mouse.Delta.X * mouse_speed * delta_time;
                        angle_x += Mouse.Delta.Y * mouse_speed * delta_time;
                        if (Keyboard.IsPressed(Key.Q)) angle_z += rot_speed * delta_time;
                        if (Keyboard.IsPressed(Key.E)) angle_z -= rot_speed * delta_time;

                        camera.Space.LocalRotation =
                            Quaternion<double>.CreateFromAxisAngle(Vector3D<double>.UnitY, angle_y * AMath.DegToRad) *
                            Quaternion<double>.CreateFromAxisAngle(Vector3D<double>.UnitX, angle_x * AMath.DegToRad);

                        Vector3D<double> dir = Vector3D<double>.Zero;
                        
                        Vector3D<double> right = camera.Space.Right;
                        Vector3D<double> up = camera.Space.Up;
                        Vector3D<double> forward = camera.Space.Forward;

                        if (Keyboard.IsPressed(Key.W)) dir += forward;
                        if (Keyboard.IsPressed(Key.S)) dir -= forward;
                        if (Keyboard.IsPressed(Key.A)) dir -= right;
                        if (Keyboard.IsPressed(Key.D)) dir += right;
                        
                        if (Keyboard.IsPressed(Key.R)) dir += up;
                        if (Keyboard.IsPressed(Key.F)) dir -= up;

                        if (dir != Vector3D<double>.Zero) dir = Vector3D.Normalize(dir);

                        camera.Location.Coordinates += dir * delta_time * speed;

                        // Log.Info("Coords: " + camera.Location.UniversalCoordinates + " / View: " + camera.Space.Forward);
                    }
                }*/
            }
            catch (Exception e)
            {
                Log.Fatal($"Unable to start test: {e}");
            }

            window_thread.Join();

            Engine.Engine.Quit();
        }
        catch (Exception e)
        {
            Engine.Engine.Abort(e);
        }
    }

    static Thread create_window()
    {
        // make window thread
        Thread win_thread = new(() =>
        {
            using ViewportWindow viewport_window = new();
            viewport_window.Run();
        });
        win_thread.Priority = ThreadPriority.Highest;
        win_thread.Name = "Window";
        win_thread.IsBackground = true;
        win_thread.Start();

        return win_thread;
    }
}