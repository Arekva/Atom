namespace Atom.Engine;

public static class Scene
{
    public static T Load<T>() where T : IScene, new() => new();
}



