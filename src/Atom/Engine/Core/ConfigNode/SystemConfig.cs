using System.Reflection;
using Atom.Engine;

namespace Atom.Game.Config;

[Bind("System")]
public class SystemConfig
{
    public override String ToString() => $"{Name} [{ID}]";


    [Bind("ID")] public string ID { get; set; }
    
    [Bind("Name")] public string Name { get; set; }
    
    [Bind("Description")] public string Description { get; set; }
    
    
    [Bind("Location", DataType.Length)] public Location Location { get; set; }

}




