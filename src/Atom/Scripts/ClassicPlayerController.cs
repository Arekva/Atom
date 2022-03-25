using Atom.Engine;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Atom.Game;

public class ClassicPlayerController : Thing
{
    private Camera _camera;

    private Vector3D<f64> _angles = Vector3D<f64>.Zero;
    private const f64 MAX_X_ROT = 89.9D;

    private f64 _eyesHeight = 1.75D;

    private f64 _mouseSpeed = 45.0D;
    
    private f64 _moveSpeed  = 1.42D;
    private f64 _runSpeed   = 3.61D;

    public ClassicPlayerController()
    {
        _camera = new ();
        _camera.Location = new Location(-Vector3D<Double>.Zero);
        _camera.NearPlane = 0.01D;
        _camera.FarPlane = double.PositiveInfinity;
    }

    protected override void Frame()
    {
        f64 delta_time = Time.DeltaTime;
        
        _angles.Y += Mouse.Delta.X * _mouseSpeed * delta_time;
        _angles.X += Mouse.Delta.Y * _mouseSpeed * delta_time;

        _angles.X = Math.Clamp(_angles.X, -MAX_X_ROT, MAX_X_ROT);
        
        _camera.Space.LocalRotation =
            Quaternion<f64>.CreateFromAxisAngle(Vector3D<f64>.UnitY, _angles.Y * AMath.DegToRad) *
            Quaternion<f64>.CreateFromAxisAngle(Vector3D<f64>.UnitX, _angles.X * AMath.DegToRad) ;

        Vector3D<f64> move_forward = _camera.Space.Forward;
        move_forward.Y = 0.0D;
        Vector3D<f64> forward = Vector3D.Normalize(move_forward);
        
        Vector3D<f64> move_right = _camera.Space.Right;
        move_right.Y = 0.0D;
        Vector3D<f64> right   = Vector3D.Normalize(move_right  );
        
        Vector3D<f64> dir = Vector3D<f64>.Zero;
        
        if (Keyboard.IsPressed(Key.W)) dir += forward;
        if (Keyboard.IsPressed(Key.S)) dir -= forward;
        if (Keyboard.IsPressed(Key.A)) dir -= right  ;
        if (Keyboard.IsPressed(Key.D)) dir += right  ;
        
        if (dir != Vector3D<f64>.Zero) dir = Vector3D.Normalize(dir);

        f64 speed = Keyboard.IsPressed(Key.ShiftLeft) ? _mouseSpeed : _runSpeed;
        
        Location += dir * delta_time * speed;
    }

    protected override void Render()
    {
        _camera.Location = Location + Vector3D<f64>.UnitY * _eyesHeight;
    }

    public override void Delete()
    {
        base.Delete();
        
        _camera.Delete();
    }
}