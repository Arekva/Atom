using Silk.NET.Maths;

namespace Atom.Engine;

public class Culler
{
    private bool _enabled = true;
    private bool _contributionCulling = true;

    public f64 ContributionDiscardSize = 0.75D;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = true;
    }       

    public Culler()
    {
        
    }

    public void CullPerspective(
        f64 fov, Vector2D<f64> screenSize,
        ReadOnlySpan<Vector3D<f64>> relativeLocations, ReadOnlySpan<f64> extents,
        Span<i32> culledIndices
    )
    {
        if (!_enabled)
        {
            for (int i = 0; i < relativeLocations.Length; i++)
            {
                culledIndices[i] = i; // cull everything.
            }

            return;
        }
        
        f64 half_fov = fov / 2.0D;
        f64 size = Math.Min(screenSize.X, screenSize.Y);
        
        i32 culled_count = 0;
        for (i32 i = 0; i < relativeLocations.Length; i++)
        {
            if (CullPerspective(in half_fov, in size, in relativeLocations[i], in extents[i]))
            {
                culledIndices[culled_count] = i;
                ++culled_count;
            }
        }
    }

    private bool CullPerspective(in f64 halfFov, in f64 size, in Vector3D<f64> relativeLocation, in f64 extent)
    {
        if (_contributionCulling && !ContributionCullPerspective(in halfFov, in size, in relativeLocation, in extent)) return false;

        return true;
    }
    
    
    
    
    /*public void CullOrthographic(
        f64 fov, Vector2D<f64> screenSize,
        ReadOnlySpan<Vector3D<f64>> relativeLocations, ReadOnlySpan<f64> extents,
        Span<i32> culledIndices
    )
    {
        if (!_enabled)
        {
            for (int i = 0; i < relativeLocations.Length; i++)
            {
                culledIndices[i] = i; // cull everything.
            }

            return;
        }
        
        f64 half_fov = fov / 2.0D;
        
        i32 culled_count = 0;
        for (i32 i = 0; i < relativeLocations.Length; i++)
        {
            if (CullPerspective(in half_fov, in size, in relativeLocations[i], in extents[i]))
            {
                culledIndices[culled_count] = i;
                ++culled_count;
            }
        }
    }
    
    private bool CullOrthographic(in Vector2D<f64> pixelSize, in Vector3D<f64> relativeLocation, in f64 extent)
    {
        if (_contributionCulling && !ContributionCullOrthographic(in pixelSize, in extent)) return false;

        return true;
    }*/

    
    
    
    
    
    private bool ContributionCullPerspective(in f64 halfFov, in f64 screenExtent, in Vector3D<f64> objectPosition, in f64 objectExtent)
    {
        f64 object_distance = objectPosition.Length;

        f64 object_size_at_distance = 2.0D * Math.Atan(object_distance / 2.0D * objectExtent);
        f64 screen_size_at_distance = Math.Tan(halfFov / 2.0D) * object_distance * screenExtent;

        f64 pixel_size_at_distance = screenExtent / screen_size_at_distance;

        return pixel_size_at_distance / object_size_at_distance > ContributionDiscardSize;
    }
    /*
    private bool ContributionCullOrthographic(in Vector2D<f64> pixelSize, in f64 objectExtent)
    {
        return Math.Max(objectExtent / pixelSize.X, objectExtent / pixelSize.Y) > ContributionDiscardSize;
    }*/
}

