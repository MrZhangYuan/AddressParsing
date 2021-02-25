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
            '~','!','@','#','$','%','^','&','(',')','-','+','_','=',':',';','\'','"','?','|','\\','{','}','[',']','<','>',',','.',' ',
            //'*',
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

            _sourceList = JsonConvert.DeserializeObject<List<Region>>(regions);
            Regions = new ReadOnlyCollection<Region>(_sourceList);
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
                        region.Children = children
                                                    .Where(_p => _p.ParentID == region.ID)
                                                    .ToList();
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

            List<MatchRegionItem> matchitems = new List<MatchRegionItem>();

            bool matchedbypath = false;
            Match(
                RegionsByLevel[AddressParser.SortedLevels[0]],
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

        private static void Match(
          IList<Region> regionscope,
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

            for (int k = 0; k < regionscope.Count; k++)
            {
                if (matchedbypath)
                {
                    return;
                }

                var currentregion = regionscope[k];
                int index = -1;
                MatchType? matchtype = null;
                string matchname = string.Empty;

                {
                    var matchindex = address.IndexOf(currentregion.Name[0], startindex);
                    if (matchindex >= 0)
                    {
                        matchindex = address.IndexOf(
                            currentregion.Name,
                            startindex,
                            StringComparison.Ordinal);

                        if (matchindex >= 0)
                        {
                            index = matchindex;
                            matchtype = MatchType.Name;
                            matchname = currentregion.Name;
                        }
                    }
                }

                if (!matchtype.HasValue
                    && currentregion.ShortNames != null)
                {
                    for (int i = 0; i < currentregion.ShortNames.Length; i++)
                    {
                        var matchindex = address.IndexOf(currentregion.ShortNames[i][0], startindex);
                        if (matchindex >= 0)
                        {
                            matchindex = address.IndexOf(
                                currentregion.ShortNames[i],
                                startindex,
                                StringComparison.Ordinal);

                            if (matchindex >= 0
                                && IsMatchedNameValid(ref address, matchindex, currentregion.ShortNames[i].Length))
                            {
                                index = matchindex;
                                matchtype = MatchType.ShortName;
                                matchname = currentregion.ShortNames[i];
                                break;
                            }
                        }
                    }
                }

                if (matchtype.HasValue
                    && currentregion.PathNames != null)
                {
                    for (int i = 0; i < currentregion.PathNames.Length; i++)
                    {
                        var matchindex = address.IndexOf(currentregion.PathNames[i][0]);
                        if (matchindex >= 0)
                        {
                            matchindex = address.IndexOf(currentregion.PathNames[i], StringComparison.Ordinal);

                            if (matchindex >= 0)
                            {
                                index = matchindex;
                                matchtype = MatchType.PathName;
                                matchname = currentregion.PathNames[i];

                                if (currentregion.Level == SortedLevels[SortedLevels.Count - 1])
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
                                    currentregion,
                                    matchtype.Value,
                                    index,
                                    matchname
                                ));

                    index += matchname.Length;
                }

                Match(
                    currentregion.Children,
                    ref matchedbypath,
                    ref address,
                    index,
                    matchitems);
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
