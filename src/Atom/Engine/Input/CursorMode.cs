namespace Atom.Engine;

/// <summary>
/// <seealso cref="Silk.NET.Input.CursorMode"/>
/// </summary>
public enum CursorMode
{
    /// <summary> Cursor is visible and has no restrictions on mobility. </summary>
    Normal = Silk.NET.Input.CursorMode.Normal,
    /// <summary> Cursor is invisible, and has no restrictions on mobility. </summary>
    Hidden = Silk.NET.Input.CursorMode.Hidden,
    /// <summary> Cursor is invisible, and is restricted to the center of the screen. </summary>
    /// <remarks>Only supported by GLFW, throws on SDL if used.</remarks>
    Disabled = Silk.NET.Input.CursorMode.Disabled,
    /// <summary> Cursor is invisible, and is restricted to the center of the screen. Mouse motion is not scaled. </summary>
    Raw = Silk.NET.Input.CursorMode.Raw,
}