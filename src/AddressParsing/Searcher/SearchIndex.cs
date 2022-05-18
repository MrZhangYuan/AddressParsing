using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AddressParsing
{
    [DebuggerDisplay("{Index}")]
    struct SearchIndex
    {
        public UpperLetter Index
        {
            get;
        }

        public Region[] Range
        {
            get;
        }

        public SearchIndex(UpperLetter index, Region[] range)
        {
            Index = index;
            Range = range;
        }

        public override string ToString()
        {
            return $"{Index}-({Range.Length})";
        }

        public override bool Equals(object obj)
        {
            return obj is SearchIndex index
                && index.Index == this.Index;
        }

        public override int GetHashCode()
        {
            return this.Index.GetHashCode();
        }
    }
}
