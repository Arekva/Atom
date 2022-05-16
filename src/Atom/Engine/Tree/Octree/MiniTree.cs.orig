namespace Atom.Engine.Tree
{
    public class MiniTree
    {
        private MiniTree Parent { get; }
        public List<MiniTree> Branches { get; }

        public char Value { get; } = 'X';

        public MiniTree(MiniTree parent, char value, u32 capacity = Octree<object>.OCTET)
        {
            Parent = parent;
            Value = value;
            Branches = new List<MiniTree>((i32)capacity);
            parent?.Branches.Add(this);
        }

        public void AddIndex(string index)
        {
            index = index.ToUpper();
            MiniTree current = this;
            for (int i = 0; i < index.Length; i++)
            {
                char val = index[i];
                current = current.FindChildByValue(val) ?? new MiniTree(current, val);
            }
        }

        private static char[] _indices = new char[8] {'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'};
        
        public string NonExistingIndicesInChildren()
        {
            List<char> idx = _indices.ToList();
            foreach (MiniTree branch in Branches) 
                idx.Remove(branch.Value);
            return new string(idx.ToArray());
        }

        public string Branch
        {
            get
            {
                List<char> chars = new List<char>();

                MiniTree current = this;
                while (current.Parent != null)
                {
                    chars.Add(current.Value);
                    current = current.Parent;
                }

                if (chars.Count == 0) return null;
                chars.Reverse();
                return new string(chars.ToArray());
            }
        }

        private MiniTree FindChildByValue(char value) => Branches.FirstOrDefault(b => b.Value == value);
    }
}