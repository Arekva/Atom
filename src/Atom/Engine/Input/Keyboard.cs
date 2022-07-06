using Silk.NET.Input;

namespace Atom.Engine;

public static class Keyboard
{
    private static readonly HashSet<Key> _recordingFrameKeys = new();
    private static HashSet<Key> _thisFrameKeys = new();
    private static HashSet<Key> _previousFrameKeys = new();
    
    private static IInputContext _context;
    
    public static IInputContext Context // todo: multiple keyboards
    {
        get => _context;
        set
        {
            _context = value ?? throw new ArgumentNullException(nameof(value));
            foreach (IKeyboard keyboard in value.Keyboards)
            {
                keyboard.KeyDown += OnKeyDown;
                keyboard.KeyUp += OnKeyUp;
            }

            Mouse.Context = value;
        }
    }

    public static bool IsAnyPressed() => GameFocus && _thisFrameKeys.Count != 0;

    public static bool IsPressed(Key key) => GameFocus && _thisFrameKeys.Contains(key);
    public static bool IsReleased(Key key) => GameFocus && !IsPressed(key);

    public static bool IsPressing(Key key) 
        => GameFocus && _thisFrameKeys.Contains(key) && !_previousFrameKeys.Contains(key);

    public static bool IsReleasing(Key key) 
        => GameFocus && !_thisFrameKeys.Contains(key) && _previousFrameKeys.Contains(key);

    private static void OnKeyUp(IKeyboard keyboard, Silk.NET.Input.Key key, i32 idk)
        => _recordingFrameKeys.Remove((Key)key);
    private static void OnKeyDown(IKeyboard keyboard, Silk.NET.Input.Key key, i32 idk) 
        => _recordingFrameKeys.Add((Key)key);

    internal static void NextFrame()
    {
        _previousFrameKeys = _thisFrameKeys;
        _thisFrameKeys = _recordingFrameKeys.ToHashSet();
    }
    
    
    private static bool _gameFocus = false;
    public static bool GameFocus
    {
        get => _gameFocus;
        set => _gameFocus = value;
    }
}