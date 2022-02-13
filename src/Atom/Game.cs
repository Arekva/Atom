using Atom.Engine;
using Atom.Engine.Astro;
using Silk.NET.Vulkan;
using Atom.Engine.Astro.Transvoxel;
using Atom.Engine.Shader;
using Silk.NET.Maths;

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
                /*using CelestialBody planet = new (
                    name: "Terre", 
                    radius: 6371000.0D, 
                    mass: 5.972E+24
                );

                Grid grid = planet.Grid;
                (Vector3D<float>[] vert, uint[] indices, Vector3D<float>[] normals) verts = grid.Cells.First().Visit();*/

                using IRasterShader shader = Shader.Load<IRasterShader>(@namespace: "Engine", name: "Standard");

                {
                    
                }

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