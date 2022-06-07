using Atom.Engine;
using Silk.NET.Maths;

namespace Atom.Game.VoxelTest;

public class CameraRot : AtomObject
{
    private Camera _camera;
    
    private Vector3D<f64> _angles = new (0.0D, 0.0D, 0.0D);
    private const f64 MAX_X_ROT = 89.9D;
    
    private f64 _mouseSpeed = 45.0D;

    public CameraRot()
    {
        _camera = Camera.World!;
        
        MakeReady();
    }
    
    protected internal override void Frame()
    {
        f64 delta_time = Time.DeltaTime;
        
        _angles.Y += Mouse.Delta.X * _mouseSpeed * delta_time;
        _angles.X += Mouse.Delta.Y * _mouseSpeed * delta_time;

        _angles.X = Math.Clamp(_angles.X, -MAX_X_ROT, MAX_X_ROT);
        
        _camera.Space.LocalRotation =
            Quaternion<f64>.CreateFromAxisAngle(Vector3D<f64>.UnitY, _angles.Y * AMath.DegToRad) *
            Quaternion<f64>.CreateFromAxisAngle(Vector3D<f64>.UnitX, _angles.X * AMath.DegToRad) ;
    }
}