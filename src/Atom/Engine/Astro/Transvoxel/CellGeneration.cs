using System.Diagnostics;
using System.Numerics;
using Silk.NET.Maths;

namespace Atom.Engine.Astro.Transvoxel;

public partial class Cell
    {
        public const int Resolution = 16;
        public const int HalfResolution = Resolution / 2;
        public const int MaxIndex = Resolution-1;
        
        public const int Width = Resolution;
        public const int Height = Resolution;
        public const int Depth = Resolution;
        
        public const int Count = Width * Height * Depth;

        public const double Delta = 1.0 / Resolution;

        public const double SolidValue = -1.0;
        public const double AirValue = 1.0;

        private static double[] AirData;
        private static double[] SolidData;

        private double[] _data = new double[Count];
        
        private bool _isAir = true;
        public bool IsAir => _isAir;
        
        private bool _isGround = true;
        public bool IsGround => _isGround;
        

        public bool IsTrivial => _isAir || _isGround;

        private double _isosurface = 0.0;
        public double Isosurface => _isosurface;

        public static List<(double x, double y, double z, double rad)> debug_spheres = new()
        {
            (0.0, 0.0, 0.0, 1.0)
        };

        public void FillData()
        {
            const double MinClamp = (double) (sbyte.MinValue + 0x01);
            const double MaxClamp = (double) sbyte.MaxValue;
            
            Vector3D<double> centre = _gridRelativeCentre;
            Vector3D<double> scale = _scales[_subdivision] * Vector3D<double>.One;
            //Vector3D deltas = scale / new Vector3D(Width, Height, Depth);
            //Vector3D halfDelta = deltas * 0.5;
            
            Vector3D<double> smin = centre - scale;
            Vector3D<double> smax = centre + scale;

            Func<double, double, double, double> generator = _grid.Generator;

            Parallel.For(0, Count, i =>
                //{
                //})
                //for (int i = 0; i < Count; i++)
            {
                // cell index 0..15 on each axis
                AMath.To3D(i, Width, Height, out int xI, out int yI, out int zI);

                // consider first mapping to be 0..16, but the values being actually 0..15 due
                // to index stuff. add the half delta to be at the centre of the current cube voxel.
                double x = AMath.Map(xI, 0, Resolution, smin.X, smax.X);
                double y = AMath.Map(yI, 0, Resolution, smin.Y, smax.Y);
                double z = AMath.Map(zI, 0, Resolution, smin.Z, smax.Z);

                _data[i] = generator(x, y, z);

                if (_isAir) _isAir = _data[i] > _isosurface;
                if (_isGround) _isGround = _data[i] < _isosurface;
            });
        }

        private double SampleAtStandalone(int xI, int yI, int zI)
        {
            const sbyte MinClamp = sbyte.MinValue + 0x01;
            const sbyte MaxClamp = sbyte.MaxValue;
            
            Vector3D<double> centre = _gridRelativeCentre;
            Vector3D<double> scale = _scales[_subdivision] * Vector3D<double>.One;
            Vector3D<double> deltas = scale / new Vector3D<double>(Width, Height, Depth);
            Vector3D<double> halfDelta = deltas * 0.5;
            
            Vector3D<double> smin = centre - scale;
            Vector3D<double> smax = centre + scale;
            
            Func<double, double, double, double> generator = _grid.Generator;
            
            // cell index 0..15 on each axis
            //UberMath.To3D(i, Width, Height, out int xI, out int yI, out int zI);
                
            // consider first mapping to be 0..16, but the values being actually 0..15 due
            // to index stuff. add the half delta to be at the centre of the current cube voxel.
            double x = AMath.Map(xI, 0,  Resolution, smin.X, smax.X);
            double y = AMath.Map(yI, 0,  Resolution, smin.Y, smax.Y);
            double z = AMath.Map(zI, 0,  Resolution, smin.Z, smax.Z);

            return generator(x, y, z);

        }

        /*        6_______7      Y                                              *
         *       /|      /|      |  Z                                           *
         *      2_______3 |      |/__ X                                         *
         *      | 4_____|_5                                                     *
         *      |/      |/       Each index of the byte array contains the      *
         *      0_______1        voxel state of the corresponding corner.       *
         *                                                                      *
         *                       The cell of the input coordinates actually     *
         *                       only owns the [0] value.                       *
         *                                                                      *
         *                                                                      */

        public unsafe (Vector3[], uint[], Vector3[]) Visit()
        {
            // first get all the required data to generate the cell.
            // we need all the neighbors of the current cell and the
            // cell itself, so a cube of 3x3x3 cells.
            
            const int cubeSize = 3;
            const int cubeVolume = cubeSize*cubeSize*cubeSize;
            const int cubeWidth = Width*cubeSize;
            const int cubeHeight = Height*cubeSize;
            const int cubeDepth = Depth*cubeSize;
            const int cubeCount = cubeWidth*cubeHeight*cubeDepth;
            
            const int maxVerticesCount = (Width + 1)*(Height + 1)*(Depth + 1);
            const int maxTrianglesCount = maxVerticesCount / 3;

            double[][] cube = new double[27][];
            Cell[] cubeCell = new Cell[27];
            
            // normal interpolation stuff
            Vector3[] grad = new Vector3[2];
            double[] isos = new double[2];
            //int[] cornerOffset = new int[3]; 

            for (int i = 0, z = -1; z <= 1; z++)
            for (       int y = -1; y <= 1; y++)
            for (       int x = -1; x <= 1; x++, i++)
            {
                int[] index = NeighborIndex(x, y, z);
                if (index.Length == 0) cube[i] = AirData;
                else
                {
                    Cell c = _grid.FindCellOrClosest(index);
                    cube[i] = c._data;
                    cubeCell[i] = c;
                }
            }
            
            (int array, int index) GetVoxelIndices(int x, int y, int z)
            {
                int cellx = x / Width;
                int celly = y / Height;
                int cellz = z / Depth;
                int cell = AMath.To1D(cellx, celly, cellz, cubeSize, cubeSize);

                int voxelx = x % Width;
                int voxely = y % Height;
                int voxelz = z % Depth;
                int voxel = AMath.To1D(voxelx, voxely, voxelz, Width, Height);

                return (cell, voxel);
            }

            Vector3 InterpolateNormal(int x, int y, int z, double isosurface, int corner1, int corner2)
            {
                for (int i = 0; i < 2; i++)
                {
                    int corner = i == 0 ? corner1 : corner2;
                    int ox = x + Transvoxel.CornerOffsets[corner][0];
                    int oy = y + Transvoxel.CornerOffsets[corner][1];
                    int oz = z + Transvoxel.CornerOffsets[corner][2];
                    grad[i] = GradientForPoint(ox, oy, oz);

                    (int arr, int idx) voxel = GetVoxelIndices(ox, oy, oz);
                    
                    isos[i] = cube[voxel.arr][voxel.idx];
                }

                Vector3 grad1 = grad[0];
                Vector3 grad2 = grad[1];
                double iso1 = isos[0];
                double iso2 = isos[1];
                
                // switch variable in order to always keep iso1 > iso 2
                if (iso2 < iso1)
                {
                    double temp = iso1;
                    iso1 = iso2;
                    iso2 = temp;
                    Vector3 temp2 = grad1;
                    grad1 = grad2;
                    grad2 = temp2;
                }

                const double epsilon = 0.0000001;

                return Math.Abs(iso1 - iso2) > epsilon
                    ? grad1 + (grad2 - grad1) / (float) (iso2 - iso1) * (float) (isosurface - iso1)
                    : grad1;
            }

            Vector3 GradientForPoint(int x, int y, int z)
            {
                int minx = x - 1;
                int maxx = x + 1;
                (int arr, int idx) pxmin = GetVoxelIndices(minx, y, z);
                (int arr, int idx) pxmax = GetVoxelIndices(maxx, y, z);
                double gradx = (cube[pxmax.arr][pxmax.idx] - cube[pxmin.arr][pxmin.idx]);
                int miny = y - 1;
                int maxy = y + 1;
                (int arr, int idx) pymin = GetVoxelIndices(x, miny, z);
                (int arr, int idx) pymax = GetVoxelIndices(x, maxy, z);
                double grady = (cube[pymax.arr][pymax.idx] - cube[pymin.arr][pymin.idx]);

                int minz = z - 1;
                int maxz = z + 1;
                (int arr, int idx) pzmin = GetVoxelIndices(x, y, minz);
                (int arr, int idx) pzmax = GetVoxelIndices(x, y, maxz);
                double gradz = (cube[pzmax.arr][pzmax.idx] - cube[pzmin.arr][pzmin.idx]);
                
                return new Vector3((float) gradx , (float)grady, (float)gradz);
            }
            
            // fig. 3.8(a) of the paper: 
            List<Vector3> edgeVertices = new (12);
            List<Vector3> edgeNormals = new (12);

            List<Vector3> normalsVector = new List<Vector3>(Count * 6);
            List<Vector3> trianglesVector = new List<Vector3>(Count * 6);

            //sbyte[] corner = new sbyte[8];
            Stopwatch sw = Stopwatch.StartNew();

            (int arr, int idx)[] cornerIndices = new (int,int)[8];
            Vector3[] cornerPositions = new Vector3[8];

            Vector3 halfRes = new Vector3(HalfResolution, HalfResolution, HalfResolution);
            
            // now that the neighborhood has been assigned, time to generate that mesh.
            // we need to loop through all the voxels of the grid.
            for (int voxel = 0; voxel < Count; voxel++)
            {
                edgeVertices.Clear();
                edgeNormals.Clear();
                
                // the cube 3D index of this cell data
                const int xOrigin = Width;
                const int yOrigin = Height;
                const int zOrigin = Depth;
                
                // in-cell position
                AMath.To3D(voxel, Width, Height, out int x, out int y, out int z);
                Vector3 offset = new Vector3(x - HalfResolution, y-HalfResolution, z-HalfResolution);

                // in-cube position
                int cubeX = x + xOrigin;
                int cubeY = y + yOrigin;
                int cubeZ = z + zOrigin;

                cornerIndices[0] = GetVoxelIndices(cubeX + 0, cubeY + 0, cubeZ + 0);
                cornerIndices[1] = GetVoxelIndices(cubeX + 1, cubeY + 0, cubeZ + 0);
                cornerIndices[2] = GetVoxelIndices(cubeX + 0, cubeY + 1, cubeZ + 0);
                cornerIndices[3] = GetVoxelIndices(cubeX + 1, cubeY + 1, cubeZ + 0);
                cornerIndices[4] = GetVoxelIndices(cubeX + 0, cubeY + 0, cubeZ + 1);
                cornerIndices[5] = GetVoxelIndices(cubeX + 1, cubeY + 0, cubeZ + 1);
                cornerIndices[6] = GetVoxelIndices(cubeX + 0, cubeY + 1, cubeZ + 1);
                cornerIndices[7] = GetVoxelIndices(cubeX + 1, cubeY + 1, cubeZ + 1);

                //const double isosurface = 0.0;
                int cubeIndex = 0;

                for (int i = 0; i < 8; i++)
                    //if (cornerIndices[i].outside) continue;
                    if (cube[cornerIndices[i].arr][cornerIndices[i].idx] <= _isosurface)
                        cubeIndex |= 1 << i;
                
                // if non trivial (not completely in or out surface)
                if (Transvoxel.EdgeTable[cubeIndex] != 0)
                {
                    byte cellClass = Transvoxel.RegularCellClass[cubeIndex];
                    RegularCellData cellData = Transvoxel.RegularCellData[cellClass];
                    ushort[] vertexData = Transvoxel.RegularVertexData[cubeIndex];

                    cornerPositions[0] = new Vector3(x - 1.0F, y - 1.0F, z - 1.0F);
                    cornerPositions[1] = new Vector3(x - 0.0F, y - 1.0F, z - 1.0F);
                    cornerPositions[2] = new Vector3(x - 1.0F, y - 0.0F, z - 1.0F);
                    cornerPositions[3] = new Vector3(x - 0.0F, y - 0.0F, z - 1.0F);
                    cornerPositions[4] = new Vector3(x - 1.0F, y - 1.0F, z - 0.0F);
                    cornerPositions[5] = new Vector3(x - 0.0F, y - 1.0F, z - 0.0F);
                    cornerPositions[6] = new Vector3(x - 1.0F, y - 0.0F, z - 0.0F);
                    cornerPositions[7] = new Vector3(x - 0.0F, y - 0.0F, z - 0.0F);
                    
                    for (int i = 0; i < cellData.GetVertexCount; i++)
                    {
                        ushort vertex = vertexData[i];
                        
                        int corner1 = (vertex >> 4) & 0x000F;
                        int corner2 = vertex & 0x000F;
                        
                        double iso1 = cube[cornerIndices[corner1].arr][cornerIndices[corner1].idx];
                        double iso2 = cube[cornerIndices[corner2].arr][cornerIndices[corner2].idx];
                        
                        Vector3 cornerPos1 = cornerPositions[corner1];
                        Vector3 cornerPos2 = cornerPositions[corner2];
                        
                        float t = (float)(iso2 / (iso2 - iso1));
                        Vector3 vertexPos = cornerPos1 * t + cornerPos2 * (1.0F-t);
                        
                        edgeVertices.Add(vertexPos);
                        edgeNormals.Add(Vector3.Normalize(InterpolateNormal(cubeX,cubeY,cubeZ, _isosurface, corner1, corner2)));
                    }

                    int totTriCount = cellData.GetTriangleCount * 3;
                    for (int i = 0; i < totTriCount; i++) //for (int i = totTriCount-1; i >= 0; i--)
                    {
                        byte vertexIndex = cellData.VertexIndex[i];

                        Vector3 vec = (edgeVertices[vertexIndex] - halfRes) * (float)Delta;
                        trianglesVector.Add(vec);
                        normalsVector.Add(edgeNormals[vertexIndex]);
                    }
                }
            }
            
            
            
            sw.Stop();
            Console.WriteLine($"[{this._tag}] Build time: {sw.Elapsed.TotalMilliseconds:F2} ms");

            Vector3[] vertices = new Vector3[trianglesVector.Count];
            uint[] triangles = new uint[vertices.Length];
            Vector3[] normals = new Vector3[normalsVector.Count];
            
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = trianglesVector[i];
                normals[i] = normalsVector[i];
                triangles[i] = (uint)i;
            }

            return (vertices, triangles, normals);
        }
        
        public static (int array, int index) TESTGetVoxelIndices(int x, int y, int z)
        {
            int cellx = x / Width;
            int celly = y / Height;
            int cellz = z / Depth;
            int cell = AMath.To1D(cellx, celly, cellz, 3, 3);

            int voxelx = x % Width;
            int voxely = y % Height;
            int voxelz = z % Depth;
            int voxel = AMath.To1D(voxelx, voxely, voxelz, Width, Height);

            return (cell, voxel);
        }

        /*private Vector3 CalculateNormal(Vector3[] vertices, int v0, int v1, int v2)
        {
            Vector3 v0pos = vertices[v0];
            Vector3 v1pos = vertices[v1];
            Vector3 v2pos = vertices[v2];
            return Vector3.Normalize(Vector3.Cross(v1pos - v0pos, v2pos - v1pos));
        }*/
        
        public static Dictionary<int, Vector3D<double>> RegularCellVertexLocation = new (12)
        {
            { 0x11, new (0.0F,0.5F,1.0F) },
            { 0x13, new (0.0F,1.0F,0.5F) },
            { 0x22, new (0.5F,0.0F,1.0F) },
            { 0x23, new (1.0F,0.0F,0.5F) },
            { 0x33, new (0.0F,0.0F,0.5F) },
            { 0x41, new (1.0F,0.5F,0.0F) },
            { 0x42, new (0.5F,1.0F,0.0F) },
            { 0x51, new (0.0F,0.5F,0.0F) },
            { 0x62, new (0.5F,0.0F,0.0F) },
            { 0x81, new (1.0F,0.5F,1.0F) },
            { 0x82, new (0.5F,1.0F,1.0F) },
            { 0x83, new (1.0F,1.0F,0.5F) },
        };
        
        /*public static Dictionary<int, Vector3D> RegularCellVertexNormal = new (12)
        {
            { 0x11, new (-1.0, 0.0, 1.0, true) },
            { 0x13, new (-1.0, 1.0, 0.0, true) },
            { 0x22, new ( 0.0,-1.0, 1.0, true) },
            { 0x23, new ( 1.0,-1.0, 0.0, true) },
            { 0x33, new (-1.0,-1.0, 0.0, true) },
            { 0x41, new ( 1.0, 0.0,-1.0, true) },
            { 0x42, new ( 0.0, 1.0,-1.0, true) },
            { 0x51, new (-1.0, 0.0,-1.0, true) },
            { 0x62, new ( 0.0,-1.0,-1.0, true) },
            { 0x81, new ( 1.0, 0.0, 1.0, true) },
            { 0x82, new ( 0.0, 1.0, 1.0, true) },
            { 0x83, new ( 1.0, 1.0, 0.0, true) },
        };*/
    }