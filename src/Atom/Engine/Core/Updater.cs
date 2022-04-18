using System.Diagnostics;

namespace Atom.Engine;

public static class Updater
{
    private static volatile bool _isRunning = true;

    private static readonly ManualResetEventSlim _frameStartEvent = new ();
    private static readonly ManualResetEventSlim _frameDoneEvent = new(initialState: true);
    
    public static void Run()
    {
        Time.Start();
        
        Thread frame_thread = new (LoopFrame)
        {
            IsBackground = true,
            Name = "Frame",
            Priority = ThreadPriority.Highest
        };
        frame_thread.Start();
        
        Thread physics_thread = new (LoopPhysics)
        {
            IsBackground = true,
            Name = "Physics",
            Priority = ThreadPriority.Highest
        };
        physics_thread.Start();
    }

    public static void Stop()
    {
        _isRunning = false;
        _frameStartEvent.Set();
    }

    private static void LoopFrame()
    {
        double previous_time = Time.Elapsed;
        
        while (_isRunning)
        {
            // wait for window thread to ask for an update
            _frameStartEvent.Wait();
            _frameStartEvent.Reset();
            _frameDoneEvent .Reset();
            if (!_isRunning) break;
            
            
            //== 0- Update Keys ==//
            Keyboard.NextFrame();
            Mouse.NextFrame();
            
            //== 1- Update Time ==//
            
            
            // compute delta time
            double now = Time.Elapsed;
            double delta_time = now - previous_time;
            previous_time = now;
            
            Time.NextUpdate(delta_time);
            Astrophysics.UniversalTime += delta_time * Astrophysics.TimeWarp;
            
            
            //== 2- Game Logic ==//
            foreach (AtomObject @object in AtomObject.Objects)
            {
                if (@object.IsDeleted) /* just to be sure */ continue;

                try { @object.Frame(); }
                catch (Exception e) { Log.Error(e); }
            }
            
            //== 3- Space relative transform to cameras ==//
            /*foreach (Thing thing in AtomObject.Objects.Where(o => o is Thing))
            {
                if (thing.IsDeleted) continue;
                
                foreach()
            }*/
            
            //= 3- Render Logic ==//
            foreach (AtomObject @object in AtomObject.Objects)
            {
                if (@object.IsDeleted) continue;

                try { @object.Render(); }
                catch (Exception e) { Log.Error(e); }
            }
            
            _frameDoneEvent.Set();
        }
    }
    
    private static void LoopPhysics()
    {
        f64 previous_time = Time.Elapsed;
        
        while (_isRunning)
        {
            DoNextPhysicsFrame();

            f64 now = Time.Elapsed;
            f64 frame_time = now - previous_time;
            previous_time = now;

            f64 expected_time = Time.PhysicsDeltaTime;

            f64 wait_time = expected_time - frame_time;

            if (wait_time > 0.0D) // pause thread if necessary
            {
                i32 wait_time_ms = (i32)(wait_time * 1000.0D);
                Thread.Sleep(wait_time_ms);
            }
        }
    }
    
    private static void DoNextPhysicsFrame()
    {
        foreach (AtomObject @object in AtomObject.Objects)
        {
            if (@object.IsDeleted) continue;
            
            try { @object.PhysicsFrame(); }
            catch (Exception e) { Log.Error(e); }
        }
    }
    
    public static void NextFrame() => _frameStartEvent.Set();

    public static void WaitUpdate() => _frameDoneEvent.Wait();
}