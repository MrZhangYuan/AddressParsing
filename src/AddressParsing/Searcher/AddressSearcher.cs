using System.Collections.Generic;
using System.Linq;

namespace AddressParsing
{
    enum MyEnum
    {
        连续字符,
        区划连续
    }

    public enum SearchMode
    {
        All,
        Precise
    }

    public static class AddressSearcher
    {
        private static SearchIndex[] _sortedSearchIndexs = null;


        static AddressSearcher()
        {
            RegionData.Build();

            BuildSearchIndex();
        }


        /// <summary>
        ///     对三级区划编排一个二级索引
        ///     索引键为两个 UpperLetter 逻辑或，索引值域为 PathFullSpells 包含两个字符的三级区划集合
        /// </summary>
        private static void BuildSearchIndex()
        {
            UpperLetter[] chars = new UpperLetter[26]
            {
                UpperLetter.A,
                UpperLetter.B,
                UpperLetter.C,
                UpperLetter.D,
                UpperLetter.E,
                UpperLetter.F,
                UpperLetter.G,
                UpperLetter.H,
                UpperLetter.I,
                UpperLetter.J,
                UpperLetter.K,
                UpperLetter.L,
                UpperLetter.M,
                UpperLetter.N,
                UpperLetter.O,
                UpperLetter.P,
                UpperLetter.Q,
                UpperLetter.R,
                UpperLetter.S,
                UpperLetter.T,
                UpperLetter.U,
                UpperLetter.V,
                UpperLetter.W,
                UpperLetter.X,
                UpperLetter.Y,
                UpperLetter.Z
            };

            var maps = new Dictionary<UpperLetter, Dictionary<UpperLetter, HashSet<Region>>>();

            foreach (var okey in chars)
            {
                var dict = new Dictionary<UpperLetter, HashSet<Region>>();

                foreach (var ikey in chars)
                {
                    var set = new HashSet<Region>();
                    var ch = ikey.ToString()[0];

                    foreach (var region in RegionData.RegionsByLevel[RegionData.SortedLevels.Last()])
                    {
                        /*
                        if (okey == ikey
                            && region.PathFullSpells.Any(_p => _p.Count(_p1 => _p1 == ch) >= 2))
                        {
                            set.Add(region);
                        }
                        else
                        {
                            if (region.PathLetters.Contains(okey | ikey))
                            {
                                set.Add(region);
                            }
                        }
                        */
                    }

                    dict[ikey] = set;
                }

                maps[okey] = dict;
            }


            List<SearchIndex> indices = new List<SearchIndex>();

            foreach (var outitem in maps)
            {
                foreach (var initem in outitem.Value)
                {
                    indices.Add(new SearchIndex(
                                    outitem.Key | initem.Key,
                                    initem.Value.ToArray())
                                );
                }
            }

            _sortedSearchIndexs = indices
                                .Distinct()
                                .OrderBy(_p => _p.Range.Length)
                                .ToArray();

        }


        //一个字符只检索 首字母
        //  按Level升序排序
        //大于等于两个字符 检索全部
        //  按首字符命中排序

        //安徽省 - 宿州市 - 砀山县
        //排除 命中 州市（ZS）而没命中 宿 的结果
        public static List<SearchRegionItem> SpellSearch(string spell)
        {
            UtilMethods.KeepLetters(ref spell);

            if (string.IsNullOrEmpty(spell))
            {
                return new List<SearchRegionItem>(0);
            }

            List<SearchRegionItem> results = new List<SearchRegionItem>();

            var letter = UtilMethods.ConvertUpperLetters(spell);

            Search(
                IndexScan(letter),
                ref spell,
                letter,
                results);

            Rules(results);

            return results;
        }


        /// <summary>
        ///     索引快速扫描，扫描参数 UpperLetter 包含的索引
        ///     若输入 AHSDSX（安徽省砀山县）那么参数转 UpperLetter 后变为：A | D | H | S | X
        ///     扫描 _sortedSearchIndexs 根据 Contains 规则选出第一个大集合，索引值：A | D，集合数量为：77个
        /// </summary>
        private static Region[] IndexScan(UpperLetter letters)
        {
            for (int i = 0; i < _sortedSearchIndexs.Length; i++)
            {
                var idx = _sortedSearchIndexs[i];

                if (letters.Contains(idx.Index))
                {
                    return idx.Range;
                }
            }
            return new Region[0];
        }


        /// <summary>
        ///     在索引命中的集合中精细化筛选出最终结果
        ///     扫描集合中每个项的 PathLetters 与 参数 UpperLetter 作比较，因为 PathLetters 是每个区划最全的 UpperLetter ，所以操作符为 Contains
        ///     若以上规则都成立，则比较当前项的 PathFullSpells 是否 ParttenContains 输入参数 spell
        /// </summary>
        private static void Search(
            Region[] range,
            ref string spell,
            UpperLetter letter,
            List<SearchRegionItem> results)
        {
            for (int j = 0; j < range.Length; j++)
            {
                var item = range[j];


                /*
                if (item.PathLetters.Contains(letter))
                {
                    for (int i = 0; i < item.PathFullSpells.Length; i++)
                    {
                        if (item.PathFullSpells[i].ParttenContains(spell))
                        {
                            results.Add(new SearchRegionItem(item));
                            break;
                        }
                    }
                }

                */
            }
        }


        /// <summary>
        ///     规则处理
        /// </summary>
        private static void Rules(List<SearchRegionItem> sitems)
        {

        }
    }
}
