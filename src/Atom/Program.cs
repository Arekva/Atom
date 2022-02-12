using Atom.Engine;
using Silk.NET.Vulkan;

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
            // make window thread
            Thread window_thread = create_window();

            Graphics.WaitRenderReady();

            try
            {
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
        win_thread.Priority = ThreadPriority.AboveNormal;
        win_thread.Name = "Window";
        win_thread.IsBackground = true;
        win_thread.Start();

        return win_thread;
    }
}