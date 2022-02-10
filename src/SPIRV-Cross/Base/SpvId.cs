using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SPIRVCross.Base
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal readonly partial struct SpvId : IEquatable<SpvId>
    {
        public SpvId(u32 value) => this.Value = value;

        public readonly u32 Value;

        public bool Equals(SpvId other) => Value.Equals(other.Value);

        public override bool Equals(object obj) => obj is SpvId other && Equals(other);

        public override i32 GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();

        public static implicit operator u32(SpvId from) => from.Value;

        public static implicit operator SpvId(u32 from) => new SpvId(from);

        public static bool operator ==(SpvId left, SpvId right) => left.Equals(right);

        public static bool operator !=(SpvId left, SpvId right) => !left.Equals(right);
    }






	
}
