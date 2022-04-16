using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Atom.Engine;

namespace Atom.Game.Config;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class BindAttribute : Attribute
{
    internal static Dictionary<DataType, Dictionary<string, f64>> BASE_UNIT_MAPPERS = new()
    {
        {
            DataType.Angle, new ()
            {
                // angles
                { "rad", 1.0D           }, // rad is the base angle unit
                { "°"  , AMath.DegToRad }, // deg => rad = PI/180
                { "deg", AMath.DegToRad }, // -
            }
        },
        {
            DataType.Length, new()
            {
                // lengths
                { "m" , Units.METRE             }, // metre is the base length unit
                { "km", Units.KILOMETRE         }, // km => m =           1_000
                { "Mm", Units.MEGAMETRE         }, // Mm => m =       1_000_000
                { "AU", Units.ASTRONOMICAL_UNIT }, // AU => m = 149_597_870_700
            }
        },
        {
            DataType.Percentage, new()
            {
                // percentages
                { "%"  , 10E-3  }, // %   => 0..1 = 1 /                   100 
                { "ppm", 10E-6  }, // ppm => 0..1 = 1 /             1_000_000
                { "ppb", 10E-9  }, // ppb => 0..1 = 1 /         1_000_000_000
                { "ppt", 10E-12 }, // ppt => 0..1 = 1 /     1_000_000_000_000
                { "ppq", 10E-15 }, // ppt => 0..1 = 1 / 1_000_000_000_000_000
            }
        },
        {
            DataType.Time, new()
            {
                // time
                { "s"  , 1.0       }, // s is the base time unit
                { "m"  , 60.0D     },
                { "h"  , 3600.0D   },
                { "day", 86164.09D }, // a day is considered as a sideral day
            }
        },
        {
            DataType.Pressure, new()
            {
                // pressure
                { "Pa"  , 1.0         }, // pascal is the base pressure unit
                { "kPa"  , Units.KILO }, // 100 pascals
                { "atm" , 101325.0D   },
            }
        }
    };
    
    public string Point { get; }
    
    public DataType DataType { get; }

    public BindAttribute(string point, DataType dataType = DataType.Unknown) => (Point, DataType) = (point, dataType);
}