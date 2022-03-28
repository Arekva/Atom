using Silk.NET.Core;
using Silk.NET.Maths;

namespace Atom.Engine;

public struct LightGLSL
{
    /* general light settings               */
    public LightType     Type                ;
    public Vector3D<f32> Color               ;
    public f32           Power               ;
    public f32           EmitterSize         ;
    public Vector3D<f32> Position            ;
    public f32           NearClip            ;
    public f32           FarClip             ;
    public Bool32        CastShadows         ;
    
    /* directional                          */
    public f32           Width               ;
    public f32           Height              ;
    
    /* point lights                         */
    public u32          PointShadowmap       ;
    
    /* spot lights                          */
    public f32          FovX                 ;
    public f32          FovY                 ;
    
    /* directional and spot lights          */
    public Vector3D<f32> DirectionalDirection;
    public u32           DirectionalShadowmap;
}