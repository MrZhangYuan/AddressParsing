using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using static AddressParsing.MatchCarPetSlot;

namespace AddressParsing
{
    //AddressParser

    internal struct SubItem
    {
        public int StartIdx
        {
            get;
        }
        public int EndIdx
        {
            get => StartIdx + (!string.IsNullOrEmpty(this.Value) ? Value.Length : 0);
        }
        public string Value
        {
            get;
        }
        public SubItem(int startIdx, string value)
        {
            StartIdx = startIdx;
            Value = value;
        }

        public override string ToString()
        {
            return $"[{this.StartIdx},{this.EndIdx}] {this.Value}";
        }
    }



    internal enum PathRelation
    {
        //包含
        Contains,
        //被包含
        Included,
        //全等
        Equal,
        //无
        None
    }



    //struct RegionToken : IComparable, IComparable<RegionToken>, IEquatable<RegionToken>
    //{
    //    public MatchType? MatchType { get; }
    //    public string Value { get; }
    //    public RegionToken(MatchType matchType, string value)
    //    {
    //        MatchType = matchType;
    //        Value = value;
    //    }

    //    public int CompareTo(object obj)
    //    {
    //        if (obj is RegionToken other)
    //        {
    //            return this.CompareTo(other);
    //        }

    //        throw new InvalidCastException();
    //    }

    //    public int CompareTo(RegionToken other)
    //    {
    //        return string.CompareOrdinal(this.Value, other.Value);
    //    }

    //    public bool Equals(RegionToken other)
    //    {
    //        return string.Equals(this.Value, other.Value, StringComparison.Ordinal);
    //    }

    //    public override int GetHashCode()
    //    {
    //        return this.Value.GetHashCode();
    //    }
    //}


    internal class RegionToken
    {
        public MatchType MatchType { get; }
        public RegionWrap RegionWrap { get; }

        public RegionToken(MatchType matchType, RegionWrap regionWrap)
        {
            MatchType = matchType;
            RegionWrap = regionWrap;
        }

        public override string ToString()
        {
            return this.RegionWrap.ToPathString();
        }
    }


    public class StructureOptions
    {
        public static StructureOptions Default { get; }
        static StructureOptions()
        {
            Default = new StructureOptions();
        }


        //  匹配时文本的最大长度，超过进行剪裁
        public int TextMatchLimitLength { get; set; } = 40;

        //匹配时是否启用日志记录
        public bool OpenTrace { get; set; }

        //路径匹配达到深度时终止匹配
        public int PathMatchTerminatDeepLimit { get; set; } = 4;
    }



    //internal class RegionTokenGroup
    //{
    //    public string Token { get; }
    //    public RegionWrap[] Regions { get; }
    //    public RegionTokenGroup(string token, RegionWrap[] regions)
    //    {
    //        Token = token;
    //        Regions = regions;
    //    }

    //    public override string ToString()
    //    {
    //        return string.Join(",", Regions.Select(_p => _p.Region.Name));
    //    }
    //}


    //class DefaultStringCompare : IEqualityComparer<string>
    //{
    //    public static DefaultStringCompare Instance { get; }

    //    static DefaultStringCompare()
    //    {
    //        Instance = new DefaultStringCompare();
    //    }
    //    private DefaultStringCompare()
    //    {

    //    }

    //    public bool Equals(string x, string y)
    //    {
    //        return string.Equals(x, y, StringComparison.Ordinal);
    //    }

    //    public int GetHashCode(string obj)
    //    {
    //        return obj.GetHashCode();
    //    }
    //}


    public class GenerateParameter
    {

    }



    /// <summary>
    /// 通过大量的地址 生成等级区划结构
    /// </summary>
    public static class RegionGenerator
    {
        public static void MyMethod(Func<string, IEnumerable<string>> factory)
        {

        }
    }


    public static class AddressParser
    {
        static ReadOnlyDictionary<string, ReadOnlyCollection<RegionToken>> RegionTokenGroups { get; set; }
        //对所有的Name、ShortName 分组后，根据分组数量倒叙排列的长度
        private static int[] SortedTokens { get; set; }

        public static void Build(List<Region> regions)
        {
            var wraps = regions.Select(_p => new RegionWrap(_p)).ToArray();

            //Relation
            var dict = wraps
                        .GroupBy(_p => _p.Region.ParentID)
                        .ToDictionary(
                            _p => _p.Key,
                            _p => _p.ToArray()
                        );
            for (int i = 0; i < wraps.Length; i++)
            {
                var current = wraps[i];
                current.Children = dict.TryGetValue(current.Region.ID, out var children) ? children : null;
                if (current.Children != null)
                {
                    for (int j = 0; j < current.Children.Length; j++)
                    {
                        var child = current.Children[j];
                        child.Parent = current;
                        child.IndexOfParent = j;
                    }
                }
            }


            //PathInfo
            foreach (var item in wraps)
            {
                item.BuildShortNames();
                item.BuildPathInfo();

                //var pname = item.BuildPathNames()
                //                    .Except(item.Region.ShortNames)
                //                    .Except(new string[1] { item.Region.Name })
                //                    .OrderByDescending(_p => _p.Length)
                //                    .ToArray();
                //item.PathNames = pname.Length > 0 ? pname : null;

                //var pspell = UtilMethods.CheckFullSpell(
                //                        item.BuildPathSpells()
                //                            .Except(item.Region.ShortNamesSpell)
                //                            .Except(new string[1] { item.Region.NameSpell })
                //                      );
                //item.PathFullSpells = pspell.Length > 0 ? pspell : null;
            }

            AddressParser.RegionTokenGroups = BuildTokenGroups(wraps);
        }


        struct _Slot
        {
            public string Key { get; }
            public RegionWrap Wrap { get; }

            public _Slot(string key, RegionWrap wrap)
            {
                Key = key;
                Wrap = wrap;
            }
        }
        private static ReadOnlyDictionary<string, ReadOnlyCollection<RegionToken>> BuildTokenGroups(IEnumerable<RegionWrap> regions)
        {
            var result = new Dictionary<string, ReadOnlyCollection<RegionToken>>();

            result = regions.SelectMany(_p => Nest(_p))
                            .GroupBy(_p => _p.Key)
                            .ToDictionary(
                                _p => _p.Key,
                                _p => Array.AsReadOnly(
                                        _p.Select(_p1 => new RegionToken(string.Equals(_p1.Key, _p1.Wrap.Region.Name, StringComparison.Ordinal) ? MatchType.Name : MatchType.ShortName, _p1.Wrap))
                                            .OrderBy(_p1 => _p1.MatchType == MatchType.Name ? 0 : 1)
                                            .ToArray()
                                    )
                            );

            SortedTokens = result.Keys.GroupBy(_p => _p.Length)
                                    .Select(_p => new
                                    {
                                        Key = _p.Key,
                                        Count = _p.Count()
                                    })
                                    .Where(_p => _p.Key > 1)
                                    .OrderByDescending(_p => _p.Count)
                                    .Select(_p => _p.Key)
                                    .ToArray();

            return new ReadOnlyDictionary<string, ReadOnlyCollection<RegionToken>>(result);

            IEnumerable<_Slot> Nest(RegionWrap reg)
            {
                var set = new HashSet<string>();
                if (reg.Region.ShortNames?.Count > 0)
                {
                    foreach (var item in reg.Region.ShortNames)
                    {
                        set.Add(item);
                    }
                }
                set.Add(reg.Region.Name);
                return set.Select(_p => new _Slot(_p, reg));
            }
        }





        public static List<Region> ParsingAddress(string address)
        {
            return ParsingAddressCore(ref address, StructureOptions.Default);
        }
        public static List<Region> ParsingAddress(string address, StructureOptions options)
        {
            return ParsingAddressCore(ref address, options != null ? options : StructureOptions.Default);
        }
        /// <summary>
        /// 更小、更快、更强、更稳定、更灵活、更智能
        /// </summary>
        private static List<Region> ParsingAddressCore(ref string address, StructureOptions options)
        {
            UtilMethods.RemoveChars(ref address, options.TextMatchLimitLength);

            if (string.IsNullOrEmpty(address)
                || address.Length < 2)
            {
                return new List<Region>(0);
            }

            //Guessing IndexMatch
            //foreach (var item in IdentitySortedLength.Where(_p => address.Length >= _p))
            //{
            //    var key = address.Substring(0, item);
            //    if (IdentitedRegions.TryGetValue(key, out var identity))
            //    {
            //        results.AddRange(identity.Regions.Select(_p => _p.Region));
            //        break;
            //    }
            //}


            //IndexMatch

            int addlen = address.Length;


            var sw = Stopwatch.StartNew();
            CarPetMatch();
            var carpets = new List<SubItem>(200);
            foreach (var item in SortedTokens.Where(_p => addlen >= _p))
            {
                carpets.AddRange(UtilMethods.SubItems(address, item));
            }
            sw.Stop();



            var sw2 = Stopwatch.StartNew();
            RuleEvaluator results = new RuleEvaluator();
            foreach (var carpet in carpets)
            {
                if (RegionTokenGroups.TryGetValue(carpet.Value, out var group))
                {
                    for (int i = 0; i < group.Count; i++)
                    {
                        var slot = new MatchCarPetSlot(carpet, group[i]);
                        results.MergeAdd(slot);
                    }
                }
            }
            sw2.Stop();


            var ss = Stopwatch.StartNew();
            var bests = results.AssessBests();
            ss.Stop();

            Console.WriteLine($"CARPET:{carpets.Count} {sw.Elapsed} - RES:{results._matchCarPets.Count} {sw2.Elapsed} - RULE:{ss.Elapsed} - SUM:{sw.Elapsed + sw2.Elapsed}");
            if (bests != null)
            {
                foreach (var item in bests)
                {
                    Console.WriteLine(item.RegionWrap.ToPathString());
                }
            }

            Console.WriteLine();

            return new List<Region>();
        }

        private static void CarPetMatch()
        {

        }
    }



    /// <summary>
    /// 规则评估器
    /// </summary>
    internal class RuleEvaluator
    {
        public List<MatchCarPetSlot> _matchCarPets = new List<MatchCarPetSlot>(20);

        public bool MergeAdd(MatchCarPetSlot carpet)
        {
            var merged = false;

            for (int j = 0; j < _matchCarPets.Count; j++)
            {
                merged = _matchCarPets[j].Merge(carpet);
                if (merged)
                {
                    break;
                }
            }

            if (!merged)
            {
                _matchCarPets.Add(carpet);
            }

            return !merged;
        }

        public List<MatchCarPetSlot> AssessBests()
        {
            if (this._matchCarPets == null
                || this._matchCarPets.Count == 0)
            {
                return null;
            }

            int max_path_length = int.MinValue;
            int sum_name_count = int.MinValue;
            int sum_start_idx = int.MaxValue;
            int sum_letter_count = int.MinValue;
            int shallowest_deep = int.MaxValue;

            for (int i = 0; i < this._matchCarPets.Count; i++)
            {
                this._matchCarPets[i].Finalizing();

                max_path_length = Math.Max(max_path_length, this._matchCarPets[i].PathLength);
                sum_name_count = Math.Max(sum_name_count, this._matchCarPets[i].NameCount);
                sum_start_idx = Math.Min(sum_start_idx, this._matchCarPets[i].StartIndexes);
                sum_letter_count = Math.Max(sum_letter_count, this._matchCarPets[i].SumLetterCount);
                shallowest_deep = Math.Min(shallowest_deep, this._matchCarPets[i].PathDeep);
            }


            var results = new List<MatchCarPetSlot>();

            //第一次筛选路径最长的
            foreach (var item in this._matchCarPets)
            {
                if (item.PathLength == max_path_length)
                {
                    results.Add(item);
                }
            }


            //第二次筛选 Name 命中最多的
            if (results.Count > 1)
            {
                var temp = new List<MatchCarPetSlot>();

                foreach (var item in results)
                {
                    if (item.NameCount == sum_name_count)
                    {
                        temp.Add(item);
                    }
                }

                results = temp;
            }


            //第四次 所有命中的初索引最小的
            if (results.Count > 1)
            {
                var temp = new List<MatchCarPetSlot>();
                foreach (var item in results)
                {
                    if (item.StartIndexes == sum_start_idx)
                    {
                        temp.Add(item);
                    }
                }
                results = temp;
            }


            //第三次筛选 命中字数最多的
            if (results.Count > 1)
            {
                var temp = new List<MatchCarPetSlot>();
                foreach (var item in results)
                {
                    if (item.SumLetterCount == sum_letter_count)
                    {
                        temp.Add(item);
                    }
                }
                results = temp;
            }



            //第五次 拿最大深度靠前的
            if (results.Count > 1)
            {
                var temp = new List<MatchCarPetSlot>();
                foreach (var item in results)
                {
                    if (item.PathDeep == shallowest_deep)
                    {
                        temp.Add(item);
                    }
                }
                results = temp;
            }

            return results;
        }
    }


    struct SlotFlag
    {
        public Nullable<SubItem> Index { get; }
        public Nullable<MatchType> MatchType { get; }

        public SlotFlag(SubItem index, MatchType matchType)
        {
            Index = index;
            MatchType = matchType;
        }

        public override string ToString()
        {
            return this.Index != null && this.MatchType != null ? $"{this.Index.Value} {this.MatchType.Value}" : "";
        }
    }

    internal class MatchCarPetSlot
    {
        private bool _isFinal = false;
        public RegionWrap RegionWrap { get; private set; }

        public List<SlotFlag[]> SlotFlags { get; private set; }

        //路径长度
        public int PathLength { get; private set; }

        //路径终点的Deep
        public int PathDeep { get; private set; }

        //SlotFlags 中 MatchType.Name 的个数
        public int NameCount { get; set; }

        //SlotFlags 中所有 Index.StartIdx 之和
        public int StartIndexes { get; set; }

        //SlotFlags 中所有 Index.Value 字数之和
        public int SumLetterCount { get; set; }

        public MatchCarPetSlot(SubItem index, RegionToken token)
        {
            this.RegionWrap = token.RegionWrap;

            var current = new SlotFlag[RegionWrap.MaxPathDeep];

            current[this.RegionWrap.PathDeep - 1] = new SlotFlag(index, token.MatchType);

            this.SlotFlags = new List<SlotFlag[]> { current };
        }

        //public override string ToString()
        //{
        //    return $"{string.Join("-", SlotFlags.Where(_p => _p.Index.HasValue).Select(_p => _p.Index.Value + ":" + _p.MatchType))} {this.RegionWrap}";
        //}

        //public int CheckPathLength()
        //{
        //    return 00;
        //}


        public SlotFlag? FindBest(int startidx, SlotFlag[] slots)
        {
            SlotFlag? slot = null;

            var valid = slots.Where(_p => _p.Index.HasValue && _p.MatchType.HasValue)
                            .Where(_p => !string.IsNullOrEmpty(_p.Index.Value.Value) && _p.Index.Value.StartIdx >= startidx)
                            .ToArray();

            if (valid.Length > 1)
            {
                slot = valid[0];

                for (int i = 1; i < valid.Length; i++)
                {
                    var cur = valid[i];

                    if (slot.Value.MatchType == MatchType.Name)
                    {
                        if (cur.MatchType == MatchType.Name)
                        {
                            //字数多优先
                            //索引靠前优先
                            if (cur.Index.Value.Value.Length > slot.Value.Index.Value.Value.Length)
                            {
                                slot = cur;
                            }
                            else if (cur.Index.Value.StartIdx < slot.Value.Index.Value.StartIdx)
                            {
                                slot = cur;
                            }
                        }
                    }
                    //shortname
                    else
                    {
                        //字数多优先
                        //索引靠前优先
                        if (cur.Index.Value.Value.Length > slot.Value.Index.Value.Value.Length)
                        {
                            slot = cur;
                        }
                        else if (cur.Index.Value.StartIdx < slot.Value.Index.Value.StartIdx)
                        {
                            slot = cur;
                        }
                    }
                }
            }
            else if (valid.Length == 1)
            {
                slot = valid[0];
            }

            return slot;
        }


        public void Finalizing()
        {
            var final = new SlotFlag[RegionWrap.MaxPathDeep];

            SlotFlag? previes = null;

            for (int j = 0; j < RegionWrap.MaxPathDeep; j++)
            {
                previes = FindBest(previes == null ? 0 : previes.Value.Index.Value.EndIdx, this.SlotFlags.Select(_p => _p[j]).ToArray());

                final[j] = previes != null ? previes.Value : default;
            }

            this.SlotFlags.Clear();

            this.SlotFlags.Add(final);

            this.PathLength = 0;
            this.PathDeep = 0;
            this.NameCount = 0;
            this.SumLetterCount = 0;
            this.StartIndexes = 0;

            for (int i = 0; i < final.Length; i++)
            {
                var cur = final[i];

                if (cur.Index != null)
                {
                    this.PathLength++;
                    this.PathDeep = Math.Max(this.PathDeep, i + 1);

                    if (cur.MatchType == MatchType.Name)
                    {
                        this.NameCount++;
                    }

                    this.SumLetterCount += cur.Index.Value.Value.Length;
                    this.StartIndexes += cur.Index.Value.StartIdx;
                }
            }

            this._isFinal = true;
        }



        public bool Merge(MatchCarPetSlot other)
        {
            if (this._isFinal)
            {
                throw new InvalidOperationException();
            }

            var relation = this.RegionWrap.PathRelationOf(other.RegionWrap);

            //若this包含或等于other，则合并SlotFlags
            if (relation == PathRelation.Equal
                || relation == PathRelation.Contains)
            {
                //for (int i = 0; i < SlotFlags.Length; i++)
                //{
                //    this.SlotFlags[i] = this.FlagLevelUp(this.SlotFlags[i], other.SlotFlags[i]);
                //}

                this.SlotFlags.AddRange(other.SlotFlags);

                return true;
            }
            //若this包含于other中，则合并SlotFlags 并且替换 RegionWrap
            else if (relation == PathRelation.Included)
            {
                //[RID = 19]->[RID = 1650]->[RID = 20051]
                //[RID = 19]->[RID = 1650]->[RID = 20051]->[RID = 51416]

                var current = this.SlotFlags.Select(_p => _p[this.RegionWrap.PathDeep - 1]).ToArray();
                var target = other.SlotFlags.Select(_p => _p[other.RegionWrap.PathDeep - 1]).ToArray();

                var curend = current.Where(_p => _p.Index.HasValue).Max(_p => _p.Index.Value.EndIdx);
                var nextstart = target.Where(_p => _p.Index.HasValue).Max(_p => _p.Index.Value.StartIdx);

                //当新路径的命中索引起点 >= 当前命中的终点，如以下地址
                //--河南省南阳市宛城区高庙乡
                //宛城区下面存在 高庙镇、城区
                //若不进行索引比对 则就被城区替换 变成错误的 河南省-南阳市-宛城区-城区
                if (nextstart >= curend)
                {
                    this.RegionWrap = other.RegionWrap;
                    this.SlotFlags.AddRange(other.SlotFlags);
                }
                else
                {
                    return false;
                }

                //for (int i = 0; i < SlotFlags.Length; i++)
                //{
                //    this.SlotFlags[i] = this.FlagLevelUp(this.SlotFlags[i], other.SlotFlags[i]);
                //}

                return true;
            }

            return false;
        }
    }
}
