using System.Reflection;

namespace Atom.Game.Config;

[Bind("Planet")]
public class PlanetConfig
{
    public override String ToString() => $"{Name} [{ID}]";


    [Bind("ID")] public string ID { get; set; }
    
    [Bind("Name")] public string Name { get; set; }
    
    [Bind("Description")] public string Description { get; set; }
    
    [Bind("Generation")] public GenerationConfig Generation { get; set; }
    
    [Bind("Rotation")] public RotationConfig Rotation { get; set; }
    
    [Bind("Orbit")] public OrbitConfig Orbit { get; set; }
    
    
    public class GenerationConfig
    {
        [Bind("Generator")]
        public string GeneratorPath { get; set; }
    
        [Bind("Parameters")]
        public IGenerator GeneratorParameters { get; set; } = new Generator();
    }
    
    public class RotationConfig
    {
        [Bind("Inclination")]
        public InclinationConfig Inclination { get; set; }
        
        [Bind("Day", DataType.Time)]
        public double Day { get; set; }


        public class InclinationConfig
        {
            [Bind("Obliquity", DataType.Angle)]
            public double Obliquity { get; set; }
    
            [Bind("RightAscension", DataType.Angle)] // reinterpreted later
            public double RightAscension { get; set; }
        }
    }

    public class OrbitConfig
    {
        [Bind("Parent")]
        public string Parent { get; set; }
        
        [Bind("SemiMajorAxis", DataType.Length)]
        public double SemiMajorAxis { get; set; }           
        [Bind("Eccentricity")]
        public double Eccentricity { get; set; }            
        [Bind("Inclination", DataType.Angle)]
        public double Inclination { get; set; }             
        [Bind("LongitudeOfAscendingNode", DataType.Angle)]
        public double LongitudeOfAscendingNode { get; set; }
        [Bind("ArgumentOfPeriapsis", DataType.Angle)]
        public double ArgumentOfPeriapsis { get; set; }     
        [Bind("MeanAnomaly", DataType.Angle)]
        public double MeanAnomaly { get; set; }             
    }
}




