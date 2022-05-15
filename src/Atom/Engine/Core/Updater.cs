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
        
        _fpsWatch.Start();
        while (_isRunning)
        {
            // wait for window thread to ask for an update
            _frameStartEvent.Wait();
            /*_frameStartEvent.Reset();
            _frameDoneEvent .Reset();*/
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
            DoFPS(Time.DeltaTime);
            
            Astrophysics.UniversalTime += delta_time * Astrophysics.TimeWarp;
            
            
            //== 2- Move celestial bodies ==//
            /*foreach (Thing thing in AtomObject.Objects.Where(o => o is Thing))
            {
                if (thing.IsDeleted) continue;
                
                foreach()
            }*/
            
            
            //== 3- Game Logic ==//
            foreach (AtomObject @object in AtomObject.Objects)
            {
                if (@object.IsDeleted) /* just to be sure */ continue;

                try { @object.Frame(); }
                catch (Exception e) { Log.Error(e); }
            }
            foreach (AtomObject @object in AtomObject.Objects)
            {
                if (@object.IsDeleted) /* just to be sure */ continue;

                try { @object.LateFrame(); }
                catch (Exception e) { Log.Error(e); }
            }

            //= 4- Render Logic ==//
            foreach (AtomObject @object in AtomObject.Objects)
            {
                if (@object.IsDeleted) continue;

                try { @object.Render(); }
                catch (Exception e) { Log.Error(e); }
            }

            if (Graphics.IsRenderReady && Camera.World != null)
            {
                Viewport viewport = ViewportWindow.Instance.Viewport;
                Camera camera = Camera.World;

                RenderTarget final_render = camera.RenderImmediate(Graphics.FrameIndex, () =>
                {
                    viewport.WaitResizeFinished();
                    viewport.WaitForRender();
                });

                viewport.Present(final_render!.Color!);
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
    
    
    private static double _minTime = double.NegativeInfinity;
    private static double _maxTime = double.PositiveInfinity;
    private static List<double> _times = new (10000);
    private static Stopwatch _fpsWatch = new();
    private static double _showRate = 1.0D;

    private static void DoFPS(double deltaTime)
    {
        _minTime = Math.Max(_minTime, deltaTime);
        _maxTime = Math.Min(_maxTime, deltaTime);
        _times.Add(deltaTime);
        
        double elapsed = _fpsWatch.Elapsed.TotalSeconds;
        if (elapsed >= 1.0D / _showRate)
        {
            double avg = _times.Average();
            double min = _minTime;
            double max = _maxTime;

            double[] ordered_times = _times.OrderBy(d => d).ToArray();
            int time_count = ordered_times.Length;

            double tenPct = ordered_times[time_count / 10];
            Log.Info($"[|#FF9100,FPS|] " + Colour((u32)(1.0D/avg)) + " FPS");
                     //$"COUNT: {time_count} | AVG: {1.0D/avg:F0} ({avg*1000.0D:F2} ms) | 10% LOW: {1.0D/tenPct:F0} ({tenPct*1000.0D:F2} ms) /// MIN: {1.0D/min:F0} ({min*1000.0D:F2} ms) / MAX: {1.0D/max:F0} ({max*1.000D:F0} ms) ({elapsed:F2} sec)");
                     
            _times.Clear();
            _minTime = double.NegativeInfinity;
            _maxTime = double.PositiveInfinity;
            
            _fpsWatch.Restart();
        }
    }

    private static string GetColour(u32 fps) => fps switch
    {
        < 10    => "#630000",
        < 20    => "#c40000",
        < 30    => "#ff0000",
        < 40    => "#ff6a00",
        < 50    => "#ffb700",
        < 60    => "#ffea00",
        < 80    => "#d0ff00",
        < 110   => "#91ff00",
        < 144   => "#62ff00",
        < 200   => "#00ffc8",
        < 240   => "#00ddff",
        < 360   => "#007bff",
        < 700   => "#001aff",
        < 1500  => "#8400ff",
        _       => "#dd00ff",
    };

    private static string Colour(u32 fps) => $"|{GetColour(fps)},{fps}|";

    private static string Colour(f64 seconds) => $"|{GetColour((u32)(1.0D / seconds))},{seconds:F2}|";
}