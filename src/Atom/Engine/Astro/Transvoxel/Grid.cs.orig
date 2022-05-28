using System.Collections.Concurrent;

namespace Atom.Engine.Astro.Transvoxel;

/// <summary> Grid / Octree </summary>
    public sealed partial class Grid
    {
        public const uint DefaultMaxSubdivision = 5U;

        public double Scale { get; set; } = 1.0;

        private static int[] RootIndex { get; } = { -1 };
        
        private uint _maxSubdivision;
        public uint MaxSubdivision => _maxSubdivision;

        public bool Disposed { get; private set; } = false;

        private Func<double, double, double, double> _generator = (x, y, z) => x * x + y * y + z * z - 1.0;
        public Func<double, double, double, double> Generator
        {
            get => _generator;
            set => _generator = value;
        }

        public event Action<Cell>? OnCellCreated;
        public event Action<Cell>? OnCellSplitted;

        private Cell _rootCell = null;
        public Cell RootCell => _rootCell;

        private ConcurrentDictionary<string, Cell> _cells = new();
        public ICollection<Cell> Cells => _cells.Values;

        private bool _initialized = false;
        public bool Initialized => _initialized;

        public Grid(uint maxSub = DefaultMaxSubdivision)
        {
            _maxSubdivision = maxSub;
        }

        public Cell FindCellOrClosest(int[] index)
        {
            Cell current = _rootCell;
            if (index == Array.Empty<int>() || index[0] == RootIndex[0]) return current;
            
            for (int i = 0; i < index.Length; i++)
            {
                if (!current.Splitted) return current;
                current = current.Children[index[i]];
            }

            return current;
        }

        internal void InvokeCellCreated(Cell cell) => OnCellCreated?.Invoke(cell);
        internal bool AddCell(Cell cell) => _cells.TryAdd(cell.Tag, cell);
        internal bool RemoveCell(Cell cell) => _cells.TryRemove(cell.Tag, out _);

        public void Init()
        {
            _rootCell = new Cell(this);
            _initialized = true;
        }

        public void TestSub(int depth)
        {
            if (!_initialized) Init();
            //if(depth != 0)
            _rootCell.SubdivideUntil(depth);
        }
    }