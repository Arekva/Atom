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
    
    


    public delegate void DrawCommand(
        Camera camera, 
        CommandRecorder.RenderPassRecorder renderPass,
        ReadOnlySpan<DrawRange> ranges
    );

    public delegate ReadOnlySpan<MeshBounding> GetMeshesBounds();
    
    
    
    private readonly Guid _guid;

    private DrawCommand _command;

    private GetMeshesBounds _bounds;

    private MeshBounding[] _meshes;

    private u32[] _subpasses;



    // ReSharper disable once InconsistentNaming
    public Guid GUID => _guid;

    public IEnumerable<Camera> DrawBy => Camera.Cameras.Where(cam => cam.Drawers.SelectMany(drawer => drawer).Contains(this));

    public u64 TotalMeshCount;
    
    

    public Drawer(DrawCommand command, GetMeshesBounds? bounds = null, IEnumerable<Camera>? cameras = null, IEnumerable<u32>? subpasses = null)
    {
        _guid = new Guid();
        _command = command;
        _bounds = bounds ?? (() => ReadOnlySpan<MeshBounding>.Empty);

        if (cameras == null) return;

        _subpasses = subpasses?.ToArray() ?? new[] { 0U };
        
        foreach (Camera camera in cameras)
        {
            for (i32 subpass = 0; subpass < _subpasses.Length; subpass++)
            {
                AssignToCamera(camera, _subpasses[subpass]);
            }
        }
    }
    
    public Drawer(DrawCommand command, GetMeshesBounds? bounds, u32 subpass)
    {
        _guid = Guid.NewGuid();
        _command = command;
        _bounds = bounds ?? (() => ReadOnlySpan<MeshBounding>.Empty);

        _subpasses = new [] { subpass };

        AssignToCamera(Camera.World!, subpass);
    }
    
    public Drawer(DrawCommand command, GetMeshesBounds? bounds, Camera camera)
    {
        _guid = Guid.NewGuid();
        _command = command;
        _bounds = bounds ?? (() => ReadOnlySpan<MeshBounding>.Empty);

        _subpasses = new [] { 0U };

        AssignToCamera(camera);
    }
    
    public Drawer(DrawCommand command, GetMeshesBounds? bounds)
    {
        _guid = Guid.NewGuid();
        _command = command;
        _bounds = bounds ?? (() => ReadOnlySpan<MeshBounding>.Empty);

        _subpasses = new [] { 0U };

        AssignToCamera(Camera.World!);
    }
    
    public Drawer(DrawCommand command, GetMeshesBounds? bounds, Camera camera, u32 subpass)
    {
        _guid = Guid.NewGuid();
        _command = command;
        _bounds = bounds;
        
        _subpasses = new [] { subpass };

        AssignToCamera(camera, subpass);

    }

    public override bool Equals(object? obj) => obj is Drawer drawer && drawer._guid == _guid;

    public override int GetHashCode() => _guid.GetHashCode();
    
    
    
    public void Draw(Camera camera, CommandRecorder.RenderPassRecorder renderPass,
        ReadOnlySpan<DrawRange> ranges) => _command(camera, renderPass, ranges);
    public ReadOnlySpan<MeshBounding> GetMeshes() => _bounds();
    

    public void AssignToCamera(Camera camera, u32 subpass = 0U) => camera.AddDrawer(this, subpass);

    public void UnAssignCamera(Camera camera, u32 subpass = 0U) => camera.RemoveDrawer(this, subpass);

    
    public void Delete()
    {
        foreach (Camera camera in Camera.Cameras)
        {
            for (i32 subpass = 0; subpass < _subpasses.Length; subpass++)
            {
                camera.RemoveDrawer(this, _subpasses[subpass]);
            }
        }
        GC.SuppressFinalize(this);
    }

    public void Dispose() => Delete();

    ~Drawer() => Dispose();
}