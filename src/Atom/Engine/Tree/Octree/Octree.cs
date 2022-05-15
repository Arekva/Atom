using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Silk.NET.Maths;

namespace Atom.Engine.Tree
{
    public delegate void OctreeBranchEditDelegate<T>(Octree<T> branch) where T : class;
    public delegate void OctreeBranchSplitEditDelegate<T>(Octree<T> branch, bool subsplitting) where T : class;
    
    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public class Octree<T> where T : class
    {
        #region Consts
        /// <summary>
        /// The number of branches an octree node has.
        /// </summary>
        public const u32 OCTET = 8;
        /// <summary>
        /// The default name for a tree.
        /// </summary>
        public const string DEFAULT_NAME = "Octree";
        #endregion

        #region Events
        /// <summary>
        /// Triggers when this node is being merged. This this only triggered onto the root node.
        /// </summary>
        public event OctreeBranchEditDelegate<T> OnMerge;
        /// <summary>
        /// Triggers when a node is merged. This this only triggered onto the root node.
        /// </summary>
        public event OctreeBranchEditDelegate<T> OnMerged;
        /// <summary>
        /// Triggers when a node is being split. This this only triggered onto the root node.
        /// </summary>
        public event OctreeBranchEditDelegate<T> OnSplit;
        /// <summary>
        /// Triggers when a node is split. This this only triggered onto the root node. Yes, I know "Splitted" is not correct english but go tell C# about that ok?
        /// </summary>
        public event OctreeBranchSplitEditDelegate<T> OnSplitted;
        /// <summary>
        /// Triggers when a node is being deleted. This this only triggered onto the root node.
        /// </summary>
        public event OctreeBranchSplitEditDelegate<T> OnDelete;
        /// <summary>
        /// Triggers when a node is deleted. This this only triggered onto the root node.
        /// </summary>
        public event OctreeBranchSplitEditDelegate<T> OnDeleted;
        
        // tree shared
        /// <summary>
        /// Triggers when a node is being created. This this only triggered onto the root node. 
        /// </summary>
        public event OctreeBranchSplitEditDelegate<T> OnCreate;
        /// <summary>
        /// Triggers when a node is created. This this only triggered onto the root node.
        /// </summary>
        public event OctreeBranchSplitEditDelegate<T> OnCreated;
        #endregion
        
        
        /// <summary>
        /// The parent node of this node. Null if root part
        /// </summary>
        public Octree<T> Parent { get; private set; }
        /// <summary>
        /// The children nodes of this node. Equals to Array.Empty{Octree{T}}() if no possible children.
        /// </summary>
        public Octree<T>[] Branches { get; private set; } = null;

        private uint _maxSubdivision = 5U;
        /// <summary>
        /// The maximum amount of subdivisions possible in this tree.
        /// Variable shared between all the branches of the tree.
        /// </summary>
        public uint MaxSubdivision
        {
            get => Root._maxSubdivision;
            set => Root._maxSubdivision = value;
        }
        /// <summary>
        /// The current subdivision tier of this node.
        /// </summary>
        public uint Subdivision { get; }

        public uint _minSubdivision = 0U;

        public uint MinSubdivision
        {
            get => Root._minSubdivision;
            set => Root._minSubdivision = value;
        }

        public Octree<T> this[int index]
        {
            get
            {
                if (IsLeaf || !HasBranches) throw new OctreeException("Cannot retrieve any branches, this node has none.");
                if (index < 0 || index > OCTET - 1) throw new ArgumentOutOfRangeException("Index must be in the 0..7 (A..H) range", nameof(index));

                return Branches[index];
            }
        }

        public Octree<T> this[char index] => this[CharToBranchIndex(index)];

        /// <summary>
        /// Is this a leaf node of the <see cref="Octree{T}"/>?
        /// The leaf is a end point of the tree, either the resolution is good enough or no more detail available in sub-nodes.
        /// </summary>
        public bool IsLeaf => Subdivision == MaxSubdivision;
        
        /// <summary>
        /// Is this a root node of the <see cref="Octree{T}"/>?
        /// </summary>
        public bool IsRoot => Parent == null;

        /// <summary>
        /// Can this node be split ?
        /// </summary>
        public bool IsSplittable => !IsLeaf && !HasBranches;

        /// <summary>
        /// Can this node be merged ?
        /// </summary>
        public bool IsMergeable => !IsLeaf && HasBranches;

        /// <summary>
        /// Do this node has active branches?
        /// </summary>
        public bool HasBranches => Branches[0] != null;

        /// <summary>
        /// Do this node's branches also have branches?
        /// </summary>
        public bool HasBranchesInheritance
        {
            get
            {
                if (IsMergeable) for (int i = 0; i < OCTET; i++) if (Branches[i].HasBranches) return true;
                return false;
            }
        }

        /// <summary>
        /// The root <see cref="Octree{T}"/> of this node.
        /// </summary>
        public Octree<T> Root
        {
            get
            {
                if (Parent == null) return this;

                Octree<T> parent = Parent;
                if (parent.IsRoot) return parent;
                else while (!(parent = parent.Parent).IsRoot) { }
                return parent;
            }
        }

        private string _name = null;

        /// <summary>
        /// The name of this tree.
        /// Variable shared across all the tree.
        /// </summary>
        public string Name
        {
            get => Root._name;
            set => Root._name = value;
        }
        
        public string Branch { get; set; } = "";
        public T Value { get; set; } = null;
        public Guid Identifier { get; } = Guid.NewGuid();

        public Octree(Octree<T>? parentNode = null, int branch = 0, string name = DEFAULT_NAME, uint maxSubdivisions = 10U, uint minSubdivisions = 0U, bool subsplitting = false)
        {
            Parent = parentNode;
            
            Root.OnCreate?.Invoke(this, subsplitting);
            if (parentNode != null)
            {
                Subdivision = parentNode.Subdivision + 1;
                
                if (Parent.IsLeaf || Subdivision > MaxSubdivision) throw new OctreeException($"Cannot use this as subdivision since it is out of the limit of {MaxSubdivision}");
                if (branch > OCTET - 1) throw new ArgumentOutOfRangeException($"{branch} is not a valid branch index, it must be in the range 0..7", nameof(branch));
                
                Branch = parentNode.Branch + BranchIndexToChar(branch);
            }
            else 
            {
                _name = name;
                _maxSubdivision = maxSubdivisions;
                _minSubdivision = minSubdivisions;
            }
            
            Branches = Subdivision == MaxSubdivision ? Array.Empty<Octree<T>>() : new Octree<T>[OCTET]; // create children container in case this is not a leaf
            
            Root.OnCreated?.Invoke(this, subsplitting);
        }

        public static char BranchIndexToChar(int branch)
            => branch switch {
                0 => 'A', 1 => 'B', 2 => 'C', 3 => 'D',
                4 => 'E', 5 => 'F', 6 => 'G', 7 => 'H',
                _ => '?' };
        public static int CharToBranchIndex(char @char) 
            => @char switch {
                'A' => 0, 'B' => 1, 'C' => 2, 'D' => 3,
                'E' => 4, 'F' => 5, 'G' => 6, 'H' => 7,
                _ => -1
            };

        public Octree<T>[] Split(int toSubdivision = -1)
        {
            if (!IsSplittable) throw new OctreeException($"Unable to split octree's node \"{this}\": leaf or already split.");
            if (toSubdivision > MaxSubdivision) throw new ArgumentException("Unable to split to asked subdivision: it is higher than the maximum subdivision possible.");

            bool subsplit = false;
            Root.OnSplit?.Invoke(this);
            Octree<T>[] branches = Branches;
            for (int i = 0; i < OCTET; i++)
            {
                if (toSubdivision - this.Subdivision > 0 && this.Subdivision + 1 != this.MaxSubdivision) subsplit = true;
                
                branches[i] = new Octree<T>(parentNode: this, branch: i, subsplitting: subsplit);
                if(subsplit) branches[i].Split(toSubdivision);
            }
            Root.OnSplitted?.Invoke(this, subsplit);
            return branches;
        }

        public static string CommonAncestor(string aIndex, string bIndex)
        {
            aIndex = aIndex.ToUpper();
            bIndex = bIndex.ToUpper();
            
            int aLength = aIndex.Length;
            int bLength = bIndex.Length;
            
            int maxSearchDepth = Math.Min(aLength, bLength);

            int sameUntil;
            for (sameUntil = 0; sameUntil < maxSearchDepth; sameUntil++) if (aIndex[sameUntil] != bIndex[sameUntil]) break;

            
            return sameUntil == 0 ? "R" : aIndex.Substring(0, sameUntil);
        }

        public Octree<T> SmoothSplit(string toIndex, int smoothFactor = 1) => Root.SmoothSplitTo(toIndex,smoothFactor);

        private Octree<T> SmoothSplitTo(string index, int smoothFactor)
        {
            Octree<T> tree = SplitTo(index);
            
            for (int i = 0; i < EOctree.CubeFaces.Length; i++)
            {
                for (int j = 0; j < index.Length; j++)
                {
                    string idx = GetNeighborIndex(index, EOctree.CubeFaces[i], (uint)(index.Length - j), 1);
                    if(idx != null) SplitTo(idx);
                }
            }

            return tree;
        }

        public Octree<T> Split(string toIndex) => Root.SplitTo(toIndex);

        private Octree<T> SplitTo(string index)
        {
            //if (!IsSplittable) throw new OctreeException($"Unable to split octree's node \"{this}\": leaf or already split.");
            if(string.IsNullOrEmpty(index) || string.IsNullOrWhiteSpace(index)) throw new ArgumentException("Please provide an index to split to.");
            int askedSubs = index.Length;
            if (askedSubs > MaxSubdivision) throw new ArgumentException("Unable to split to asked subdivision: it is higher than the maximum subdivision possible.");

            Octree<T> result = FindNode(index);
            if (result != null) return result;
            result = this;

            index = index.ToUpper();
            
            for (int i = 0; i < index.Length; i++)
            {
                //todo: sanitize inputs.
                if (result.HasBranches) result = result[index[i]]; // split if required
                else if (result.IsSplittable)
                {
                    Octree<T>[] splitted = result.Split();
                    result = splitted[CharToBranchIndex(index[i])]; // otherwise just grab the current node
                }
            }

            return result;
        }

        /// <summary>
        /// Remove nodes that are useless to get to index.
        /// </summary>
        /// <param name="index"></param>
        public void Cleanup(string index)
        {
            Octree<T> current = Root;
            for (int i = 0; i < index.Length; i++)
            {
                // for each node
                if (current.IsSplittable) break;
                char idx = index[i];

                // for each branch
                for (int j = 0; j < OCTET; j++)
                {
                    // get the tree of the branch
                    if(BranchIndexToChar(j) == idx) continue; // ignore if useful branch
                    Octree<T> tree = current[j];
                    if(tree.IsMergeable) tree.Merge();
                }

                if (current.IsSplittable) break; // no need to get deeper, no more nodes to clear.
                current = current[idx];
            }
        }

        public void SmoothCleanup(string index, int smoothFactor = 1)
        {
            // what index is used at which 
            MiniTree indices = new MiniTree(null, 'X');
            
            for (int i = 0; i < EOctree.CubeFaces.Length; i++)
            {
                for (int j = 0; j < index.Length; j++)
                {
                    string idx = GetNeighborIndex(index, EOctree.CubeFaces[i], (uint) (index.Length - j - 1), 1);
                    if (idx != null) indices.AddIndex(idx);
                }
            }

            RemoveUnusedBranches(indices);
        }

        private void RemoveUnusedBranches(MiniTree tree)
        {
            MiniTree current = tree;
            
            string unused = current.NonExistingIndicesInChildren();
            for (int i = 0; i < unused.Length; i++)
            {
                string unusedBranch = current.Branch + unused[i];
                FindNode(unusedBranch)?.Merge();
            }

            foreach (MiniTree miniBranch in current.Branches)
            {
                RemoveUnusedBranches(miniBranch);
            }
        }

        public void Merge()
        {
            if (!IsMergeable) return;//throw new OctreeException($"Unable to merge octree's node \"{this}\": leaf or nothing to merge.");

            Merge(0);
        }
        private void Merge(int recursiveCount)
        {
            Root.OnMerge?.Invoke(this);
            for (int i = 0; i < OCTET; i++)
            {   // delete all branches
                Octree<T> branch = Branches[i];
                if(branch == null) continue;
                
                // merge child branch if needed
                if(branch.IsMergeable) branch.Merge(recursiveCount + 1);

                branch.Delete();
            }

            // create a new array in order to throw out the previous one,
            // or just set to null if the branch has to be totally deleted 
            // (avoiding the creation of a useless array [branch.Merge() => array:new => branch.Delete() => array:null])
            Branches = recursiveCount == 0 ? new Octree<T>[OCTET] : null;
            Root.OnMerged?.Invoke(this);
        }

        public Octree<T> FindNode(Guid id)
        {
            Octree<T> result = null;
            if (this.Identifier == id) result = this;
            
            else if(!IsLeaf)
                for (int i = 0; i < OCTET; i++)
                {
                    Octree<T> branch = Branches[i];

                    if (branch != null) result = branch.FindNode(id);
                    if (result != null) break;
                }

            return result;
        }

        /// <summary>
        /// Find a specific branch from its branch name (i.e. ACHBD will be the Root->A->C->H->B->D node). This is shared onto the tree.
        /// </summary>
        /// <param name="branch"></param>
        /// <returns></returns>
        public Octree<T> FindNode(string branch)
        {
            // branches names are into the following format:
            // [tree name]-ABCDEF
            // A => index 0 of branches, B => index 1, C => index 2...
            // so [name]-ABC will give: Root[0[1[2]]] 

            Octree<T> currentBranch = Root;

            if (string.IsNullOrEmpty(branch) || string.IsNullOrWhiteSpace(branch)) return currentBranch;
            if (branch.Length > MaxSubdivision)
                throw new ArgumentOutOfRangeException(nameof(branch),
                    "Node cannot exist since its branch name is longer than the maximum subdivision possible.");

            branch = branch.ToUpper();

            i32 i = 0;
            try
            {
                for (i = 0; i < branch.Length; i++)
                {
                    if (!currentBranch.HasBranches)
                    {
                        currentBranch = null;
                        break;
                    }

                    currentBranch = currentBranch[branch[i]]; // retrieve next branch
                }

                if (currentBranch == null) return null;
                if (currentBranch.Branch == branch) return currentBranch; // found
            }
            catch (ArgumentOutOfRangeException aoore) // rethrow as a ArgumentException
            {
                throw new ArgumentException(
                    $"Node name is not valid at character n°{i}: '{branch[i]}': node names must be in the A..H range.",
                    nameof(branch));
            }

            return currentBranch;
        }

        private static string GetNeighborIndex(string index, Faces side, uint subdivision, float scale = 1)
        {
            // there is surely a better way to get a neighbor but I'll use
            // the location finder from position I've made.
            Vector3D<f64> nodeCenter = EOctree.GetNodeCenter(index);
            Vector3D<f64> shift = side.GetDirectionCubical() * EOctree.SubdivisionScale(subdivision);
            
            Vector3D<f64> sample = nodeCenter + shift * (scale * 2);
            return EOctree.ToGridSpace(sample, subdivision);
        }

        // delete everything in this node so the GC deletes it.
        // now I'm thinking about it, the GC's kinda like a grim reaper,
        // taking away the remaining empty variable to its numerical void...
        // I should probably get to bed probably isn't it?
        public void Delete(bool subsplitting = false)
        {
            Root.OnDelete?.Invoke(this, subsplitting);
            Value = null;
            Branch = "Merged node (previously {this})";
            Parent = null;
            Branches = null;
            Root.OnDeleted?.Invoke(this, subsplitting);
        }
        
        public override int GetHashCode() => Identifier.GetHashCode();
        public override string ToString() => ToString("N-B");
        public override bool Equals(object obj) => obj is Octree<T> tree && Identifier.Equals(tree.Identifier);
        public string ToString(string format) => format.Replace("N", Name).Replace("B", IsRoot ? "R" : Branch);
    }
}