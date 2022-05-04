using Silk.NET.Maths;

namespace Atom.Engine;

public class Space : AtomObject
{
    private Thing _thing;
    
    private Space?      _parent   ;
    private List<Space> _subspaces;

    private Vector3D<f64>   _localPosition   = Vector3D<f64>.Zero      ;
    private Quaternion<f64> _localRotation   = Quaternion<f64>.Identity;
    private Vector3D<f64>   _localScale      = Vector3D<f64>.One       ;
    
    
    // Dictionary<CameraIndex, Matrix4X4<f64>[FrameIndex]>
    private Dictionary<u32, Matrix4X4<f64>[]> _renderMatrices;

    
    public Thing Thing => _thing;

    public Space? Parent => _parent;
    public IEnumerable<Space> Subspaces => _subspaces;

    public ref Matrix4X4<f64> this[u32 cameraIndex, u32 frameIndex]
    {
        get
        {
            const u32 MAX_FRAME_COUNT = 3;
            const u32 MAX_CAMERA_COUNT = 1024;
            
            if (frameIndex > MAX_FRAME_COUNT)
            {
                throw new IndexOutOfRangeException($"Frame index {frameIndex} is higher than {MAX_FRAME_COUNT}.");
            }
            if (cameraIndex > MAX_CAMERA_COUNT)
            {
                throw new IndexOutOfRangeException($"Camera index {frameIndex} is higher than {MAX_FRAME_COUNT}.");
            }
            
            if (_renderMatrices.TryGetValue(cameraIndex, out Matrix4X4<f64>[]? frame_array))
            {
                return ref frame_array[frameIndex];
            }
            else
            {
                Matrix4X4<f64>[] frames = new Matrix4X4<f64>[3];
                _renderMatrices.Add(cameraIndex, frames);
                return ref frames[frameIndex];
            }
        }
    }
    
    

    public Space(Thing thing, string? name = "Space") : base(name)
    {
        _thing = thing;

        _parent = null;
        _subspaces = new List<Space>();
        
        _thing.AddSpace(this);

        _renderMatrices = new Dictionary<u32, Matrix4X4<f64>[]>(capacity: 15);

        MakeReady();
    }

    public Space(Space parent, string? name = "Space") : base(name)
    {
        _thing = parent._thing ?? throw new ArgumentNullException(nameof(parent), "An upper space must be set");

        _parent = parent;
        _subspaces = new List<Space>();
        
        _parent._subspaces.Add(this);
        
        _renderMatrices = new Dictionary<u32, Matrix4X4<f64>[]>(capacity: 15);

        MakeReady();
    }

    public override void Delete()
    {
        base.Delete();
        
        _parent?._subspaces.Remove(this);
        _thing.RemoveSpace(this);
        _thing = null!;

        _renderMatrices = null!;
    }


    public ref Vector3D<f64> LocalPosition   => ref _localPosition;
    public ref Quaternion<f64> LocalRotation => ref _localRotation;
    public ref Vector3D<f64> LocalScale      => ref _localScale;
    
    public Vector3D<f64> Position
    {
        get => _parent is null 
            ? _localPosition 
            : (_localPosition * _parent.Scale).RotateAround(Vector3D<f64>.Zero, _parent.Rotation) + _parent.Position;
        set => _localPosition = _parent is null 
            ? value 
            : (value - _parent.Position).RotateAround(Vector3D<f64>.Zero, _parent.Rotation) * (Vector3D<f64>.One / _parent.Scale);
    }
    
    public Quaternion<f64> Rotation
    {
        get => _parent is null 
            ? _localRotation 
            : _parent.Rotation * _localRotation;
        set => _localRotation = _parent is null 
            ? value 
            : Quaternion<f64>.Inverse(_parent.Rotation) * Quaternion<f64>.Normalize(value);
    }
    
    public Vector3D<f64> Scale
    {
        get => _parent is null 
            ? _localScale 
            : _parent.Scale * _localScale;
        set => _localScale = _parent is null 
            ? value 
            : value / _parent.Scale;
    }

    public Location Location => _thing.Location + new Location(coordinates: Position);
    
    public Vector3D<f64> Left
    {
        get => Rotation.Multiply(-Vector3D<f64>.UnitX);
        set => Rotation = SilkExtender.FromCross(-Vector3D<f64>.UnitX, Vector3D.Normalize(value));
    }
    
    public Vector3D<f64> Right
    {
        get => Rotation.Multiply(Vector3D<f64>.UnitX);
        set => Rotation = SilkExtender.FromCross(Vector3D<f64>.UnitX, Vector3D.Normalize(value));
    }
    
    public Vector3D<f64> Down
    {
        get => Rotation.Multiply(-Vector3D<f64>.UnitY);
        set => Rotation = SilkExtender.FromCross(-Vector3D<f64>.UnitY, Vector3D.Normalize(value));
    }
    
    public Vector3D<f64> Up
    {
        get => Rotation.Multiply(Vector3D<f64>.UnitY);
        set => Rotation = SilkExtender.FromCross(Vector3D<f64>.UnitY, Vector3D.Normalize(value));
    }
    
    public Vector3D<f64> Backward
    {
        get => Rotation.Multiply(-Vector3D<f64>.UnitZ);
        set => Rotation = SilkExtender.FromCross(-Vector3D<f64>.UnitZ, Vector3D.Normalize(value));
    }
    
    public Vector3D<f64> Forward
    {
        get => Rotation.Multiply(Vector3D<f64>.UnitZ);
        set => Rotation = SilkExtender.FromCross(Vector3D<f64>.UnitZ, Vector3D.Normalize(value));
        
    }
    
    public Matrix4X4<f64> LocalMatrix => Matrix4X4.Multiply(Matrix4X4.Multiply(
        Matrix4X4.CreateFromQuaternion(_localRotation), 
        Matrix4X4.CreateScale(_localScale)), 
        Matrix4X4.CreateTranslation(_localPosition));
    
    public Matrix4X4<f64> Matrix => Matrix4X4.Multiply(Matrix4X4.Multiply(
        Matrix4X4.CreateFromQuaternion(Rotation), 
        Matrix4X4.CreateScale(Scale)), 
        Matrix4X4.CreateTranslation(Position));
    
    public Matrix4X4<f64> UniverseMatrix => Matrix4X4.Multiply(Matrix4X4.Multiply(
        Matrix4X4.CreateFromQuaternion(Rotation), 
        Matrix4X4.CreateScale(Scale)), 
        Matrix4X4.CreateTranslation(Location.Position));

    
    // (((sca * rot) * pos) * identity)
    public Matrix4X4<f64> RelativeMatrix(Location location) =>
        Matrix4X4.Multiply(
        Matrix4X4.Multiply(
        Matrix4X4.Multiply(Matrix4X4.CreateScale(Scale), Matrix4X4.CreateFromQuaternion(Rotation)), Matrix4X4.CreateTranslation((Location - location).Position)), Matrix4X4<f64>.Identity);
        
        /*Matrix4X4.Multiply(Matrix4X4.Multiply(
        Matrix4X4.CreateFromQuaternion(Rotation), 
        Matrix4X4.CreateScale(Scale)), 
        Matrix4X4.CreateTranslation((Location - location).Position));*/
    

    public Plane Plane => new (Right, Up, Forward);
}