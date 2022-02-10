namespace Atom.Engine;

/// <summary>
/// Marks a static method as the entry point of the engine into the game code. The method is called from <see cref="Updater.Initialize()"/> in the <see cref="Updater"/> thread.
/// </summary>
/// <example>
/// <code>
/// [Entry] private static void StartGame()
/// {
///     Log.Info("Hello, World!");
///     // Add some elements and modules ...
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class EntryAttribute : Attribute { }