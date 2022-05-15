using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Atom.Engine;

public static class Mouse
{
#region Inputs 
    
    private static readonly HashSet<MouseButton> _recordingFrameButtons = new();
    private static HashSet<MouseButton> _thisFrameButtons = new();
    private static HashSet<MouseButton> _previousFrameButtons = new();
    
    public static bool IsAnyPressed() => _thisFrameButtons.Count != 0;

    public static bool IsPressed(MouseButton button) => _thisFrameButtons.Contains(button);
    public static bool IsReleased(MouseButton button) => !IsPressed(button);
    public static bool IsPressing(MouseButton button) => _thisFrameButtons.Contains(button) && !_previousFrameButtons.Contains(button);
    public static bool IsReleasing(MouseButton button) => !_thisFrameButtons.Contains(button) && _previousFrameButtons.Contains(button);
    
#endregion
    
    
    private static IInputContext _context;
    public static IInputContext Context // todo: multiple keyboards
    {
        get => _context;
        set
        {
            _context = value ?? throw new ArgumentNullException(nameof(value));

            foreach (IMouse mouse in _context.Mice)
            {
                mouse.DoubleClickTime = -1;
                
                mouse.Click += (_, _, _) => Input.GameFocus = true;
            }
        }
    }


    private static Vector2D<double> _previousFramePosition = new (double.PositiveInfinity);
    private static Vector2D<double> _delta = Vector2D<double>.Zero;
    public static Vector2D<double> Delta => InputFocus ? _delta : Vector2D<double>.Zero;


    private static bool _windowFocus = true;
    internal static bool WindowFocus
    {
        get => _windowFocus;
        set
        {
            _windowFocus = value;

            if (!value)
            {
                GameFocus = false;
            }

            _previousFramePosition = new Vector2D<double>(double.PositiveInfinity);

            UpdateCurrentCursorMode();
        }
    }
    
    private static bool _gameFocus = false;
    public static bool GameFocus
    {
        get => _gameFocus;
        set
        {
            _gameFocus = value;

            _previousFramePosition = new Vector2D<f64>(double.PositiveInfinity);

            UpdateCurrentCursorMode();
        }
    }

    public static bool InputFocus => _gameFocus && _windowFocus;
    
    

    private static CursorMode _userCursorMode = CursorMode.Normal;
    public static CursorMode CursorMode
    {
        get => _userCursorMode;
        set
        {
            _userCursorMode = value;

            UpdateCurrentCursorMode();
        }
    }
    public static event Action<CursorMode>? OnCursorModeChanged;

    private static void UpdateCurrentCursorMode()
    {
        _context.Mice[0].Cursor.CursorMode = InputFocus ? _userCursorMode : CursorMode.Normal;
    }
    
    
    public static Vector2D<double> ViewportPosition { get; private set; }
    
    public static Vector2D<double> NormalizedViewportPosition { get; private set; }

    
    
    
    
    internal static void NextFrame()
    {
        IMouse mouse = _context.Mice.First();
        Vector2 f_pos = mouse.Position;
        Vector2D<double> d_pos = new(f_pos.X, f_pos.Y);

        if (!double.IsPositiveInfinity(_previousFramePosition.X))
        {
            _delta = d_pos - _previousFramePosition;
        }
        
        _previousFramePosition = d_pos;

        Vector2D<uint> viewport_resolution = Video.Resolution;

        ViewportPosition = new Vector2D<double>(
            x: d_pos.X,
            y: viewport_resolution.Y - d_pos.Y
        );

        NormalizedViewportPosition = new Vector2D<double>(
            x: ViewportPosition.X / viewport_resolution.X,
            y: ViewportPosition.Y / viewport_resolution.Y
        );
        
        _previousFrameButtons = _thisFrameButtons;
        _thisFrameButtons = _recordingFrameButtons.ToHashSet();
    }
}