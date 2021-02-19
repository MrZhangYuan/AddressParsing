using System;

namespace AddressParsing
{

    public enum MatchType
    {
        /// <summary>
        /// 等级全称命中
        /// </summary>
        FullName = 0,
        /// <summary>
        /// 全称命中
        /// </summary>
        Name = 1,
        /// <summary>
        /// 简称命中
        /// </summary>
        ShortName = 2
    }

    public class MatchRegionItem
    {
        public Region MatchRegion
        {
            get;
        }

        public MatchType MatchType
        {
            get;
        }

        /// <summary>
        /// 命中的字符串在源字符串的索引位置
        /// </summary>
        public int MatchIndex
        {
            get;
        }

        /// <summary>
        /// 命中的全称 或 简称
        /// </summary>
        public string MatchName
        {
            get;
        }

        internal MatchRegionItem(
            Region matchRegion,
            MatchType matchType, 
            int matchIndex, 
            string matchName)
        {
            MatchRegion = matchRegion;
            MatchType = matchType;
            MatchIndex = matchIndex;
            MatchName = matchName;
        }

        public override string ToString()
        {
            return $"MatchType:{MatchType}, MatchIndex:{MatchIndex}, MatchName:{MatchName}, MatchRegion:{MatchRegion}";// this.MatchRegion.ToString();
        }
    }
}
