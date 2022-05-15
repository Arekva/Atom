using System;

namespace Atom.Engine.Tree
{
    [Flags]
    public enum Faces : byte
    {
        None = 0b_00_00_00,
        Left = 0b_00_00_01,
        Right = 0b_00_00_10,
        Down = 0b_00_01_00,
        Up = 0b_00_10_00,
        Backward = 0b_01_00_00,
        Forward = 0b_10_00_00,
        All = Left|Right|Down|Up|Backward|Forward,
    }
}