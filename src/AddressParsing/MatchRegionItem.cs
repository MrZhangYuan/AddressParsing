using System;

namespace AddressParsing
{
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
            return $"MatchType:\t{MatchType}{Environment.NewLine}MatchIndex:\t{MatchIndex}{Environment.NewLine}MatchName:\t{MatchName}{Environment.NewLine}MatchRegion:\t{MatchRegion}";// this.MatchRegion.ToString();
        }
    }
}
