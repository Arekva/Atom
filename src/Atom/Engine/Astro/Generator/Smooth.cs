using Atom.Engine.Astro;
using Silk.NET.Maths;

namespace Atom.Engine.Generator;

public class Smooth : IGenerator
{
    public f64 Radius { get; set; } = 1.0D;
    
    public void Generate(u128 location, u32 depth, Span<Voxel> voxels)
    {
        f64 radius_squared = Radius * Radius;

        Location cell_location_centre = Grid.GetGridLocation(location, depth);

        Location scale = new(Vector3D<f64>.One * Cell.SCALES[depth]);

        Location cell_location_000 = cell_location_centre - scale;
        Location cell_location_111 = cell_location_centre + scale;

        // if a precision error happens, this would be here
        Vector3D<f64> from_pos = cell_location_000.Coordinates;
        Vector3D<f64> to_pos = cell_location_111.Coordinates;

        for (u32 z_cell = 0; z_cell < Cell.UNITS; ++z_cell) 
        for (u32 y_cell = 0; y_cell < Cell.UNITS; ++y_cell) 
        for (u32 x_cell = 0; x_cell < Cell.UNITS; ++x_cell)
        {
            u32 i = AMath.To1D(x_cell, y_cell, z_cell, Cell.UNITS, Cell.UNITS);
            ref Voxel voxel = ref voxels[(i32)i];

            f64 x_pct = x_cell / (f64)Cell.UNITS;
            f64 y_pct = y_cell / (f64)Cell.UNITS;
            f64 z_pct = z_cell / (f64)Cell.UNITS;
            
            f64 x_grid = AMath.Lerp(from_pos.X, to_pos.X, x_pct);
            f64 y_grid = AMath.Lerp(from_pos.Y, to_pos.Y, y_pct);
            f64 z_grid = AMath.Lerp(from_pos.Z, to_pos.Z, z_pct);

            Vector3D<f64> pos_grid = new(x_grid, y_grid, z_grid);

            if (pos_grid.LengthSquared <= radius_squared)
            {
                voxel.Element = 1;
            }
        }
    }
}