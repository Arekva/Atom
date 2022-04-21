// ReSharper disable CompareOfFloatsByEqualityOperator

using System.Runtime.CompilerServices;

using Silk.NET.Maths;



namespace Atom.Engine;



public class OrthographicProjection : IProjection
{
    private       Matrix4X4<f64> _projection       ;
    
    private       f64            _near             ;
    private       f64            _far              ;
    
    // rect based
    private       f64            _width            ;
    private       f64            _height           ;
    
    // aspect ratio based
    private       f64            _size             ;
    private       f64            _aspectRatio      ;

    private       bool           _aspectRatioDriven;
    private       bool           _autoBake         ;



    public ref readonly Matrix4X4<f64> ProjectionMatrix => ref _projection;

    public f64 Near
    {
        get => _near;
        set
        {
            if (value == _near) return;
            
            _near = value;
            DoAutoBake();
        }
    }

    public f64 Far
    {
        get => _far;
        set
        {
            if (value == _far) return;
            
            _far = value;
            DoAutoBake();
        }
    }
    
    public f64 Width
    {
        get => _width;
        set
        {
            if (value == _width) return;
            
            _width = value;
            if (!_aspectRatioDriven) DoAutoBake();
        }
    }

    public f64 Height
    {
        get => _height;
        set
        {
            if (value == _height) return;
            
            _height = value;
            if (!_aspectRatioDriven) DoAutoBake();
        }
    }
    
    public f64 Size
    {
        get => _size;
        set
        {
            if (value == _size) return;
            
            _size = value;
            if (_aspectRatioDriven) DoAutoBake();
        }
    }

    public f64 AspectRatio
    {
        get => _aspectRatio;
        set
        {
            if (value == _aspectRatio) return;
            
            _aspectRatio = value;
            if (_aspectRatioDriven) DoAutoBake();
        }
    }

    public bool AspectRatioDriven
    {
        get => _aspectRatioDriven;
        set
        {
            if (value == _aspectRatioDriven) return;
            
            _aspectRatioDriven = value;
            DoAutoBake();
        }
    }
    
    public bool AutoBake
    {
        get => _autoBake;
        set
        {
            if (_autoBake == value) return;
            
            _autoBake = value;
            DoAutoBake();
        }
    }
    
    public event IProjection.ProjectionCallback? OnProjectionMatrixChange;
    
    

    public OrthographicProjection(
        f64 near = -1.0D, f64 far = 1.0D, f64 width = 1.0D, f64 height = 1.0D)
    {
        _near   = near  ;
        _far    = far   ;
        _width  = width ;
        _height = height;
    }

    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DoAutoBake()
    {
        if (_autoBake) Bake();
    }
    
    

    public ref readonly Matrix4X4<f64> Bake()
    {
        f64 w;
        f64 h;
        if (_aspectRatioDriven)
        {
            w = _size * _aspectRatio;
            h = _size               ;
        }
        else
        {
            w = 2.0D / _width ;
            h = 2.0D / _height;
        }
        
        f64 nf = _near - _far;
        f64 n  = _near / nf  ;
        f64 f  = 1.0D / nf   ;
        
        _projection = new Matrix4X4<f64>(
            w, 0, 0, 0,
            0, h, 0, 0,
            0, 0, f, 0,
            0, 0, n, 1
        );
        
        OnProjectionMatrixChange?.Invoke(in _projection);

        return ref ProjectionMatrix;
    }
}