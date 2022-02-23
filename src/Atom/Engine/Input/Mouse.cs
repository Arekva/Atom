using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Atom.Engine;

public static class Mouse
{
    private static IInputContext _context;
    public static IInputContext Context // todo: multiple keyboards
    {
        get => _context;
        set
        {
            _context = value ?? throw new ArgumentNullException(nameof(value));

            OnCursorModeChanged += mode =>
            {
                foreach (IMouse mouse in value.Mice)
                {
                    mouse.Cursor.CursorMode = mode;
                }
            };
        }
    }

    private static Vector2D<double> _previousFramePosition = new (double.PositiveInfinity);
    public static Vector2D<double> Delta { get; private set; }


    private static CursorMode _mode = CursorMode.Normal;

    public static CursorMode Mode
    {
        get => _mode;
        set
        {
            if (value != _mode)
            {
                _mode = value;
                OnCursorModeChanged?.Invoke(value);
            }
        }
    }
    public static Action<CursorMode>? OnCursorModeChanged;

    internal static void NextFrame()
    {
        IMouse mouse = _context.Mice.First();
        Vector2 f_pos = mouse.Position;
        Vector2D<double> d_pos = new(f_pos.X, f_pos.Y);

        if (!double.IsPositiveInfinity(_previousFramePosition.X))
        {
            Delta = d_pos - _previousFramePosition;
        }
        
        _previousFramePosition = d_pos;
    }
}