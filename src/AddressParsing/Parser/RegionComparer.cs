using System.Collections.Generic;

namespace AddressParsing
{
    internal class RegionComparer : IEqualityComparer<Region>
    {
        public static RegionComparer Instance { get; }
        static RegionComparer()
        {
            Instance = new RegionComparer();
        }

        private RegionComparer()
        {

        }

        public bool Equals(Region x, Region y)
        {
            return x.ID == y.ID;
        }

        public int GetHashCode(Region obj)
        {
            return obj.ID.GetHashCode();
        }
    }
}
