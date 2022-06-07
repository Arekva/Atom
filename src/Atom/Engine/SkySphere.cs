using Atom.Engine.Mesh;
using Atom.Engine.Shader;
using Atom.Engine.Vulkan;
using Silk.NET.Maths;

namespace Atom.Engine;

public class SkySphere : AtomObject
{
    private ReadOnlyMesh<GVertex, u16> _mesh;
    
    private RasterShader _shader;
    private RasterizedMaterial _material;

    private BufferSubresource _colorBuffer;
    
    private Drawer _drawer;

    public SkySphere(Vector4D<f64> color)
    {
        _mesh = ReadOnlyMesh.Load<u16>(path: "assets/Meshes/InvSphere.obj");

        _shader = Shader.Shader.Load<RasterShader>(@namespace: "Atom.Sky", name: "StaticProcedural");
        _material = new RasterizedMaterial(_shader);

        Vector4D<f32> f_color = (Vector4D<f32>)color;

        Span<Vector4D<f32>> sky_color = f_color.AsSpan();

        _colorBuffer = sky_color.CreateVulkanMemory(usages: BufferUsageFlags.UniformBuffer, type: MemoryType.DeviceLocalShared);
        _material.WriteBuffer<IFragmentModule>("_colorSettings", _colorBuffer.Subresource(0, 16), vk.DescriptorType.UniformBuffer);
        _material.WriteBuffer<IVertexModule>(Camera.ShaderDataName, Camera.ShaderData);
        
        _drawer = new Drawer(Draw, null, Camera.World!);
    }
    
    public override void Delete()
    {
        base.Delete();
        
        _drawer.Delete();
        _mesh.Delete();
        
        _material.Delete();
        _shader.Delete();

        _colorBuffer.Delete();
    }



    private void Draw(
        Camera camera, 
        CommandRecorder.RenderPassRecorder renderPass,
        ReadOnlySpan<Drawer.DrawRange> ranges // ignored
    )
    {
        renderPass.BindMaterial(_material, camera);
        renderPass.DrawIndexed(_mesh);
    }
}