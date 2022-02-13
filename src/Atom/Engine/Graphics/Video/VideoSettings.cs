using Silk.NET.Maths;

namespace Atom.Engine;

public static class Video
{
    // resolution 
    private static Vector2D<uint> _resolution = Resolutions.HD;
    public static event Action<Vector2D<uint>>? OnResolutionChanged;
    public static event Action<Vector2D<uint>>? OnResolutionManuallyChanged;

    public static Vector2D<uint> Resolution
    {
        get => _resolution;
        set
        {
            if (_resolution != value)
            {
                _resolution = value;
                OnResolutionChanged?.Invoke(value);
                OnResolutionManuallyChanged?.Invoke(value);
            }
        }
    }

    /// <summary>
    /// Used for the Viewport to update its own resolution without invoking its own callback
    /// (and cause an infinite resize loop / stackoverflow).
    /// This method calls <see cref="OnResolutionChanged"/> but not <see cref="OnResolutionManuallyChanged"/>.
    /// </summary>
    /// <param name="resolution">The new resolution to set.</param>
    public static void SetResolutionAutoChange(Vector2D<uint> resolution)
    {
        _resolution = resolution;
        OnResolutionChanged?.Invoke(resolution);
    }

    public static double AspectRatio => (double)_resolution.X / (double)_resolution.Y;

    // display mode
    private static DisplayMode _displayMode = DisplayMode.Normal;
    public static event Action<DisplayMode>? OnDisplayModeChanged;

    public static DisplayMode DisplayMode
    {
        get => _displayMode;
        set
        {
            if (_displayMode != value)
            {
                _displayMode = value;
                OnDisplayModeChanged?.Invoke(value);
            }
        }
    }

    // vsync
    private static bool _vsync = true;
    public static event Action<bool>? OnVSyncChanged;

    public static bool VSync
    {
        get => _vsync;
        set
        {
            if (_vsync != value)
            {
                _vsync = value;
                OnVSyncChanged?.Invoke(value);
            }
        }
    }

    // do limit fps
    private static bool _limitFPS = false;
    public static event Action<bool>? OnLimitFPSChanged;

    public static bool LimitFPS
    {
        get => _limitFPS;
        set
        {
            if (_limitFPS != value)
            {
                _limitFPS = value;
                OnLimitFPSChanged?.Invoke(value);
            }
        }
    }

    // fps limit
    private static ushort _fpsLimit = 60;
    public static event Action<ushort>? OnFPSLimitChanged;

    public static ushort FPSLimit
    {
        get => _fpsLimit;
        set
        {
            if (_fpsLimit != value)
            {
                _fpsLimit = value;
                OnFPSLimitChanged?.Invoke(value);
            }
        }
    }

    // window title
    // todo: probably move that out of the video settings, more like in the general game ones.
    private static unsafe string _title = $"Atom Game x{sizeof(IntPtr)*8}";
    public static event Action<string>? OnTitleChanged;
    public static string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnTitleChanged?.Invoke(value);
            }
        }
    }
}