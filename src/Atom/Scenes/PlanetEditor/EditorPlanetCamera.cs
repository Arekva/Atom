using Atom.Engine;
using Silk.NET.Maths;

namespace Atom.Game.PlanetEditor;

public class EditorPlanetCamera : AtomObject
{
    private Camera _camera;
    
    private Vector3D<f64> _angles = new (0.0D, 0.0D, 0.0D);
    
    const f64 MOUSE_SPEED = 45.0D;
    
    
    private f64 _normalizedDistance = 10.0D;

    public f64 BodyRadius { get; set; } = 1.0D;

    public EditorPlanetCamera()
    {
        _camera = Camera.World = new Camera(identifier: "editor_planet_viewport");
        _camera.Perspective.Near = 0.001D;

        MakeReady();
    }

    public void Rotate(Vector2D<f64> angles)
    {
        _angles.X = angles.X;
        _angles.Y = angles.Y;

        UpdateSpace();
    }

    private void UpdateSpace()
    {
        Quaternion<f64> rotation = 
            Quaternion<f64>.CreateFromAxisAngle(Vector3D<f64>.UnitY, _angles.Y * AMath.DEG_TO_RAD) *
            Quaternion<f64>.CreateFromAxisAngle(Vector3D<f64>.UnitX, _angles.X * AMath.DEG_TO_RAD) ;
        
        _camera.Space.LocalRotation = -rotation;
        _camera.Space.LocalPosition = rotation.Multiply(-Vector3D<Double>.UnitZ) * _normalizedDistance * BodyRadius;
    }
    
    protected internal override void Frame()
    {
    }

    public override void Delete()
    {
        base.Delete();
        
        _camera.Delete();
    }
}