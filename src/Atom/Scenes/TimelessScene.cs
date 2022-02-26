using Atom.Engine;

namespace Atom.Game.Scenes;

public class TimelessScene : IScene
{
    private ClassicSkySphere _sky;
    
    public TimelessScene()
    {
        _sky = new ClassicSkySphere();
    }
}