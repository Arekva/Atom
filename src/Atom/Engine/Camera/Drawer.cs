using Silk.NET.Maths;

namespace Atom.Engine;

public class Drawer : IDisposable
{
    public struct DrawRange
    {
        public i32 CallIndex, Start, Length;
    }

    public struct MeshBounding
    {
        public Vector3D<f64> Position;
        public f64 Bounding;
        public i32 CallIndex; // fuck C# for using 32bit signed ints everywhere for anything.....
    }
    
    


    public delegate void DrawCommand(Camera camera, CommandRecorder.RenderPassRecorder renderPass,
        ReadOnlySpan<DrawRange> ranges,
        Vector2D<u32> resolution, u32 frameIndex);

    public delegate ReadOnlySpan<MeshBounding> GetMeshesBounds();
    
    
    
    private readonly Guid _guid;

    private DrawCommand _command;

    private GetMeshesBounds _bounds;

    private MeshBounding[] _meshes;



    // ReSharper disable once InconsistentNaming
    public Guid GUID => _guid;

    public IEnumerable<Camera> DrawBy => Camera.Cameras.Where(cam => cam.Drawers.Contains(this));

    public u64 TotalMeshCount;
    
    

    public Drawer(DrawCommand command, GetMeshesBounds bounds, IEnumerable<Camera>? cameras = null)
    {
        _guid = new Guid();
        _command = command;
        _bounds = bounds;

        if (cameras == null) return;
        
        foreach (Camera camera in cameras)
        {
            AssignToCamera(camera);
        }
    }
    
    public Drawer(DrawCommand command, GetMeshesBounds bounds, Camera camera)
    {
        _guid = new Guid();
        _command = command;
        _bounds = bounds;

       AssignToCamera(camera);
    }

    public override bool Equals(object? obj) => obj is Drawer drawer && drawer._guid == _guid;

    public override int GetHashCode() => _guid.GetHashCode();



    public void Draw(Camera camera, CommandRecorder.RenderPassRecorder renderPass,
        ReadOnlySpan<DrawRange> ranges,
        Vector2D<u32> resolution, u32 frameIndex) => _command(camera, renderPass, ranges, resolution, frameIndex);
    public ReadOnlySpan<MeshBounding> GetMeshes() => _bounds();
    

    public void AssignToCamera(Camera camera) => camera.AddDrawer(this);

    public void UnAssignCamera(Camera camera) => camera.RemoveDrawer(this);

    
    private void Destroy()
    {
        foreach (Camera camera in Camera.Cameras)
        {
            camera.RemoveDrawer(this);
        }
        GC.SuppressFinalize(this);
    }

    public void Dispose() => Destroy();

    ~Drawer() => Dispose();
}