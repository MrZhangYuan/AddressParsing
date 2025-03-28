using System.Collections.Generic;

namespace AddressParsing
{
    internal class RegionWrapComparer : IEqualityComparer<RegionWrap>
    {
        public static RegionWrapComparer Instance { get; }
        static RegionWrapComparer()
        {
            Instance = new RegionWrapComparer();
        }

        private RegionWrapComparer()
        {

        }

        public bool Equals(RegionWrap x, RegionWrap y)
        {
            return RegionComparer.Instance.Equals(x.Region, y.Region);
        }

        public int GetHashCode(RegionWrap obj)
        {
            return RegionComparer.Instance.GetHashCode(obj.Region);
        }
    }
}
