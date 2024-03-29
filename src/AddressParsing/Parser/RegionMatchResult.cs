﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace AddressParsing
{
    public class RegionMatchResult
    {
        public int Weight
        {
            get;
            internal set;
        }

        public MatchRegionItem PathEndItem
        {
            get;
        }

        public List<MatchRegionItem> SourceItems
        {
            get;
        }

        internal RegionMatchResult(MatchRegionItem pathenditem)
        {
            PathEndItem = pathenditem;
            this.SourceItems = new List<MatchRegionItem>(3) { this.PathEndItem };
            Weight = 1;
        }

        public override string ToString()
        {
            return $"Weight:\t\t{Weight}{Environment.NewLine}{PathEndItem}";
        }
    }
}
