using System.Collections.Concurrent;

using Silk.NET.Maths;



namespace Atom.Engine;



public partial class Camera : Thing
{
    public const u32                MAX_CAMERA_COUNT    = 1023U       ;
    public const i32                UNINITIALIZED_INDEX = i32.MaxValue;
    
    private static ConcurrentDictionary<string, Camera> _cameras = new(concurrencyLevel: 6, capacity: (i32)MAX_CAMERA_COUNT);


    
    private string                 _identifier         ;
    private i32                    _index              ;
    private Space                  _space              ;
    
    private Resolution             _resolutionMode     ;
    private Projection             _projectionMode     ;
    
    private ScreenResolution       _automaticResolution;
    private ScreenResolution       _manualResolution   ;

    private PerspectiveProjection  _perspective        ;
    private OrthographicProjection _orthographic       ;



    public string Identifier => _identifier;

    public i32 Index => _index;
    
    public Space Space => _space;

    public Projection ProjectionMode
    {
        get => _projectionMode;
        set => _projectionMode = value;
    }

    public Resolution ResolutionMode
    {
        get => _resolutionMode;
        set
        {
            if (value == _resolutionMode) return;

            _resolutionMode = value;
            ScreenResolution resolution = Resolution;

            Perspective.AspectRatio = (f64)resolution.X / resolution.Y;
        }
    }

    public ScreenResolution Resolution
    {
        get => _resolutionMode == Atom.Engine.Resolution.Automatic ? _automaticResolution : _manualResolution;
        set
        {
            ScreenResolution previous_resolution = _manualResolution;
            _manualResolution = value;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_resolutionMode == Atom.Engine.Resolution.Manual)
            {
                if (previous_resolution != value)
                {
                    SetProjectionsAspectRatio();
                }
            }
            else
            {
                Log.Info("A manual resolution is set but the camera resolution mode is set on automatic.");
            }
        }
    }

    public f64 AspectRatio
    {
        get => _projectionMode == Projection.Perspective ? _perspective.AspectRatio : _orthographic.AspectRatio;
        set => SetProjectionsAspectRatio(value);
    }

    public PerspectiveProjection  Perspective  =>  _perspective;

    public OrthographicProjection Orthographic => _orthographic;

    public ref readonly Matrix4X4<f64> ProjectionMatrix => ref _projectionMode == Projection.Perspective
        ? ref _perspective .ProjectionMatrix
        : ref _orthographic.ProjectionMatrix;
    


    public Camera(
        string? identifier = null, Space? parent = null,
        Projection projectionMode = Projection.Perspective,
        Resolution resolutionMode = Atom.Engine.Resolution.Automatic,
        Vector2D<u32>? resolution = null) : base(name: identifier ?? "Camera")
    {
        BorrowIndex();
        
        // if there is a parent, use it, otherwise create a space.
        _space = parent == null ? new Space(thing: this) : new Space(parent: parent);

        if (!_cameras.TryAdd(identifier ?? GUID.ToString(), this))
        {
            Delete();
            throw new Exception("A camera with the same identifier already is existing.");
        }

        _perspective  = new PerspectiveProjection() ;
        _orthographic = new OrthographicProjection();

        _projectionMode = projectionMode;

        // 1024x1024 is the default camera resolution for manual mode
        Vector2D<u32> default_manual_resolution = new(1024);

        _resolutionMode   = resolutionMode;
        _manualResolution = resolutionMode == Atom.Engine.Resolution.Manual
            ? resolution ?? default_manual_resolution
            : default_manual_resolution;

        _automaticResolution = Video.Resolution;


        Orthographic.AspectRatioDriven = resolutionMode != Atom.Engine.Resolution.Manual;

        SetProjectionsAspectRatio(AspectRatio);

        // ReSharper disable once VariableHidesOuterVariable
        Video.OnResolutionChanged += resolution =>
        {
            if (_automaticResolution == resolution) return;

            _automaticResolution = resolution;

            if (_resolutionMode == Atom.Engine.Resolution.Manual) return;

            SetProjectionsAspectRatio();
        };
    }

    public override void Delete()
    {
        base.Delete()  ;

        RetrieveIndex();
        _space.Delete();
        _cameras.TryRemove(_identifier, out _);
    }
    
    
    
    public Matrix4X4<f64> ViewMatrix(Camera? reference = null)
    {
        // if no reference, consider this as the origin
        Location location = Space.Location - reference?.Space.Location ?? Location.Origin;

        Vector3D<f64> position = location.Position;

        Vector3D<f64> forward = Space.Forward;
        Vector3D<f64> up      = Space.Up     ;

        Matrix4X4<f64> view_double = Matrix4X4.CreateLookAt(
            cameraPosition: position          ,
            cameraTarget  : position + forward,
            cameraUpVector: up
        );

        return view_double;
    }
    
    public void CopyMatrices(Span<Matrix4X4<f32>> target, Camera? reference = null)
    {
        if (target.Length < 2)
        {
            throw new ArgumentException("Target span must have at least a count of 2.", nameof(target));
        }
        
        target[0] = (Matrix4X4<f32>)ViewMatrix(reference);
        target[1] = (Matrix4X4<f32>)ProjectionMatrix     ;
    }
    
    public void CopyMatrices(Span<Matrix4X4<f64>> target, Camera? reference = null)
    {
        if (target.Length < 2)
        {
            throw new ArgumentException("Target span must have at least a count of 2.", nameof(target));
        }
        
        target[0] = ViewMatrix(reference);
        target[1] = ProjectionMatrix     ;
    }
    
    

    private void SetProjectionsAspectRatio()
    {
        ScreenResolution resolution = Resolution;
        f64 aspect_ratio = (f64)resolution.X / resolution.Y;
        SetProjectionsAspectRatio(aspect_ratio);
    }

    private void SetProjectionsAspectRatio(f64 aspectRatio)
    {
        if (_resolutionMode == Atom.Engine.Resolution.Manual)
        {
            Log.Info("Cannot manually set camera's aspect ratio, it is in automatic mode.");
            return;
        }

        Perspective .AspectRatio = aspectRatio;
        Orthographic.AspectRatio = aspectRatio;
    }
}