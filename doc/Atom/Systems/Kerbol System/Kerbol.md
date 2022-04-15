``` cs
namespace Atom.Game.Config;  
  
public class PlanetConfig  
{  
    [Bind("ID")]  
    public string ID { get; init; }  
        [Bind("Name")]  
    public string Name { get; init; }  
        [Bind("Description")]  
    public string Description { get; init; }  
}
```