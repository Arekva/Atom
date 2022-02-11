using Silk.NET.Maths;

namespace Atom.Engine;

/// <summary> Defines an on-rail orbit </summary>
public class Orbit : ITrajectory
{
    // Around what this orbits
    private ICelestialBody _referenceBody;

    private double _semiMajorAxis;            // sma
    private double _eccentricity;             // e
    private double _inclination;              // i      -- x rot (after lan)
    private double _longitudeOfAscendingNode; // asc    -- y rot (before inclination)
    private double _argumentOfPeriapsis;      // y rot (after inclination)
    private double _trueAnomaly;
    
    private double _cosw, _sinw, _cosO, _sinO, _cosi, _sini;

    public Orbit(ICelestialBody referenceBody,
        double semiMajorAxis,
        double eccentricity = 0.0D,
        double inclination = 0.0D,
        double longitudeOfAscendingNode = 0.0D,
        double argumentOfPeriapsis = 0.0D, 
        double trueAnomaly = 0.0D)
    {
        _referenceBody = referenceBody ?? throw new ArgumentNullException(nameof(referenceBody));
        
        _semiMajorAxis = semiMajorAxis;
        _eccentricity = eccentricity;
        _inclination = inclination;
        _longitudeOfAscendingNode = longitudeOfAscendingNode;
        _argumentOfPeriapsis = argumentOfPeriapsis;
        _trueAnomaly = trueAnomaly;
        
        PrecalculateTrigonometry();
    }
    
    //Gets relative position to the orbited body
    //https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf
    public Vector3D<double> GetRelativePosition(double universalTime)
    {
        double sma = _semiMajorAxis;
        double e = _eccentricity;
        
        // M(t) (Mean Anomaly)
        // original \/
        //double v = 2 * Atan2DoubleArgument(Math.Sqrt(1 + e) * Math.Sin(E/2), Math.Sqrt(1 - e) * Math.Cos(E/2));
        double M = _trueAnomaly + universalTime * Math.Sqrt((Astrophysics.G * _referenceBody.Mass)/(sma * sma * sma));

        M = NormalizeAngle(M);

        const int NEWTON_RAPHSON_METHOD_ITERATIONS = 3;
        
        // E(t) (I think it's something related with eccentricity?)
        double E = M;
        for(int i = 0; i < NEWTON_RAPHSON_METHOD_ITERATIONS; i++)
        {
            E -= (E - e * Math.Sin(E) - M)/(1 - e * Math.Cos(E));
        }

        // v(t) (True Anomaly)
        double v = 2 * Math.Atan2(Math.Sqrt(1 + e) * Math.Sin(E/2), Math.Sqrt(1 - e) * Math.Cos(E/2));

        // r_c(t) (Distance to center object)
        double rc = sma * (1 - e * Math.Cos(E));
		
        // o(t) Position in plane
        double oX = rc * (float)Math.Cos(v);
        double oZ = rc * (float)Math.Sin(v);

        // Obtaining final position
        double rX = oX * (_cosw * _cosO - _sinw * _cosi * _sinO) - oZ * (_sinw * _cosO + _cosw * _cosi * _sinO);
        double rZ = oX * (_cosw * _sinO + _sinw * _cosi * _cosO) + oZ * (_cosw * _cosi * _cosO - _sinw * _sinO);
        double rY = oX * (_sinw * _sini) + oZ * (_cosw * _sini);

        return new Vector3D<double>(rX, rY, rZ);
    }
    
    private void PrecalculateTrigonometry() {
        _cosw = Math.Cos(_argumentOfPeriapsis);
        _sinw = Math.Sin(_argumentOfPeriapsis);
        _cosO = Math.Cos(_longitudeOfAscendingNode);
        _sinO = Math.Sin(_longitudeOfAscendingNode);
        _cosi = Math.Cos(_inclination);
        _sini = Math.Sin(_inclination);
    }

    public static double NormalizeAngle(double input) {
        int n = (int)Math.Floor(input/(2*Math.PI));
        return input - n * (2*Math.PI);
    }

    public Matrix4X4<double> TransformMatrix => Matrix4X4.CreateFromQuaternion(
        Quaternion<double>.CreateFromYawPitchRoll(
            pitch: -_inclination,               // X axis
            yaw: -_longitudeOfAscendingNode,    // Y axis
            roll: 0                             // Z axis
        ));

    public double Period
    {
        get
        {
            double sma = _semiMajorAxis;
            
            return 2.0D * Math.PI * Math.Sqrt((sma * sma * sma)/(Astrophysics.G*_referenceBody.Mass));
        }
    }
}