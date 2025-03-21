using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AddressParsing
{
    public static class BasicData
    {
        private static readonly object _initLocker = new object();

        /// <summary>
        ///     正序排列的区划等级，通常为固定值 [1,2,3]
        /// </summary>
        internal static int[] SortedLevels
        {
            get;
            private set;
        }

        /// <summary>
        ///     提供自定义数据源的方案，请在首次调用前赋值
        /// </summary>
        public static Func<List<Region>> RegionsCreater
        {
            get;
            set;
        }

        /// <summary>
        ///     在建立缓存之前做一些需要的变动，如：优先级排序、行政区划代码设置、邮编设置
        /// </summary>
        public static Action<Region> OnBuilding
        {
            get;
            set;
        }


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


        internal static void Build()
        {
            if (Regions == null)
            {
                lock (_initLocker)
                {
                    if (Regions == null)
                    {
                        if (RegionsCreater != null)
                        {
                            var regions = RegionsCreater();
                            if (regions != null)
                            {
                                Regions = new ReadOnlyCollection<Region>(regions);
                            }
                        }

                        if (Regions == null)
                        {
                            var region_txt = ReadRegionsFile();
                            var regions = JsonConvert.DeserializeObject<List<Region>>(region_txt);
                            Regions = new ReadOnlyCollection<Region>(regions);
                        }

                        if (OnBuilding != null)
                        {
                            for (int i = 0; i < Regions.Count; i++)
                            {
                                if (Regions[i].Level == 1)
                                {
                                    OnBuilding(Regions[i]);
                                }
                            }
                        }

                        var reg_level = Regions.GroupBy(_p => _p.Level)
                                                .ToDictionary(
                                                    _p => _p.Key,
                                                    _p => new ReadOnlyCollection<Region>(_p.ToList())
                                                );

                        SortedLevels = reg_level.Keys.OrderBy(_p => _p).ToArray();

                        if (SortedLevels.Length != 3)
                        {
                            throw new ArgumentException("自定义 RegionsCreater 返回数据无效，应包含三级区划 Level");
                        }

                        RegionsByLevel = MakePrioritySort(reg_level);

                        BuildRootLevelIndex();
                        BuildRelation();
                        BuildPathInfo();
                    }
                }
            }
        }

        private static string ReadRegionsFile()
        {
            var curpath = Path.Combine(Directory.GetCurrentDirectory(), "AddressParsingRegions.json");
            if (File.Exists(curpath))
            {
                return File.ReadAllText(curpath);
            }

            using (Stream sm = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{Assembly.GetExecutingAssembly().GetName().Name}.Internal.Regions.json"))
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

        private static void BuildRootLevelIndex()
        {
            //  第一级 Region 的 IndexOfParent 设定为在根集合的索引
            var rootregions = RegionsByLevel[SortedLevels[0]];
            for (int i = 0; i < rootregions.Count; i++)
            {
                rootregions[i].IndexOfParent = i;
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


        private static void BuildPathInfo()
        {
            foreach (var item in Regions.Where(_p => _p.Parent != null))
            {
                item.IndexOfParent = item.Parent.Children.IndexOf(item);

                item.PathNames = item.BuildPathNames()
                                .Except(item.ShortNames)
                                .Except(new string[1] { item.Name })
                                .OrderByDescending(_p => _p.Length)
                                .ToArray();

                item.PathFullSpells = UtilMethods.CheckFullSpell(
                                            item.BuildPathSpells()
                                            .Except(item.ShortNamesSpell)
                                            .Except(new string[1] { item.NameSpell })
                                        );

                item.PathLetters = UtilMethods.ConvertUpperLetters(
                                                string.Join("", item.PathFullSpells)
                                            );

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
        private static ReadOnlyDictionary<int, ReadOnlyCollection<Region>> MakePrioritySort(Dictionary<int, ReadOnlyCollection<Region>> reg_level)
        {
            var rootlevel = SortedLevels[0];
            var roots = reg_level[rootlevel]
                        .OrderBy(_p => _p.PrioritySort)
                        .ToList();
            reg_level[rootlevel] = new ReadOnlyCollection<Region>(roots);

            return new ReadOnlyDictionary<int, ReadOnlyCollection<Region>>(reg_level);
        }
    }
}
