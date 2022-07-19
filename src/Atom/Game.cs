using Atom.Engine;
using Atom.Game.PlanetEditor;
using Atom.Tricot;

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

            if (!Engine.Engine.IsRunning) return; // something happened idk what but we must not run the game.

            try
            {
                Run();
                
                GC.Collect( // enforce collection to free potential native handles
                    generation: 2                      , 
                    mode      : GCCollectionMode.Forced,
                    blocking  : true                   , 
                    compacting: false
                );
            }
            catch (Exception e) { Log.Fatal($"Unable to start test: {e}"); }

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
        })
        {
            Priority = ThreadPriority.Highest,
            Name = "Window",
            IsBackground = true
        };
        win_thread.Start();

        return win_thread;
    }
}