using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AddressParsing
{
    public static class AddressParser
    {
        public static ReadOnlyCollection<Region> Regions
        {
            get;
            private set;
        }

        public static ReadOnlyCollection<int> SortedLevels
        {
            get;
            private set;
        }

        public static Dictionary<int, ReadOnlyCollection<Region>> RegionsByLevel
        {
            get;
            private set;
        }

        /// <summary>
        /// 地址常用分割符，用来首次处理地址时移除
        /// </summary>
        internal static char[] SplitterChars { get; } = new char[]
        {
            '~','!','@','#','$','%','^','&','*','(',')','-','+','_','=',':',';','\'','"','?','|','\\','{','}','[',']','<','>',',','.',' ',
            '！','￥','…','（','）','—','【','】','、','：','；','“','’','《','》','？','，','　'
        };

        /// <summary>
        /// 非三级地区常用后缀和前缀
        /// </summary>
        internal static string[] RegionInvalidSuffix { get; } = new string[]
        {
            "街", "路", "村", "弄", "幢", "号", "道",
            "大厦", "工业", "产业", "广场", "科技", "公寓", "中心", "小区", "花园", "大道", "农场",
            "0","1","2","3","4","5","6","7","8","9",
            "０","１","２","３","４","５","６","７","８","９",
            "A","B","C","D","E","F","G","H","I","J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "a","b","c","d","e","f","g","h","i","j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
            "ａ","ｂ","ｃ","ｄ","ｅ","ｆ","ｇ","ｈ","ｉ","ｊ", "ｋ", "ｌ", "ｍ", "ｎ", "ｏ", "ｐ", "ｑ", "ｒ", "ｓ", "ｔ", "ｕ", "ｖ", "ｗ", "ｘ", "ｙ", "ｚ",

            //"沟", "屯", "坡", "组", "庄", "苑", "墅", "寓",
        };

        static AddressParser()
        {
            var regions = ReadRegionsFile();
            Regions = new ReadOnlyCollection<Region>(JsonConvert.DeserializeObject<List<Region>>(regions));
            RegionsByLevel = Regions.GroupBy(_p => _p.Level)
                                            .ToDictionary(
                                                _p => _p.Key,
                                                _p => new ReadOnlyCollection<Region>(_p.ToList())
                                            );
            SortedLevels = new ReadOnlyCollection<int>(RegionsByLevel.Keys.OrderBy(_p => _p).ToList());
            BuildRelation();
            BuildPathNames();
        }

        private static string ReadRegionsFile()
        {
            using (Stream sm = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{Assembly.GetExecutingAssembly().GetName().Name}.Regions.json"))
            {
                if (sm == null)
                {
                    return string.Empty;
                }
                byte[] bs = new byte[sm.Length];
                sm.Read(bs, 0, (int)sm.Length);
                return Encoding.Default.GetString(bs);
            }
        }

        private static void BuildRelation()
        {
            foreach (var item in RegionsByLevel)
            {
                if (RegionsByLevel.TryGetValue(item.Key + 1, out var children)
                    && children != null)
                {
                    foreach (var region in item.Value)
                    {
                        region.Children = children.Where(_p => _p.ParentID == region.ID).ToList();
                    }
                }

                if (RegionsByLevel.TryGetValue(item.Key - 1, out var parents)
                    && parents != null)
                {
                    var parentdict = parents.ToDictionary(_p => _p.ID);

                    foreach (var region in item.Value)
                    {
                        if (parentdict.TryGetValue(region.ParentID, out var parent))
                        {
                            region.Parent = parent;
                        }
                    }
                }
            }
        }

        private static void BuildPathNames()
        {
            foreach (var item in Regions.Where(_p => _p.Parent != null))
            {
                item.PathNames = item.BuildPathNames()
                                                .Except(item.ShortNames)
                                                .Except(new string[1] { item.Name })
                                                .OrderByDescending(_p => _p.Length)
                                                .ToArray();
            }
        }


        public static string FinalCut(
            RegionMatchResult matchresult,
            string address)
        {
            if (matchresult != null
                && matchresult.PathEndItem != null
                && matchresult.SourceItems != null)
            {
                foreach (var matchitem in matchresult
                                                                .SourceItems
                                                                .OrderByDescending(_p => _p.MatchIndex))
                {
                    var newindex = address.IndexOf(matchitem.MatchName);
                    if (newindex >= 0)
                    {
                        address = address.Remove(newindex, matchitem.MatchName.Length);
                    }
                }

                return $"{matchresult.PathEndItem.MatchRegion.GetFullPathText()} - {address}";
            }

            return address;
        }


        public static List<RegionMatchResult> ParsingAddress(
            string address)
        {
            ExtensionMethods.RemoveChars(ref address, SplitterChars);

            List<MatchRegionItem> matchitems = new List<MatchRegionItem>();

            MatchRoughly(
                RegionsByLevel[AddressParser.SortedLevels.Min()],
                ref address,
                0,
                matchitems);

            if (matchitems.Count > 0)
            {
                MatchPathName(
                    matchitems.Where(_p => _p.MatchRegion.Parent != null)
                            .Select(_p => _p.MatchRegion),
                    ref address,
                    out List<MatchRegionItem> fullnamematch);

                matchitems.InsertRange(0, fullnamematch);
            }

            return MergeAndSort(matchitems);
        }


        private static List<RegionMatchResult> MergeAndSort(
            List<MatchRegionItem> matchitems)
        {
            var matchresults = new List<RegionMatchResult>();

            var fullnamematch = matchitems
                                                .Where(
                                                    _p => _p.MatchType == MatchType.PathName
                                                )
                                                .ToArray();
            if (fullnamematch.Length > 0)
            {
                Merge(ref matchresults, fullnamematch);
            }
            else
            {
                var namematch = matchitems
                                                .Where(
                                                    _p => _p.MatchType == MatchType.Name
                                                )
                                                .ToArray();
                if (namematch.Length > 0)
                {
                    Merge(ref matchresults, namematch);
                }
                else
                {
                    var shortnamematch = matchitems
                                                            .Where(
                                                                _p => _p.MatchType == MatchType.ShortName
                                                            )
                                                            .ToArray();
                    if (shortnamematch.Length > 0)
                    {
                        Merge(ref matchresults, shortnamematch);
                    }
                }
            }

            //最小索引靠前的优先，后半部分详细地址造成误配概率大
            if (matchresults.Count > 1)
            {
                var minindex = matchresults.MinGroup(_p => _p.PathEndItem.MatchIndex);
                if (minindex.Count > 0)
                {
                    matchresults = minindex;
                }
            }


            //命中字数多的优先
            if (matchresults.Count > 1)
            {
                var strcount = matchresults.MaxGroup(_p => _p.PathEndItem.MatchName.Length);
                if (strcount.Count > 0)
                {
                    matchresults = strcount;
                }
            }

            //命中的等级越低的越优先？？？
            //若二级区域和其它二级区域下的三级区域简称重名，那么一般指小等级
            //西安某某某大厦
            //if (matchresults.Count > 1)
            //{
            //    var level = matchresults.MinGroup(_p => _p.PathEndItem.MatchRegion.Level);
            //    if (level.Count > 0)
            //    {
            //        matchresults = level;
            //    }
            //}

            return matchresults;
        }


        private static bool Merge(
            ref List<RegionMatchResult> matchresults,
            IEnumerable<MatchRegionItem> matchitemscope)
        {
            var fulllevelgroup = matchitemscope.GroupBy(_p => _p.MatchRegion.Level)
                                                    .ToDictionary(
                                                        _p => _p.Key,
                                                        _p => _p.ToList()
                                                    );

            bool result = false;

            foreach (var key in fulllevelgroup.Keys.OrderByDescending(_p => _p))
            {
                foreach (var matchitem in fulllevelgroup[key])
                {
                    //TODO 全称也分：全称组合、短称组合、全短称组合
                    var exists = matchresults.Where(_p => _p.PathEndItem.MatchRegion.PathContains(matchitem.MatchRegion));
                    if (!exists.Any())
                    {
                        matchresults.Add(new RegionMatchResult(matchitem));
                        result = true;
                    }
                    else
                    {
                        foreach (var item in exists)
                        {
                            Debug.Assert(
                                item.SourceItems
                                    .Select(_p => _p.MatchRegion)
                                    .All(_p => _p.PathContains(matchitem.MatchRegion)),
                                "按序排列的Path依次添加时，存在的Result下的Region必须全部PathContains"
                            );

                            item.Weight++;
                            item.SourceItems.Add(matchitem);
                        }
                    }
                }
            }

            if (result)
            {
                matchresults = matchresults.MaxGroup(_p => _p.Weight);
            }

            return result;
        }


        private static void MatchPathName(
            IEnumerable<Region> regionscope,
            ref string address,
            out List<MatchRegionItem> fullnamematch)
        {
            fullnamematch = new List<MatchRegionItem>();
            foreach (var regionitem in regionscope)
            {
                if (regionitem.PathNames != null)
                {
                    for (int i = 0; i < regionitem.PathNames.Length; i++)
                    {
                        var index = address.IndexOf(regionitem.PathNames[i]);
                        if (index >= 0)
                        {
                            fullnamematch.Add(
                                new MatchRegionItem(
                                    regionitem,
                                    MatchType.PathName,
                                    index,
                                    regionitem.PathNames[i]
                                ));

                            break;
                        }
                    }
                }
            }
        }


        private static void MatchRoughly(
            IList<Region> regionscope,
            ref string address,
            int startindex,
            List<MatchRegionItem> matchitems)
        {
            if (regionscope == null
                || startindex >= address.Length)
            {
                return;
            }

            startindex = startindex >= 0 ? startindex : 0;

            for (int k = 0; k < regionscope.Count; k++)
            {
                var currentregion = regionscope[k];

                MatchRegionItem matchitem = null;

                int index = address.IndexOf(currentregion.Name, startindex);

                if (index >= 0)
                {
                    matchitem = new MatchRegionItem(
                        currentregion,
                        MatchType.Name,
                        index,
                        currentregion.Name);

                    index = index + currentregion.Name.Length;
                }

                if (matchitem == null)
                {
                    if (currentregion.ShortNames != null
                        && currentregion.ShortNames.Length > 0)
                    {
                        for (int i = 0; i < currentregion.ShortNames.Length; i++)
                        {
                            index = address.IndexOf(currentregion.ShortNames[i], startindex);

                            if (index >= 0
                                && IsMatchedNameValid(ref address, index + currentregion.ShortNames[i].Length))
                            {
                                matchitem = new MatchRegionItem(
                                    currentregion,
                                    MatchType.ShortName,
                                    index,
                                    currentregion.ShortNames[i]);

                                index = index + currentregion.ShortNames[i].Length;
                                break;
                            }
                        }
                    }
                }

                if (matchitem != null)
                {
                    matchitems.Add(matchitem);
                }

                MatchRoughly(
                    currentregion.Children,
                    ref address,
                    index,
                    matchitems);
            }
        }


        private static bool IsMatchedNameValid(
            ref string address,
            int startindex)
        {
            if (startindex >= 0
                && startindex < address.Length)
            {
                int offset = 2;
                var substr = string.Empty;

                //截取 shortname 往后和往前的 offset 个字符，若不包含非三级地址常用后缀前缀，则本次ShortName匹配成功

                if (startindex + offset > address.Length)
                {
                    substr = address.Substring(startindex);
                }
                else
                {
                    substr = address.Substring(startindex, offset);
                }
                bool valid = !RegionInvalidSuffix.Any(_p => substr.Contains(_p));

                if (!valid)
                {
                    return false;
                }

                if (startindex - offset >= 0)
                {
                    substr = address.Substring(startindex - offset, offset);
                }
                else
                {
                    substr = address.Substring(0, startindex);
                }
                return !RegionInvalidSuffix.Any(_p => substr.Contains(_p));
            }

            return true;
        }
    }
}
