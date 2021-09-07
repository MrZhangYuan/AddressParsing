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
    /// <summary>
    ///     AddressParsing 算法，将常见的地址数据：家庭地址、工作地址等归一化
    ///     归一到 省 - 市 - 区 
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         使用方法 <see cref="MakePrioritySort(Func{Region, int})" /> 对内部区划字典匹配优先级配置
    ///     </para>
    ///     <para>
    ///         使用方法 <see cref="ParsingAddress(string)" /> 对指定地址进行三级区划 省 - 市 - 区 匹配
    ///     </para>
    ///     <para>
    ///         使用方法 <see cref="FinalCut(RegionMatchResult, string)" /> 对命中的结果进行裁剪，得到展示友好的字符串：
    ///         如："上海市 - 上海市 - 闵行区 - 浦江镇恒南路899号"
    ///     </para>
    /// </remarks>
    public static class AddressParser
    {
        private static List<Region> _sourceList = null;
        private static Dictionary<int, ReadOnlyCollection<Region>> _regionsByLevel = null;
        private static int[] _sortedLevels = null;
        private static int[] _topLevel2KeySortedLength = null;
        private static readonly Dictionary<string, List<Region>> _topLevel2KeyedIndex = new Dictionary<string, List<Region>>();

#if DEBUG
        public class Static
        {
            public int CallStringIndexOfTimes { get; set; }
            public int CallMatchTimes { get; set; }
            public int CallIsMatchedNameValidTimes { get; set; }
            public int MatchLoopTimes { get; set; }
            public int PathNameSkip { get; set; }

            public void Reset()
            {
                this.CallStringIndexOfTimes = 0;
                this.CallMatchTimes = 0;
                this.CallIsMatchedNameValidTimes = 0;
                this.MatchLoopTimes = 0;
                this.PathNameSkip = 0;
            }

            public override string ToString()
            {
                return $"{nameof(CallStringIndexOfTimes)}：{CallStringIndexOfTimes}{Environment.NewLine}"
                    + $"{nameof(CallMatchTimes)}：{CallMatchTimes}{Environment.NewLine}"
                    + $"{nameof(CallIsMatchedNameValidTimes)}：{CallIsMatchedNameValidTimes}{Environment.NewLine}"
                    + $"{nameof(MatchLoopTimes)}：{MatchLoopTimes}{Environment.NewLine}"
                    + $"{nameof(PathNameSkip)}：{PathNameSkip}{Environment.NewLine}";
            }
        }
#endif

#if DEBUG
        public static Static Statics
        {
            get;
            private set;
        }
#endif

        /// <summary>
        ///     所有的区划
        /// </summary>
        public static ReadOnlyCollection<Region> Regions
        {
            get;
            private set;
        }


        /// <summary>
        ///     按区划等级分组的只读字典
        /// </summary>
        public static ReadOnlyDictionary<int, ReadOnlyCollection<Region>> RegionsByLevel
        {
            get;
            private set;
        }


        /// <summary>
        ///     地址常用分割符，用来首次处理地址时移除
        /// </summary>
        private static char[] SplitterChars { get; } = new char[]
        {
            '~','!','@','#','$',
            '%','^','&','(',')',
            '-','+','_','=',':',
            ';','\'','"','?','|',
            '\\','{','}','[',']',
            '<','>',',','.',' ',
            '！','￥','…','（','）',
            '—','【','】','、','：',
            '；','“','’','《','》',
            '？','，','　'
            //'*',
        };


        /// <summary>
        ///     非三级地区常用后缀和前缀
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

            BuildRootLevelIndex();
            BuildRelation();
            BuildPathNameAndSkips();
            BuildTopLevel2QuickIndex();

#if DEBUG
            Statics = new Static();
#endif
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


        private static void BuildPathNameAndSkips()
        {
            foreach (var item in Regions.Where(_p => _p.Parent != null))
            {
                item.IndexOfParent = item.Parent.Children.IndexOf(item);

                item.PathNames = item.BuildPathNames()
                                .Except(item.ShortNames)
                                .Except(new string[1] { item.Name })
                                .OrderByDescending(_p => _p.Length)
                                .ToArray();

                item.PathNameSkip = new int[item.PathNames.Length];

                for (int i = 0; i < item.PathNames.Length; i++)
                {
                    var ch = item.PathNames[i][0];

                    for (int j = i + 1; j < item.PathNames.Length; j++)
                    {
                        var nextch = item.PathNames[j][0];
                        if (ch == nextch)
                        {
                            item.PathNameSkip[i]++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            foreach (var item in Regions)
            {
                if (item.ShortNames != null)
                {
                    item.ShortNameSkip = new bool[item.ShortNames.Length];

                    var ch = item.Name[0];

                    for (int i = 0; i < item.ShortNames.Length; i++)
                    {
                        if (item.ShortNames[i][0] == ch)
                        {
                            item.ShortNameSkip[i] = true;
                        }
                    }
                }
            }
        }


        private static void BuildTopLevel2QuickIndex()
        {
            var level1and2 = RegionsByLevel[_sortedLevels[0]].Concat(RegionsByLevel[_sortedLevels[1]]);

            foreach (var region in level1and2)
            {
                if (region.ShortNames != null
                    && region.ShortNames.Length > 0)
                {
                    var matchkeys = region.ShortNames.ToList();
                    matchkeys.Add(region.Name);

                    for (int i = 0; i < matchkeys.Count; i++)
                    {
                        if (_topLevel2KeyedIndex.TryGetValue(matchkeys[i], out var exists))
                        {
                            //  exists 其实是个有序的 List， Level 小的放前面
                            //  以便在快速匹配索引时提升准确度
                            bool inserted = false;
                            for (int j = 0; j < exists.Count; j++)
                            {
                                if (region.Level <= exists[j].Level)
                                {
                                    exists.Insert(j, region);
                                    inserted = true;
                                    break;
                                }
                            }

                            if (!inserted)
                            {
                                exists.Add(region);
                            }
                        }
                        else
                        {
                            _topLevel2KeyedIndex.Add(matchkeys[i], new List<Region> { region });
                        }
                    }
                }
            }

            _topLevel2KeySortedLength = _topLevel2KeyedIndex.Keys
                                    .Select(_p => _p.Length)
                                    .Distinct()
                                    .OrderBy(_p => _p)
                                    .ToArray();
        }


        private static void BuildRootLevelIndex()
        {
            //  第一级 Region 的 IndexOfParent 设定为在根集合的索引
            var rootregions = _regionsByLevel[_sortedLevels[0]];
            for (int i = 0; i < rootregions.Count; i++)
            {
                rootregions[i].IndexOfParent = i;
            }
        }


        /// <summary>
        ///     <para>
        ///         顶级 <see cref="Region" /> 匹配优先级设置，算法内部将按照指定顺序对内部字典进行匹配
        ///         设置优先级有助于针对性的提升算法性能，如：
        ///         数据库地址全是“上海市”开头的地址，那么配置上海市优先，可将性能提升 10 ~ 20 倍
        ///         注意：
        ///             此方法并不是线程安全的，应该在首次匹配地址之前调用，且地址匹配期间不应再次调用，否则易产生意想不到的结果
        ///     </para>
        ///     <code>
        ///         //将“上海市”标记为匹配的最高优先级，当算法内部索引快速命中 <see cref="IndexQuickMatch(ref string, int, string[])" /> 
        ///         //没有命中索引时，优先处理的顶级 <see cref="Region" />
        ///         //若是命中了索引，则以算法命中的为优先，该配置优先级次于算法
        ///         //参数可针对 <see cref="Region.Name" /> 或 <see cref="Region.ID" /> 进行更多的顺序配置，只需要返回相对顺序即可
        ///         AddressParser.MakePrioritySort(_p => _p.Name == "上海市" ? 0 : 1);
        ///     </code>
        /// </summary>
        /// <param name="selector"> 对给定的 <see cref="Region" /> 返回一个序号，该序号作为内部顶级区划的相对顺序，默认顺序“0” </param>
        public static void MakePrioritySort(Func<Region, int> selector)
        {
            if (selector != null)
            {
                var rootlevel = AddressParser._sortedLevels[0];
                var roots = RegionsByLevel[rootlevel]
                            .OrderBy(selector)
                            .ToList();

                _regionsByLevel[rootlevel] = new ReadOnlyCollection<Region>(roots);

                BuildRootLevelIndex();
            }
        }


        /// <summary>
        ///     <para>
        ///         根据匹配结果 <see cref="RegionMatchResult" /> 对指定地址进行裁剪处理
        ///         该方法存在的问题：
        ///             由于 <see cref="ParsingAddress(string)" /> 方法内部首先对指定地址进行了常用分割符移除处理
        ///             所以此方法可能不能进行有效的剪裁，如原地址为：[上海市][闵行区]浦江镇恒南路899号
        ///             那么在移除‘[’和‘]’后，其实是处理的地址：上海市闵行区浦江镇恒南路899号，这个地址其实是会 <see cref="MatchType.PathName" /> 
        ///             命中字符为”上海市闵行区“，该地址在进行最后裁剪时，无法找到，所以最后结果为：
        ///             上海市 - 上海市 - 闵行区 - [上海市][闵行区]浦江镇恒南路899号
        ///     </para>
        ///     <code>
        ///         var address = "上海市闵行区浦江镇恒南路899号";
        ///         var matchitems = AddressParser.ParsingAddress(address);
        ///         var finaladdress = AddressParser.FinalCut(matchitems[0], address);
        ///         Console.WriteLine(finaladdress);
        ///         //输出：上海市 - 上海市 - 闵行区 - 浦江镇恒南路899号
        ///     </code>
        /// </summary>
        /// <param name="matchresult"> 算法匹配到的结果项 </param>
        /// <param name="address"> 指定的地址，如："上海市闵行区浦江镇恒南路899号" </param>
        /// <returns> 裁剪后的地址，如："上海市 - 上海市 - 闵行区 - 浦江镇恒南路899号" </returns>
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


        /// <summary>
        ///     <para>
        ///         匹配地址，算法整体分为三个步骤：
        ///         1：修正，将地址中的一些特殊字符 <see cref="SplitterChars" /> 移除掉，便于内部进行 <see cref="MatchType.PathName" /> 匹配
        ///         2：匹配，从根 <see cref="Region" /> 开始循环和递归匹配，匹配之前会调用 <see cref="IndexQuickMatch(ref string, int, string[])" /> 进行索引快速命中
        ///         3：规则，若是第2步没有 <see cref="MatchType.PathName" /> 匹配结果并且结果集数量大于1，进行规则处理 <see cref="MergeAndSort(List{MatchRegionItem})" /> 
        ///     </para>
        ///     <code>
        ///         var matchitems = AddressParser.ParsingAddress("上海市闵行区浦江镇恒南路899号");
        ///     </code>
        /// </summary>
        /// <param name="address"> 给定的需要匹配的地址，如："上海市闵行区浦江镇恒南路899号" </param>
        /// <returns>
        ///     返回 <see cref="RegionMatchResult" /> 的列表，该类型记录了一些匹配的详细信息：
        ///     <see cref="RegionMatchResult.Weight" /> 命中的路径的权重
        ///     <see cref="RegionMatchResult.PathEndItem" /> 命中的路径的终节点
        ///     <see cref="RegionMatchResult.SourceItems" /> 命中的路径中的所有命中节点
        /// </returns>
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

#if DEBUG
            Statics.Reset();
#endif

            int pathmatchlevel = 0;
            int index = 0;

            //  启用前两级索引快速命中
            //  命中：优先匹配命中 Region 的 TopParent
            //  未命中：默认从 0 开始
            if (IndexQuickMatchTopLevel2(ref address, 0, out var region, out int nextindex))
            {
                index = region.GetTopParent().IndexOfParent;
            }

            Match(
                RegionsByLevel[AddressParser._sortedLevels[0]],
                index,
                ref pathmatchlevel,
                ref address,
                0,
                matchitems);

            return MergeAndSort(matchitems);
        }


        /// <summary>
        ///     <para>
        ///         对方法 <see cref="Match(IList{Region}, int, ref bool, ref string, int, List{MatchRegionItem})" /> 产生的结果集规则处理
        ///         并根据一些规则进行优选筛选：
        ///         1：优先选择 <see cref="MatchType.Name" /> 命中的 <see cref="Region" /> 而非 <see cref="MatchType.ShortName" />
        ///         2：优先选择命中名称在原地址中索引小的，后半部分详细地址造成误配概率大
        ///         3：优先选择命中字数多的
        ///         4：优先选择命中的 <see cref="Region" /> 的 <see cref="Region.Level" /> 较小的，如：
        ///             同时命中西安，取西安市而非西安区，因为大多数下是指的西安市
        ///     </para>
        /// </summary>
        /// <param name="matchitems"> 命中项 <see cref="MatchRegionItem" /> 列表 </param>
        /// <returns>
        ///     返回 <see cref="RegionMatchResult" /> 的列表，该类型记录了一些匹配的详细信息：
        ///     <see cref="RegionMatchResult.Weight" /> 命中的路径的权重
        ///     <see cref="RegionMatchResult.PathEndItem" /> 命中的路径的终节点
        ///     <see cref="RegionMatchResult.SourceItems" /> 命中的路径中的所有命中节点
        /// </returns>
        private static List<RegionMatchResult> MergeAndSort(
            List<MatchRegionItem> matchitems)
        {
            //  首先按照 MatchType 优先级选组
            if (matchitems.Count > 1)
            {
                matchitems = matchitems.MinGroup(_p => (int)_p.MatchType);
            }

            List<RegionMatchResult> matchresults = null;

            //  合并结果
            if (matchitems.Count > 1)
            {
                matchresults = new List<RegionMatchResult>();
                Merge(ref matchresults, matchitems);
            }
            else if (matchitems.Count == 1)
            {
                matchresults = new List<RegionMatchResult>(1)
                {
                    new RegionMatchResult(matchitems[0])
                };
            }
            else
            {
                matchresults = new List<RegionMatchResult>(0);
            }

            //  最小索引靠前的优先，后半部分详细地址造成误配概率大
            if (matchresults.Count > 1)
            {
                var minindex = matchresults.MinGroup(_p => _p.PathEndItem.MatchIndex);
                if (minindex.Count > 0)
                {
                    matchresults = minindex;
                }
            }


            //  命中字数多的优先
            if (matchresults.Count > 1)
            {
                var strcount = matchresults.MaxGroup(_p => _p.PathEndItem.MatchName.Length);
                if (strcount.Count > 0)
                {
                    matchresults = strcount;
                }
            }

            //  命中的等级越低的越优先
            //  如：同时命中西安，取西安市而非西安区，因为大多数下是指的西安市
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


        /// <summary>
        ///     <para>
        ///         对算法产生的中间结果集 <see cref="MatchRegionItem" /> 列表，根据是否属于同一路径进行合并
        ///         对 <see cref="MatchRegionItem.MatchRegion" /> 属于同一路径(<see cref="Region.PathContains(Region)" />)的结果合并，
        ///         得到类型 <see cref="RegionMatchResult" /> 并且权重相加
        ///         最后取权重最高的一组结果
        ///     </para>
        /// </summary>
        /// <param name="matchresults"> 引用参数：合并结果 <see cref="RegionMatchResult" /> 列表</param>
        /// <param name="matchitemscope"> 算法中间结果 <see cref="MatchRegionItem" /> 列表 </param>
        /// <returns></returns>
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


        /// <summary>
        ///     <para>
        ///         在给定的起始位置往后的2-3（因为 <see cref="Region.ShortNames" /> 长度基本都小于3）个字符中，
        ///         快速命中指定 <see cref="Region" /> 的 <see cref="Region.ChildrenShortestNames" /> 的索引。
        ///         注意：
        ///             索引快速命中并不会改变算法的结果，只会改变算法匹配的优先级
        ///             当快速命中无法有效命中索引时，算法依旧从索引 0 开始依次匹配
        ///     </para> 
        ///     <para>
        ///         这是个内部方法，该方法可能会被更名、删除等，并且不提供任何通知
        ///     </para>
        /// </summary>
        /// <param name="address"> 指定的地址字符串 </param>
        /// <param name="startindex"> 开始匹配的位置 </param>
        /// <param name="shortnames">
        ///     <para>
        ///         指定的 <see cref="Region" /> 的 <see cref="Region.ChildrenShortestNames" /> 。
        ///     </para> 
        /// </param>
        /// <returns> 命中的 <see cref="Region.ChildrenShortestNames" /> 的索引，当未命中时返回0 </returns>
        private static int IndexQuickMatch(
            ref string address,
            int startindex,
            string[] shortnames)
        {
            if (shortnames == null
                || shortnames.Length == 0)
            {
                return 0;
            }

            startindex = startindex >= 0 && startindex < address.Length ? startindex : 0;

            var lastlength = address.Length - startindex;

            int length = lastlength >= 3 ? 3 : lastlength >= 2 ? 2 : 0;

            if (length > 0)
            {
                for (int i = 0; i < shortnames.Length; i++)
                {
                    //  首先匹配第一个字符，在第一个字符存在的情况下再去匹配整个ShortName
                    //  这对单次的匹配到的情况下性能是不好的
                    //  但多数情况下是匹配不到的，所以在实测下有性能提升

                    var namelength = shortnames[i].Length;
                    bool firsteq = false;
                    var addch = address[startindex];
                    var namech = shortnames[i][0];

                    if (namelength == 2)
                    {
                        firsteq = addch == namech
                               || address[startindex + 1] == shortnames[i][0];
                    }
                    else if (namelength == 3)
                    {
                        firsteq = addch == namech;
                    }

                    if (firsteq)
                    {
                        int matchindex = address.IndexOf(
                                        shortnames[i],
                                        startindex,
                                        length,
                                        StringComparison.Ordinal);

#if DEBUG
                        Statics.CallStringIndexOfTimes++;
#endif

                        if (matchindex >= 0)
                        {
                            return i;
                        }
                    }
                }
            }
            return 0;
        }


        /// <summary>
        ///     <para>
        ///         快速命中前两级 <see cref="Region" />
        ///         注意：
        ///             索引快速命中并不会改变算法的结果，只会改变算法匹配的优先级
        ///             当快速命中无法有效命中索引时，算法依旧从索引 0 开始依次匹配
        ///     </para> 
        ///     <para>
        ///         这是个内部方法，该方法可能会被更名、删除等，并且不提供任何通知
        ///     </para>
        /// </summary>
        /// <param name="address"> 指定的地址字符串 </param>
        /// <param name="startindex"> 开始匹配的位置 </param>
        /// <param name="region">
        ///     <para>
        ///         输出参数，命中时命中的 <see cref="Region" /> 。
        ///     </para> 
        /// </param>
        /// <param name="nextindex">
        ///     <para>
        ///         输出参数，命中时下次匹配时的地址起始索引。
        ///     </para> 
        /// </param>
        /// <returns> 是否命中前两级 <see cref="Region" /> </returns>
        private static bool IndexQuickMatchTopLevel2(
            ref string address,
            int startindex,
            out Region region,
            out int nextindex)
        {
            startindex = startindex >= 0 && startindex < address.Length ? startindex : 0;

            region = null;
            nextindex = startindex;
            var lastlength = address.Length - startindex;

            for (int i = 0; i < _topLevel2KeySortedLength.Length && _topLevel2KeySortedLength[i] <= lastlength; i++)
            {
                if (_topLevel2KeyedIndex.TryGetValue(address.Substring(startindex, _topLevel2KeySortedLength[i]), out var list))
                {
                    nextindex += _topLevel2KeySortedLength[i];
                    region = list[0];
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        ///     <para>
        ///         在给定的 <see cref="Region" /> 集合中，循环和递归 <see cref="Region.Children" /> 匹配
        ///     </para>
        ///     <para>
        ///         这是个内部方法，该方法可能会被更名、删除等，并且不提供任何通知
        ///     </para>
        /// </summary>
        /// <param name="regionscope"> 需要循环匹配的 <see cref="Region" /> 列表 </param>
        /// <param name="looppriorityindex"> 循环的优先索引 </param>
        /// <param name="pathmatchlevel"> 
        ///     <para>
        ///         引用参数： <see cref="MatchType.PathName" /> 命中的 <see cref="Region" /> 的 Level
        ///             当命中的 <see cref="Region" /> 的 Level 处于最下级时，算法直接返回
        ///     </para>
        /// </param>
        /// <param name="address"> 给定的地址 </param>
        /// <param name="startindex"> 匹配的起始位置 </param>
        /// <param name="matchitems"> 命中的 <see cref="MatchRegionItem" /> 结果集 </param>
        private static void Match(
            IList<Region> regionscope,
            int looppriorityindex,
            ref int pathmatchlevel,
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

#if DEBUG
            Statics.CallMatchTimes++;
#endif

            for (int k = looppriorityindex; k < loopcount; k++)
            {
                if (pathmatchlevel == _sortedLevels[_sortedLevels.Length - 1])
                {
                    return;
                }

                if (handled
                    && k == looppriorityindex)
                {
                    continue;
                }

#if DEBUG
                Statics.MatchLoopTimes++;
#endif

                var current = regionscope[k];
                int index = -1;
                MatchType? matchtype = null;
                string matchname = string.Empty;

                bool needskip = true;

                //  匹配优先级，优先匹配 MatchType.Name
                //  若匹配不到 MatchType.Name，则匹配 MatchType.ShortName
                //  若匹配到 MatchType.Name，不匹配 MatchType.ShortName
                //  MatchType.Name 和  MatchType.ShortName 其一，就匹配 MatchType.PathName
                //  最下级的 MatchType.PathName 匹配具有绝对的优先级，一旦匹配到，算法终止，返回结果
                if (address.IndexOf(current.Name[0], startindex) >= 0)
                {
                    var matchindex = address.IndexOf(
                                current.Name,
                                startindex,
                                StringComparison.Ordinal);
                    needskip = false;

#if DEBUG
                    Statics.CallStringIndexOfTimes++;
#endif

                    if (matchindex >= 0)
                    {
                        index = matchindex;
                        matchtype = MatchType.Name;
                        matchname = current.Name;
                    }
                }

                if (!matchtype.HasValue
                    && current.ShortNames != null)
                {
                    for (int i = 0; i < current.ShortNames.Length; i++)
                    {
                        if (needskip
                            && current.ShortNameSkip[i])
                        {
                            continue;
                        }

                        if (address.IndexOf(current.ShortNames[i][0], startindex) >= 0)
                        {
                            var matchindex = address.IndexOf(
                                            current.ShortNames[i],
                                            startindex,
                                            StringComparison.Ordinal);

#if DEBUG
                            Statics.CallStringIndexOfTimes++;
#endif

                            //  MatchType.ShortName 匹配时，会出现“匹配到，但不是”的情况：
                            //  上海市闸北区西藏南路 - 西藏
                            //  上海市闸北区南京东路 - 南京
                            //  针对这种情况，我们需要特殊处理下：
                            //  在匹配到的 MatchType.ShortName 的当前索引向前和向后2个字符中,寻找一些特殊的字符后缀 RegionInvalidSuffix
                            //  若是包含这些字符后缀，本次匹配无效
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

#if DEBUG
                        Statics.CallStringIndexOfTimes++;
#endif

                        if (address.IndexOf(current.PathNames[i][0]) >= 0)
                        {
                            var matchindex = address.IndexOf(
                                            current.PathNames[i],
                                            StringComparison.Ordinal);

#if DEBUG
                            Statics.CallStringIndexOfTimes++;
#endif

                            if (matchindex >= 0)
                            {
                                index = matchindex;
                                matchtype = MatchType.PathName;
                                matchname = current.PathNames[i];
                                pathmatchlevel = current.Level;
                                break;
                            }
                        }
                        else
                        {

#if DEBUG
                            Statics.PathNameSkip += current.PathNameSkip[i];
#endif

                            i += current.PathNameSkip[i];
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

                //  递归匹配当前 Region 的 Children
                //  若当前 Region 的 Level 非最后一级，启用 IndexQuickMatch 来快速命中下级索引
                Match(
                    current.Children,
                    index > 0
                        && current.Level < _sortedLevels[_sortedLevels.Length - 1] ? IndexQuickMatch(ref address, index, current.ChildrenShortestNames) : 0,
                    ref pathmatchlevel,
                    ref address,
                    index,
                    matchitems);

                //  looppriorityindex 最先处理的索引，当该索引处理完成后，需要0开始处理其它项
                if (!handled
                    && looppriorityindex > 0
                    && k == looppriorityindex)
                {
                    handled = true;
                    k = -1;
                }
            }
        }

        /// <summary>
        ///     从给定地址的给定位置开始向前和向后2个字符中
        ///     匹配是否不包含特殊字符  <see cref="RegionInvalidSuffix" /> 
        /// </summary>
        /// <param name="address"> 给定的地址 </param>
        /// <param name="startindex"> 匹配的起始位置 </param>
        /// <param name="matchlength"> 本次匹配到的某个 <see cref="Region.ShortNames" /> 的字符长度 </param>
        /// <returns> 本次匹配不包含 <see cref="RegionInvalidSuffix" /> 后缀 </returns>
        private static bool IsMatchedNameValid(
            ref string address,
            int startindex,
            int matchlength)
        {
            if (startindex >= 0
                && startindex < address.Length)
            {

#if DEBUG
                Statics.CallIsMatchedNameValidTimes++;
#endif
                int offset = 2;
                var substr = string.Empty;

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
