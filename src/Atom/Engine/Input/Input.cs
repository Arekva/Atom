namespace Atom.Engine;

public static class Input
{
    private static bool _gameFocus = false;
    public static bool GameFocus
    {
        get => _gameFocus;
        set
        {
            _gameFocus = value;

            Mouse.GameFocus = value;
            Keyboard.GameFocus = value;
        }
    }
}