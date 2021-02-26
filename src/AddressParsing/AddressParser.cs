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
        private static List<Region> _sourceList = null;
        private static Dictionary<int, ReadOnlyCollection<Region>> _regionsByLevel = null;
        private static string[] _rootLevelShortestShortName = null;
        private static int[] _sortedLevels = null;

        /// <summary>
        /// 所有的区划
        /// </summary>
        public static ReadOnlyCollection<Region> Regions
        {
            get;
            private set;
        }

        /// <summary>
        /// 按区划等级分组的字典
        /// </summary>
        public static ReadOnlyDictionary<int, ReadOnlyCollection<Region>> RegionsByLevel
        {
            get;
            private set;
        }

        /// <summary>
        /// 地址常用分割符，用来首次处理地址时移除
        /// </summary>
        private static char[] SplitterChars { get; } = new char[]
        {
            '~','!','@','#','$','%','^','&','(',')','-','+','_','=',':',';','\'','"','?','|','\\','{','}','[',']','<','>',',','.',' ',
            //'*',
            '！','￥','…','（','）','—','【','】','、','：','；','“','’','《','》','？','，','　'
        };

        /// <summary>
        /// 非三级地区常用后缀和前缀
        /// </summary>
        private static string[] RegionInvalidSuffix { get; } = new string[]
        {
            "街", "路", "村", "弄", "幢", "号", "道",
            "大厦", "工业", "产业", "广场", "科技", "公寓", "中心", "小区", "花园", "大道", "农场",
            "0","1","2","3","4","5","6","7","8","9",
            "０","１","２","３","４","５","６","７","８","９",
            "A","B","C","D","E","F","G","H","I","J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "a","b","c","d","e","f","g","h","i","j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
            "ａ","ｂ","ｃ","ｄ","ｅ","ｆ","ｇ","ｈ","ｉ","ｊ", "ｋ", "ｌ", "ｍ", "ｎ", "ｏ", "ｐ", "ｑ", "ｒ", "ｓ", "ｔ", "ｕ", "ｖ", "ｗ", "ｘ", "ｙ", "ｚ"
        };

        static AddressParser()
        {
            var regions = ReadRegionsFile();
            _sourceList = JsonConvert.DeserializeObject<List<Region>>(regions);
            Regions = new ReadOnlyCollection<Region>(_sourceList);
            _regionsByLevel = Regions.GroupBy(_p => _p.Level)
                                    .ToDictionary(
                                        _p => _p.Key,
                                        _p => new ReadOnlyCollection<Region>(_p.ToList())
                                    );
            RegionsByLevel = new ReadOnlyDictionary<int, ReadOnlyCollection<Region>>(_regionsByLevel);
            _sortedLevels = RegionsByLevel.Keys.OrderBy(_p => _p).ToArray();
            _rootLevelShortestShortName = RegionsByLevel[_sortedLevels[0]]
                                        .Select(_p => _p.ShortNames[_p.ShortNames.Length - 1])
                                        .ToArray();
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
                return Encoding.UTF8.GetString(bs);
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
                        var currentchildren = children
                                            .Where(_p => _p.ParentID == region.ID)
                                            .ToList();
                        region.Children = new ReadOnlyCollection<Region>(currentchildren);

                        region.ChildrenShortestNames = currentchildren
                                                    .Where(
                                                        _p => _p.ShortNames != null
                                                                && _p.ShortNames.Length > 0
                                                    )
                                                    .Select(
                                                        _p => _p.ShortNames[_p.ShortNames.Length - 1]
                                                    )
                                                    .ToArray();
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

        public static void MakePrioritySort(Func<Region, int> selector)
        {
            if (selector != null)
            {
                var rootlevel = AddressParser._sortedLevels[0];
                var roots = RegionsByLevel[rootlevel]
                            .OrderBy(selector)
                            .ToList();

                _regionsByLevel[rootlevel] = new ReadOnlyCollection<Region>(roots);

                _rootLevelShortestShortName = RegionsByLevel[rootlevel]
                                            .Select(_p => _p.ShortNames[_p.ShortNames.Length - 1])
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
                    var newindex = address.IndexOf(matchitem.MatchName, StringComparison.Ordinal);
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

            if (string.IsNullOrEmpty(address)
                || address.Length < 2)
            {
                return new List<RegionMatchResult>(0);
            }

            List<MatchRegionItem> matchitems = new List<MatchRegionItem>();

            bool matchedbypath = false;
            Match(
                RegionsByLevel[AddressParser._sortedLevels[0]],
                QuickSort(ref address, 0, _rootLevelShortestShortName),
                ref matchedbypath,
                ref address,
                0,
                matchitems);

            if (matchedbypath
                || matchitems.Count == 1)
            {
                return new List<RegionMatchResult>(1)
                {
                    new RegionMatchResult(matchitems[matchitems.Count - 1])
                };
            }

            return MergeAndSort(matchitems);
        }


        private static List<RegionMatchResult> MergeAndSort(
            List<MatchRegionItem> matchitems)
        {
            var matchresults = new List<RegionMatchResult>();

            var namematch = matchitems
                            .Where(_p => _p.MatchType == MatchType.Name)
                            .ToArray();
            if (namematch.Length > 0)
            {
                Merge(ref matchresults, namematch);
            }
            else
            {
                var shortmatch = matchitems
                                .Where(_p => _p.MatchType == MatchType.ShortName)
                                .ToArray();
                if (shortmatch.Length > 0)
                {
                    Merge(ref matchresults, shortmatch);
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

            //命中的等级越低的越优先：同时命中西安，取西安市而非西安区
            if (matchresults.Count > 1)
            {
                var level = matchresults.MinGroup(_p => _p.PathEndItem.MatchRegion.Level);
                if (level.Count > 0)
                {
                    matchresults = level;
                }
            }

            return matchresults;
        }


        private static bool Merge(
            ref List<RegionMatchResult> matchresults,
            IEnumerable<MatchRegionItem> matchitemscope)
        {
            var fulllevelgroup = matchitemscope
                                .GroupBy(_p => _p.MatchRegion.Level)
                                .ToDictionary(
                                    _p => _p.Key,
                                    _p => _p.ToList()
                                );

            bool result = false;

            foreach (var key in fulllevelgroup.Keys.OrderByDescending(_p => _p))
            {
                foreach (var matchitem in fulllevelgroup[key])
                {
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


        private static int QuickSort(ref string address, int startindex, string[] shortnames)
        {
            if (shortnames == null
                || shortnames.Length == 0)
            {
                return 0;
            }

            startindex = startindex >= 0 && startindex < address.Length ? startindex : 0;

            int length = address.Length >= 3 ? 3 : 2;

            for (int i = 0; i < shortnames.Length; i++)
            {
                var matchindex = address.IndexOf(
                    shortnames[i][0],
                    startindex,
                    length);

                if (matchindex >= 0)
                {
                    matchindex = address.IndexOf(
                        shortnames[i],
                        startindex,
                        length,
                        StringComparison.Ordinal);

                    if (matchindex >= 0)
                    {
                        return i;
                    }
                }
            }
            return 0;
        }


        private static void Match(
              IList<Region> regionscope,
              int loopstartindex,
              ref bool matchedbypath,
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
            int loopcount = regionscope.Count;
            bool handled = false;

            for (int k = loopstartindex; k < loopcount; k++)
            {
                if (matchedbypath)
                {
                    return;
                }

                if (handled
                    && k == loopstartindex)
                {
                    continue;
                }

                var current = regionscope[k];
                int index = -1;
                MatchType? matchtype = null;
                string matchname = string.Empty;

                {
                    var matchindex = address.IndexOf(current.Name[0], startindex);
                    if (matchindex >= 0)
                    {
                        matchindex = address.IndexOf(
                            current.Name,
                            startindex,
                            StringComparison.Ordinal);

                        if (matchindex >= 0)
                        {
                            index = matchindex;
                            matchtype = MatchType.Name;
                            matchname = current.Name;
                        }
                    }
                }

                if (!matchtype.HasValue
                    && current.ShortNames != null)
                {
                    for (int i = 0; i < current.ShortNames.Length; i++)
                    {
                        var matchindex = address.IndexOf(current.ShortNames[i][0], startindex);
                        if (matchindex >= 0)
                        {
                            matchindex = address.IndexOf(
                                current.ShortNames[i],
                                startindex,
                                StringComparison.Ordinal);

                            if (matchindex >= 0
                                && IsMatchedNameValid(ref address, matchindex, current.ShortNames[i].Length))
                            {
                                index = matchindex;
                                matchtype = MatchType.ShortName;
                                matchname = current.ShortNames[i];
                                break;
                            }
                        }
                    }
                }

                if (matchtype.HasValue
                    && current.PathNames != null)
                {
                    for (int i = 0; i < current.PathNames.Length; i++)
                    {
                        var matchindex = address.IndexOf(current.PathNames[i][0]);
                        if (matchindex >= 0)
                        {
                            matchindex = address.IndexOf(current.PathNames[i], StringComparison.Ordinal);

                            if (matchindex >= 0)
                            {
                                index = matchindex;
                                matchtype = MatchType.PathName;
                                matchname = current.PathNames[i];

                                if (current.Level == _sortedLevels[_sortedLevels.Length - 1])
                                {
                                    matchedbypath = true;
                                }
                                break;
                            }
                        }
                    }
                }

                if (matchtype.HasValue)
                {
                    matchitems.Add(new MatchRegionItem(
                                    current,
                                    matchtype.Value,
                                    index,
                                    matchname
                                ));

                    index += matchname.Length;
                }

                Match(
                    current.Children,
                    index > 0
                        && current.Level < _sortedLevels[_sortedLevels.Length - 1] ? QuickSort(ref address, index, current.ChildrenShortestNames) : 0,
                    ref matchedbypath,
                    ref address,
                    index,
                    matchitems);

                //QuickSort确定最先处理的索引
                //若索引不是从0开始的，确保处理完当前索引，再从0开始
                if (!handled
                    && loopstartindex > 0
                    && k == loopstartindex)
                {
                    handled = true;
                    k = -1;
                }
            }
        }



        private static bool IsMatchedNameValid(
            ref string address,
            int startindex,
            int matchlength)
        {
            if (startindex >= 0
                && startindex < address.Length)
            {
                int offset = 2;
                var substr = string.Empty;

                //截取 shortname 往后和往前的 offset 个字符，若不包含非三级地址常用后缀前缀，则本次ShortName匹配成功

                if (startindex + matchlength + offset > address.Length)
                {
                    substr = address.Substring(startindex);
                }
                else
                {
                    substr = address.Substring(startindex + matchlength, offset);
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
