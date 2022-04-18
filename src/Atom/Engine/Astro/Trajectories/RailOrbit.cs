using System.Runtime.CompilerServices;
using Atom.Game.Config;
using Silk.NET.Maths;

namespace Atom.Engine.Astro;

/// <summary> Defines an on-rail orbit </summary>
public class RailOrbit : ITrajectory
{
    private f64 _semiMajorAxis;            // sma
    private f64 _eccentricity;             // e
    private f64 _inclination;              // i      -- x rot (after lan)
    private f64 _longitudeOfAscendingNode; // asc    -- y rot (before inclination)
    private f64 _argumentOfPeriapsis;      //        -- y rot (after inclination)
    private f64 _trueAnomaly;
    
    private f64 _cosw, _sinw, _cosO, _sinO, _cosi, _sini;

    private f64 _referenceMass;
    private f64 _period;
    
    
    
    
    public f64 Period => _period;
    
    public RailOrbit(ICelestialBody body, PlanetConfig.OrbitConfig cfg)
    {
        _semiMajorAxis            = cfg.SemiMajorAxis                 ;
        _eccentricity             = cfg.Eccentricity                  ;
        _inclination              = cfg.Inclination                   ;
        _longitudeOfAscendingNode = cfg.LongitudeOfAscendingNode      ;
        _argumentOfPeriapsis      = cfg.ArgumentOfPeriapsis           ;
        _trueAnomaly              = MeanToTrueAnomaly(cfg.MeanAnomaly);
        
        PrecalculateTrigonometry();

        PlanetUpdate(body);
    }
    
    public RailOrbit(ICelestialBody body   ,
        f64 semiMajorAxis                  ,
        f64 eccentricity             = 0.0D,
        f64 inclination              = 0.0D,
        f64 longitudeOfAscendingNode = 0.0D,
        f64 argumentOfPeriapsis      = 0.0D, 
        f64 trueAnomaly              = 0.0D)
    {
        _semiMajorAxis            = semiMajorAxis           ;
        _eccentricity             = eccentricity            ;
        _inclination              = inclination             ;
        _longitudeOfAscendingNode = longitudeOfAscendingNode;
        _argumentOfPeriapsis      = argumentOfPeriapsis     ;
        _trueAnomaly              = trueAnomaly             ;
        
        PrecalculateTrigonometry();

        PlanetUpdate(body);
    }

    internal void PlanetUpdate(ICelestialBody body)
    {
        // always got a reference body: rail orbit always need to have a reference body.
        _referenceMass = body.Reference!.Mass;
        ComputePeriod();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ComputePeriod()
    {
        f64 sma = _semiMajorAxis;
            
        _period = Math.Tau * Math.Sqrt((sma * sma * sma)/(Astrophysics.G*_referenceMass));
    }

    // Gets relative position to the orbited body
    // https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf
    public Vector3D<f64> GetRelativePosition(f64 universalTime)
    {
        f64 sma = _semiMajorAxis;
        f64 e = _eccentricity;
        
        // M(t) (Mean Anomaly)
        // original \/
        //double v = 2 * Atan2DoubleArgument(Math.Sqrt(1 + e) * Math.Sin(E/2), Math.Sqrt(1 - e) * Math.Cos(E/2));
        f64 M = _trueAnomaly + universalTime * Math.Sqrt((Astrophysics.G * _referenceMass)/(sma * sma * sma));

        M = NormalizeAngle(M);

        const u32 NEWTON_RAPHSON_METHOD_ITERATIONS = 3U;
        
        // E(t) (I think it's something related with eccentricity?)
        f64 E = M;
        for(u32 i = 0; i < NEWTON_RAPHSON_METHOD_ITERATIONS; i++)
        {
            E -= (E - e * Math.Sin(E) - M)/(1.0D - e * Math.Cos(E));
        }

        // v(t) (True Anomaly)
        f64 v = 2.0D * Math.Atan2(Math.Sqrt(1.0D + e) * Math.Sin(E/2.0D), Math.Sqrt(1.0D- e) * Math.Cos(E/2.0D));

        // r_c(t) (Distance to center object)
        f64 r_c = sma * (1 - e * Math.Cos(E));
		
        // o(t) Position in plane
        f64 o_x = r_c * Math.Cos(v);
        f64 o_z = r_c * Math.Sin(v);

        // Obtaining final position
        f64 r_x = o_x * (_cosw * _cosO - _sinw * _cosi * _sinO) - o_z * (_sinw * _cosO + _cosw * _cosi * _sinO);
        f64 r_z = o_x * (_cosw * _sinO + _sinw * _cosi * _cosO) + o_z * (_cosw * _cosi * _cosO - _sinw * _sinO);
        f64 r_y = o_x * (_sinw * _sini) + o_z * (_cosw * _sini);

        return new Vector3D<f64>(r_x, r_y, r_z);
    }
    
    private void PrecalculateTrigonometry() {
        _cosw = Math.Cos(_argumentOfPeriapsis);
        _sinw = Math.Sin(_argumentOfPeriapsis);
        _cosO = Math.Cos(_longitudeOfAscendingNode);
        _sinO = Math.Sin(_longitudeOfAscendingNode);
        _cosi = Math.Cos(_inclination);
        _sini = Math.Sin(_inclination);
    }

    public static f64 NormalizeAngle(f64 input) {
        i32 n = (i32)Math.Floor(input/Math.Tau);
        return input - n * Math.Tau;
    }

    public Matrix4X4<f64> TransformMatrix => Matrix4X4.CreateFromQuaternion(
        Quaternion<f64>.CreateFromYawPitchRoll(
            pitch: -_inclination,                 // X axis
            yaw  : -_longitudeOfAscendingNode,    // Y axis
            roll : 0                              // Z axis
        ));


    public f64 MeanToTrueAnomaly(f64 meanAnomaly)
    {
        double M = meanAnomaly;
        double e = _eccentricity;
        
        // true anomaly
        double v = 
            M + 
            (2*e - 1/4.0D*(e*e*e)) *
            Math.Sin(M) + 5/4.0D*(e*e) * 
            Math.Sin(2*M) + 13/12.0D*(e*e*e) *
            Math.Sin(3*M) /* + O(e*e*e*e)*/;

        return v;
    }
}