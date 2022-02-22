using System.Diagnostics;
using Atom.Engine;
using Atom.Engine.Astro;
using Silk.NET.Vulkan;
using Atom.Engine.Astro.Transvoxel;
using Atom.Engine.Shader;
using Silk.NET.Input;
using Silk.NET.Maths;
using CommandBuffer = Atom.Engine.CommandBuffer;
using CommandBufferLevel = Atom.Engine.CommandBufferLevel;

namespace Atom.Game;

public class Game
{
    public static void Main(string[] args)
    {
        Engine.Engine.Run(gui: true, "Atom");
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
                Camera camera = new ();
                //camera.Location = new Location(Vector3D<double>.UnitY * 5.0D);
                camera.NearPlane = 0.01D;
                camera.FarPlane = 100.0D;

                using CelestialSystem sun_system = new (location: default);
                
                using (IRasterShader terrain_shader = Shader.Load<IRasterShader>(@namespace: "Engine", name: "Standard"))
                {
                    using CelestialBody planet = new (
                        name: "Terre", 
                        radius: 6371000.0D, 
                        mass: 5.972E+24,
                        terrain_shader,
                        sun_system
                    );


                    /*Random r = new Random(0);
                    for (int i = 0; i < 1000000000; i++)
                    {
                        double x = r.NextDouble();
                        double y = r.NextDouble();
                        double z = r.NextDouble();
                        camera.Space.Forward = Vector3D.Normalize(new Vector3D<double>(x, y, z));
                    }*/

                    double speed = 5.0D;
                    double rot_speed = 15.0;

                    double angle_y = 0.0D;
                    
                    Stopwatch sw = Stopwatch.StartNew();
                    double last_time = 0.0D;
                    for (int i = 0; i < 50000000; i++)
                    {
                        double elapsed = sw.Elapsed.TotalSeconds;
                        double delta_time = elapsed - last_time;
                        last_time = elapsed;

                        Vector3D<double> dir = Vector3D<double>.Zero;
                        if (Keyboard.IsPressed(Key.W))
                        {
                            dir += Vector3D<double>.UnitZ;
                        }
                        if (Keyboard.IsPressed(Key.S))
                        {
                            dir -= Vector3D<double>.UnitZ;
                        }
                        if (Keyboard.IsPressed(Key.A))
                        {
                            dir -= Vector3D<double>.UnitX;
                        }
                        if (Keyboard.IsPressed(Key.D))
                        {
                            dir += Vector3D<double>.UnitX;
                        }
                        if (Keyboard.IsPressed(Key.Q))
                        {
                            dir -= Vector3D<double>.UnitY;
                        }
                        if (Keyboard.IsPressed(Key.E))
                        {
                            dir += Vector3D<double>.UnitY;
                        }

                        if (dir != Vector3D<double>.Zero)
                        {
                            dir = Vector3D.Normalize(dir);
                        }
                        
                        camera.Location.Coordinates += dir * delta_time * speed;


                        if (Keyboard.IsPressed(Key.Left))
                        {
                            angle_y -= rot_speed * delta_time;
                        }
                        if (Keyboard.IsPressed(Key.Right))
                        {
                            angle_y += rot_speed * delta_time;
                        }

                        camera.Space.LocalRotation = Quaternion<double>.CreateFromAxisAngle(Vector3D<double>.UnitY, angle_y);

                        Log.Info(camera.Location.UniversalCoordinates + " / " + camera.Space.Forward);
                    }
                }
                
                
                

                //Grid grid = planet.Grid;
                //(Vector3D<float>[] vert, uint[] indices, Vector3D<float>[] normals) verts = grid.Cells.First().Visit();

                /*Device device = VK.Device;
                
                SlimCommandPool pool = new SlimCommandPool(device, 0);
                {
                    pool.AllocateCommandBuffer(device, CommandBufferLevel.Primary, out SlimCommandBuffer cmd);
                    
                    using (IRasterShader shader = Shader.Load<IRasterShader>(@namespace: "Engine", name: "Standard"))
                    {
                        using IRasterizedMaterial material = new RasterizedMaterial(shader);
                        
                        material.CmdBindMaterial(cmd, Video.Resolution, cameraIndex: 0);
                    }
                }
                pool.Destroy(device);*/

                /*using RasterShader shader = Shader.Load<RasterShader>("Engine", "Standard");
                using RasterizedMaterial material = new(shader);
    
                using OpaqueMesh<ushort> model = OpaqueMesh<ushort>.Load("Assets/Meshes/Suzanne.obj");
    
                using Element<double> element = new("Test Object");
                MeshRenderer renderer = element.AddModule<MeshRenderer>(model, material);
    
                UniversalElement player_element = new("Player");
                GameCamera camera = player_element.AddModule<GameCamera>();*/
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
        Thread win_thread = new (() =>
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