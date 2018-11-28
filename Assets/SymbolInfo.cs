using System;

namespace XRay
{
    struct SymbolInfo : IEquatable<SymbolInfo>
    {
        public int Index { get; private set; }
        public bool Flipped { get; private set; }

        public SymbolInfo(int index, bool flipped)
        {
            Index = index;
            Flipped = flipped;
        }

        public bool Equals(SymbolInfo other)
        {
            return Index == other.Index && Flipped == other.Flipped;
        }

        public override bool Equals(object obj)
        {
            return obj is SymbolInfo && Equals((SymbolInfo) obj);
        }

        public override int GetHashCode()
        {
            return Index + (Flipped ? 8472 : 0);
        }

        public static bool operator ==(SymbolInfo a, SymbolInfo b) { return a.Equals(b); }
        public static bool operator !=(SymbolInfo a, SymbolInfo b) { return !a.Equals(b); }

        public override string ToString()
        {
            return string.Format(@"[{0}{1}]", Index, Flipped ? " flipped" : "");
        }
    }
}