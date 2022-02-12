using Silk.NET.Maths;

namespace Atom.Engine.Astro.Transvoxel;

public partial class Cell
    {
        public const int Octo = 8;
        
        private static int[] RootIndex { get; } = { -1 };

        private Cell _parent = null;

        public Cell Parent
        {
            get => _parent;
            internal set => _parent = value;
        }

        private Cell[] _children = Array.Empty<Cell>();
        public Cell[] Children => _children;

        private Grid _grid;

        private bool _isRoot = false;
        public bool IsRoot => _isRoot;

        private bool _isLeaf = false;
        public bool IsLeaf => _isLeaf;

        private bool _splittable = true;
        public bool Splittable => _splittable;

        private uint _subdivision = 0;
        public uint Subdivision => _subdivision;

        private bool _splitted = false;
        public bool Splitted => _splitted;

        private int _location = -1;
        public int Location => _location;

        private string _tag = null;
        public string Tag => _isRoot ? "R" : _tag;
        

        private Vector3D<double> _gridRelativeCentre;
        public Vector3D<double> GridRelativeCentre => _gridRelativeCentre;
        

        static Cell()
        {
            SolidData = new double[Count];
            AirData = new double[Count];

            //Parallel.For(0, Count, i =>
            //{
            for (int i = 0; i < Count; i++)
            {
                SolidData[i] = SolidValue;
                AirData[i] = AirValue;
            }

            //});
        }

        public Cell(Grid grid) : this(grid, null)
        {
            _grid.AddCell(this);
            _grid.InvokeCellCreated(this);
        }

        public Cell(Cell parent) : this(parent._grid, parent)
        {
            // has to be invoked from the splitting cell because 
            // the said cell asigns stuff after the ctor
            
            //_grid.InvokeCellCreatedAsync(this);
        }

        private Cell(Grid grid, Cell? parent = null)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            if (parent is null) _isRoot = true;
            else
            {
                _parent = parent;
                _subdivision = parent._subdivision + 1;
            }

            // todo: consider leaf if no more detailed data available
            _isLeaf = _subdivision == grid.MaxSubdivision;
            _splittable = !_isLeaf;
            
            // grid relative position assigned by splitted cell
        }

        public Cell[]? Subdivide()
        {
            if (_splitted || !_splittable) return null;

            _children = new Cell[Octo];

            Vector3D<double> ccentre = _gridRelativeCentre;
            double nextScale = _scales[_subdivision];
            for (int i = 0; i < Octo; i++)
            {
                Cell c = _children[i] = new Cell(this);
                c._location = i;
                c._tag = _tag + _indexCharMap[i]; 

                Vector3D<double> ncentre = _indexCentre[i];
                c._gridRelativeCentre = ccentre + ncentre * nextScale;

                _grid.AddCell(c);
                _grid.InvokeCellCreated(c);
            }

            _grid.RemoveCell(this);

            _splitted = true;
            _splittable = false;

            return _children;
        }

        public void SubdivideUntil(int targetDepth)
        {
            if (_subdivision >= targetDepth || !_splittable) return;
            
            Cell[]? cells = this.Subdivide();

            if (cells is null) return;
            for (int i = 0; i < cells.Length; i++) 
                cells[i].SubdivideUntil(targetDepth);
        }

        public int[] GetTag(Vector3D<double> position, uint subdivision)
        {
            // if outside of grid, doesn't correspond to anything
            if (Math.Abs(position.X) >= 1.0 || Math.Abs(position.Y) >= 1.0 || Math.Abs(position.Z) >= 1.0) 
                return Array.Empty<int>();
            
            // if is root
            if (subdivision == 0) return Array.Empty<int>();
            
            // the principle here will be to get the node coordinates (ABCDE...) of any
            // specified coordinates between -1.0 and 1.0 on both x y and z axis.
            // the subdivision parameter will determine the precision of the coordinates
            // i.e. sub 2 will give AB, while sub 6 could give ABDEAH.
            // the coordinates compute is achieved by getting the index relative
            // to the previous node, and then reuse that node in order to recalibrate the sampler:
            // i.e. the x coord is -0.48, so : (consider L as left and R as right)
            
            // 1st node: from -1.0 to 1.0, it is on the left side so L
            // 2nd node: from -1.0 to 0.0, remap so it gives a range from -1.0 to 1.0, which gives R => LR et ceterra. 
            
            //tl;dr take sample, zoom on sampled cube, sample, zoom on the new sampled cube...
            
            int[] coordinates = new int[subdivision];

            Vector3D<double> halfOne = Vector3D<double>.One * 0.5;
            Vector3D<double> zoomMin = Vector3D<double>.One * -1.0;
            Vector3D<double> zoomMax = Vector3D<double>.One;

            Vector3D<double> samplePos = position;
            Vector3D<double> indexPos;

            for (int i = 0; i < subdivision; i++)
            {
                // zoom sample pos
                samplePos.X = AMath.Map(samplePos.X, zoomMin.X, zoomMax.X, -1.0, 1.0);
                samplePos.Y = AMath.Map(samplePos.Y, zoomMin.Y, zoomMax.Y, -1.0, 1.0);
                samplePos.Z = AMath.Map(samplePos.Z, zoomMin.Z, zoomMax.Z, -1.0, 1.0);

                // get index from position
                coordinates[i] = NormalizedPositionToIndex(samplePos);

                indexPos = _indexCentre[coordinates[i]];
                zoomMin = indexPos - halfOne;
                zoomMax = indexPos + halfOne;
            }

            return coordinates;
        }
        
        

        public int[] NeighborIndex(int xdir, int ydir, int zdir)
        {
            // if this
            if (xdir == 0 && ydir == 0 && zdir == 0) 
                return _isRoot ? RootIndex : _tag.Select(c => _charIndexMap[c]).ToArray();
            
            if (_subdivision == 0)
                return Array.Empty<int>();

            Vector3D<double> thisPos = _gridRelativeCentre;
            double scale = _scales[_subdivision];
            Vector3D<double> shift = new Vector3D<double>(xdir, ydir, zdir) * scale * 2.0;
            Vector3D<double> searchPos = thisPos + shift;

            return GetTag(searchPos, _subdivision);
        }

        private static char[] _indexCharMap = new char[8]
        { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };

        private static Dictionary<char, int> _charIndexMap = new(8)
        { { 'A', 0 }, { 'B', 1 }, { 'C', 2}, { 'D', 3 }, 
          { 'E', 4 }, { 'F', 5 }, { 'G', 6}, { 'H', 7 } };

        public static double GetScale(uint subdivision) => _scales[subdivision];

        // precomputed scale down to sub < 20
        // 
        // script:
        // for(int i = 0; i < 20; i++) { Console.WriteLine((1.0/(1<<(int)(i + 1.0))*2.0).ToString("F19")); }
        private static double[] _scales = new double[20]
        {
            1.0000000000000000000,
            0.5000000000000000000,
            0.2500000000000000000,
            0.1250000000000000000,
            0.0625000000000000000,
            0.0312500000000000000,
            0.0156250000000000000,
            0.0078125000000000000,
            0.0039062500000000000,
            0.0019531250000000000,
            0.0009765625000000000,
            0.0004882812500000000,
            0.0002441406250000000,
            0.0001220703125000000,
            0.0000610351562500000,
            0.0000305175781250000,
            0.0000152587890625000,
            0.0000076293945312500,
            0.0000038146972656250,
            0.0000019073486328125,
        };
        
        private static Vector3D<double>[] _indexCentre = new Vector3D<double>[8]
        {
            new(-0.5D,-0.5D,-0.5D), // left down backward
            new( 0.5D,-0.5D,-0.5D), // right down backward
            new(-0.5D, 0.5D,-0.5D), // left up backward
            new( 0.5D, 0.5D,-0.5D), // right up backward
            new(-0.5D,-0.5D, 0.5D), // left down forward
            new( 0.5D,-0.5D, 0.5D), // right down forward
            new(-0.5D, 0.5D, 0.5D), // left up forward
            new( 0.5D, 0.5D, 0.5D), // right up forward
        };
        
        private static int NormalizedPositionToIndex(Vector3D<double> pos)
        {
            bool left = pos.X < 0.0D;
            bool down = pos.Y < 0.0D;
            bool back = pos.Z < 0.0D;

            // fuck off
            return left ? down ? back ? 0 : 4 : back ? 2 : 6 : down ? back ? 1 : 5 : back ? 3 : 7;
        }
    }