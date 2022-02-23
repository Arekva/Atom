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
                Mouse.Mode = CursorMode.Raw;

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

                    double rad = planet.Radius;
                    
                    Camera camera = new ();
                    camera.Location = new Location(-Vector3D<double>.UnitZ * rad);
                    camera.Space.Rotation = Quaternion<double>.CreateFromYawPitchRoll(Math.PI, 0.0, 0.0);
                    camera.NearPlane = 0.01D;
                    camera.FarPlane = double.PositiveInfinity;
                    
                    double speed = 2.5D * rad;
                    double mouse_speed = 20.0D;
                    double rot_speed = 90.0;

                    
                    double angle_x = 0.0D;
                    double angle_y = 0.0D;
                    double angle_z = 0.0D;
                    
                    Stopwatch sw = Stopwatch.StartNew();
                    double last_time = 0.0D;
                    while (Engine.Engine.IsRunning)
                    {
                        double elapsed = sw.Elapsed.TotalSeconds;
                        double delta_time = elapsed - last_time;
                        last_time = elapsed;

                        if (Keyboard.IsPressed(Key.Escape))
                        {
                            Mouse.Mode = CursorMode.Normal;
                        }

                        angle_y += Mouse.Delta.X * mouse_speed * delta_time;

                        angle_x += Mouse.Delta.Y * mouse_speed * delta_time;
                        if (Keyboard.IsPressed(Key.Q))
                        {
                            angle_z += rot_speed * delta_time;
                        }
                        if (Keyboard.IsPressed(Key.E))
                        {
                            angle_z -= rot_speed * delta_time;
                        }


                        camera.Space.LocalRotation =
                            Quaternion<double>.CreateFromAxisAngle(Vector3D<double>.UnitY, angle_y * AMath.DegToRad) *
                            Quaternion<double>.CreateFromAxisAngle(Vector3D<double>.UnitX, angle_x * AMath.DegToRad); //*
                            //Quaternion<double>.CreateFromAxisAngle(Vector3D<double>.UnitZ, angle_z * AMath.DegToRad) ;
                        
                        
                        
                        Vector3D<double> dir = Vector3D<double>.Zero;
                        
                        Vector3D<double> right = camera.Space.Right;
                        Vector3D<double> up = camera.Space.Up;
                        Vector3D<double> forward = camera.Space.Forward;

                        if (Keyboard.IsPressed(Key.W))
                        {
                            dir += forward;
                        }
                        if (Keyboard.IsPressed(Key.S))
                        {
                            dir -= forward;
                        }
                        if (Keyboard.IsPressed(Key.A))
                        {
                            dir -= right;
                        }
                        if (Keyboard.IsPressed(Key.D))
                        {
                            dir += right;
                        }
                        if (Keyboard.IsPressed(Key.R))
                        {
                            dir += up;
                        }
                        if (Keyboard.IsPressed(Key.F))
                        {
                            dir -= up;
                        }

                        if (dir != Vector3D<double>.Zero)
                        {
                            dir = Vector3D.Normalize(dir);
                        }
                        
                        camera.Location.Coordinates += dir * delta_time * speed;

                        Log.Info(camera.Location.UniversalCoordinates);
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