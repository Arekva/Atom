using System.Runtime.CompilerServices;

using Silk.NET.Maths;
// ReSharper disable CompareOfFloatsByEqualityOperator



namespace Atom.Engine;



public class PerspectiveProjection : IProjection
{
    public  const f64            MIN_NEAR     = 0.001D               ;
    public  const f64            FAR          = f64.PositiveInfinity ;
 
    public  const f64            MIN_FOV      = f64.Epsilon          ;
    public  const f64            MAX_FOV      = Math.PI - f64.Epsilon;
    
    public  const f64            MIN_ASPECT   = f64.Epsilon          ;
    

    
    private       Matrix4X4<f64> _projection                         ;

    private       f64            _near                               ;
    private       f64            _fieldOfView                        ;
    private       f64            _aspectRatio                        ;

    private       bool           _autoBake                           ;
    
    

    public ref readonly Matrix4X4<f64> ProjectionMatrix => ref _projection;

    public f64 Near
    {
        get => _near;
        set
        {
            if (value == _near) return;
            
            if (value < MIN_NEAR)
            {
                Log.Warning($"Clamping near value {value} to {MIN_NEAR} in order to match camera's perspective min near clip value.");
                _near = MIN_NEAR;
            }
            else
            {
                _near = value;
            }

            DoAutoBake();
        }
    }

    public f64 AspectRatio
    {
        get => _aspectRatio;
        set
        {
            if (_aspectRatio < MIN_ASPECT)
            {
                Log.Warning($"Clamping aspect ratio {value} to epsilon in order to avoid NaNs.");
                _aspectRatio = MIN_ASPECT;
            }
            
            if (value == _aspectRatio) return;
            
            _aspectRatio = value;
            DoAutoBake();
        }
    }

    public f64 FieldOfView
    {
        get => _fieldOfView;
        set
        {
            if (value == _fieldOfView) return;
            
            if (value is < MIN_FOV or > MAX_FOV)
            {
                Log.Warning($"Clamping FOV value {value} in the ]0..PI[ range for camera's perspective.");

                value = Math.Clamp(value, MIN_FOV, MAX_FOV);
            }
            
            _fieldOfView = value;
            
            DoAutoBake();
        }
    }
    
    public bool AutoBake
    {
        get => _autoBake;
        set
        {
            _autoBake = value;
            DoAutoBake();
        }
    }


    public event IProjection.ProjectionCallback? OnProjectionMatrixChange;


    public PerspectiveProjection(f64 fieldOfView = 70.0D, f64 near = 0.01D, f64 aspectRatio = 1.0D, bool autoBake = true)
    {
        _fieldOfView = fieldOfView;
        _near        = near       ;
        _aspectRatio = aspectRatio;
        _autoBake    = autoBake   ;
        
        DoAutoBake();
    }
    
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DoAutoBake()
    {
        if (_autoBake) Bake();
    }

    
    
    public ref readonly Matrix4X4<f64> Bake()
    {
        f64 f = 1.0D / Math.Tan(_fieldOfView / 2.0D);
        f64 r = f / _aspectRatio;
        f64 n = _near;
        
        _projection = new Matrix4X4<f64>(
            r, 0, 0, 0,
            0, f, 0, 0,
            0, 0, 0,-1, // infinite far clip
            0, 0, n, 0  // only near clip matters
        );
        
        OnProjectionMatrixChange?.Invoke(in _projection);

        return ref ProjectionMatrix;
    }
}