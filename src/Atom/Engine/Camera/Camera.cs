using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace Atom.Engine;

public class Camera : Thing
{
    public static Camera Main { get; set; }

    
    private double _manualAspectRatio = 1.0D;

    private CameraData _data;

    
    public Space Space { get; }

    public Projection Projection { get; set; } = Projection.Perspective;

    public double NearPlane { get; set; } = 0.01D;

    public double FarPlane { get; set; } = double.PositiveInfinity;

    public double Width { get; set; } = 10.0D;

    public double Height { get; set; } = 10.0D;
    
    /// <summary> The current field of view of the camera in radians. </summary>
    [EditorExposed("Field of View", typeof(AngleFormatter), ".0D")]
    public double FieldOfView { get; set; } = 70.0D * AMath.DegToRad;

    public double AspectRatio
    {
        get => AspectRatioMode == Atom.Engine.AspectRatio.Automatic ? Video.AspectRatio : _manualAspectRatio;
        set => _manualAspectRatio = value;
    }

    public AspectRatio AspectRatioMode { get; set; }
    
    
    public uint Index { get; }
    

    public Camera()
    {
        Index = 0;
        
        if (Main == null!) Main = this;

        Space = new Space(thing: this);
        
        _data = new CameraData(Index);
        _data.OnFrameUpdate += UpdateFrameData;
        
        
        /*if (_cameraCount >= CameraData.MaxCameraCount)
        {
            ++_cameraCount;
            
            _cameras.Add(this);
        }*/
    }

    public void UpdateFrameData(uint frameIndex)
    {
        UpdateMatrices(frameIndex);
    }

    private void ComputeViewMatrix(out Matrix4X4<float> view)
    {
        if (Main == null!)
        {
            Log.Error("Null main camera !");
            view = default;
            return;
        }
        
        /*Location this_loc = Location;
        Location main_loc = Main.Location;

        Location rel_loc = main_loc - this_loc;*/
        
        Vector3D<double> position = /*rel_loc*/Location.UniversalCoordinates;
        //position.X *= -1.0D; // Invert X position
        //position.Y *= -1.0D;

        Vector3D<double> forward = Space.Forward;
        Vector3D<double> up = Space.Up;
        
        Matrix4X4<double> view_double = Matrix4X4.CreateLookAt(
            cameraPosition: position,
            cameraTarget:   position + forward,
            cameraUpVector: up);

        Matrix4X4<double> world_axis = Matrix4X4<double>.Identity with
        {
            // M11 = -1.0D,
            // M22 = -1.0D,
            // M33 = -1.0D
        };
        
        // view_double.M33 *= -1.0D; // From -Y up to +Y up

        view = (Matrix4X4<float>) Matrix4X4.Multiply(world_axis, view_double);
    }
    
    private void ComputeProjectionMatrix(out Matrix4X4<float> projection)
    {
        if (Projection == Projection.Orthographic)
        {
            projection = (Matrix4X4<float>)Matrix4X4.CreateOrthographic(
                Width,
                Height,
                NearPlane,
                FarPlane
            );
        }
        else
        {
            float f = (float) (1.0 / Math.Tan(FieldOfView / 2.0D));
            float r = (float) (f / AspectRatio);
            float n = (float) NearPlane;
            
            projection = new Matrix4X4<float>(
                r, 0, 0, 0,
                0, f, 0, 0,
                0, 0, 0,-1,
                0, 0, n, 0
            );
        }
    }

    public void UpdateMatrices(uint frameIndex)
    {
        Span<Matrix4X4<float>> vp = stackalloc Matrix4X4<float>[2];
        ComputeViewMatrix(out vp[0]);
        ComputeProjectionMatrix(out vp[1]);
        
        _data.Update(vp, frameIndex);
    }

    public override void Delete()
    {
        base.Delete();

        _data.Dispose();
        if (Main == this) Main = null!;
        //_cameras.Remove(this);
    }
}