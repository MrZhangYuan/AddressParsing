using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace AddressParsing
{
    /// <summary>
    ///     区划
    /// </summary>
    public class Region
    {
        /// <summary>
        ///     ID
        /// </summary>
        public string ID
        {
            get;
        }

        /// <summary>
        ///     上级 Region ID
        /// </summary>
        public string ParentID
        {
            get;
        }

        /// <summary>
        ///     标准全称，如：安徽省、昆明市、徐州市、砀山县、赵屯镇等
        /// </summary>
        public string Name
        {
            get;
        }


        /// <summary>
        ///     名称拼音
        /// </summary>
        public string NameSpell
        {
            get;
        }


        /// <summary>
        ///     等级
        ///     省   市   区   县
        ///     1    2    3   4
        /// </summary>
        public int Level
        {
            get;
        }


        /// <summary>
        ///     简称
        /// </summary>
        public ReadOnlyCollection<string> ShortNames
        {
            get;
            internal set;
        }


        /// <summary>
        ///     和 ShortNames 索引位置对应的拼音
        /// </summary>
        public ReadOnlyCollection<string> ShortNamesSpell
        {
            get;
            internal set;
        }

        public Region(string id,
            string parentid,
            string name,
            string namespell,
            int level,
            IEnumerable<string> shortnames,
            IEnumerable<string> shortnamesspell)
        {
            this.ID = UtilMethods.ThrowIfNull(id, nameof(id));
            this.ParentID = UtilMethods.ThrowIfNull(parentid, nameof(parentid));
            this.Name = UtilMethods.ThrowIfNull(name, nameof(name));
            this.NameSpell = UtilMethods.ThrowIfNull(namespell, nameof(namespell));
            this.Level = level;

            if (shortnames != null)
            {
                this.ShortNames = new ReadOnlyCollection<string>(shortnames.Where(_p => !string.IsNullOrEmpty(_p)).Distinct().ToList());
            }

            if (shortnamesspell != null)
            {
                this.ShortNamesSpell = new ReadOnlyCollection<string>(shortnamesspell.Where(_p => !string.IsNullOrEmpty(_p)).Distinct().ToList());
            }
        }


        /// <summary>
        ///     行政区划代码
        /// </summary>
        public string AdDivCode
        {
            get;
            set;
        }

        /// <summary>
        ///     区号
        /// </summary>
        public string AreaCode
        {
            get;
            set;
        }

        /// <summary>
        ///     邮政编码
        /// </summary>
        public string ZipCode
        {
            get;
            set;
        }
    }


    /// <summary>
    ///     区划包装器
    /// </summary>
    internal class RegionWrap
    {
        private static long _unionKeySeeds = 0;
        //区划总共有几级
        public static int MaxPathDeep = 0;

        //内部唯一ID，通过这个字段进行比较
        public long UnionKey
        {
            get;
        }

        public Region Region { get; }

        public RegionWrap Parent
        {
            get;
            internal set;
        }

        public RegionWrap[] Children
        {
            get;
            internal set;
        }

        internal int IndexOfParent
        {
            get;
            set;
        }


        /// <summary>
        ///     从起点至当前节点的所有 ID 使用 -> 拼接
        /// </summary>
        public string PathId
        {
            get;
            private set;
        }

        //internal string[] PathNames
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        ///     PathNames 的拼音
        ///     其数量和索引不与 PathNames 一一对应
        ///     它是 PathNames 经 <see cref="UtilMethods.CheckFullSpell(IEnumerable{string})"> 算法处理过的
        /// </summary>
        internal string[] PathFullSpells
        {
            get;
            set;
        }

        internal UpperLetter PathLetters
        {
            get;
            private set;
        }

        /// <summary>
        ///     实际路径深度：
        ///     省：1
        ///     市：2
        ///     县：3
        ///     ......
        /// </summary>
        internal int PathDeep
        {
            get;
            private set;
        }




        public RegionWrap(Region region)
        {
            this.Region = region ?? throw new ArgumentNullException(nameof(region));
            this.UnionKey = Interlocked.Increment(ref _unionKeySeeds);
        }

        public IEnumerable<RegionWrap> GetPath()
        {
            if (this.Parent != null)
            {
                foreach (var item in this.Parent.GetPath())
                {
                    yield return item;
                }
            }

            yield return this;
        }

        //public IEnumerable<string> BuildPathNames()
        //{
        //    if (this.Parent != null)
        //    {
        //        foreach (var item in this.Parent.GetPath().SelectMany(_p => _p.BuildPathNames()))
        //        {
        //            yield return item + this.Region.Name;

        //            foreach (var shortname in this.Region.ShortNames)
        //            {
        //                yield return item + shortname;
        //            }
        //        }
        //    }

        //    if (this.Children != null
        //        && this.Children.Length > 0)
        //    {
        //        yield return this.Region.Name;

        //        foreach (var shortname in this.Region.ShortNames)
        //        {
        //            yield return shortname;
        //        }
        //    }
        //}

        //public IEnumerable<string> BuildPathSpells()
        //{
        //    if (this.Parent != null)
        //    {
        //        foreach (var item in this.Parent.GetPath().SelectMany(_p => _p.BuildPathSpells()))
        //        {
        //            yield return item + this.Region.NameSpell;

        //            foreach (var shortspell in this.Region.ShortNamesSpell)
        //            {
        //                yield return item + shortspell;
        //            }
        //        }
        //    }

        //    if (this.Children != null
        //        && this.Children.Length > 0)
        //    {
        //        yield return this.Region.NameSpell;

        //        foreach (var shortspell in this.Region.ShortNamesSpell)
        //        {
        //            yield return shortspell;
        //        }
        //    }
        //}





        public IEnumerable<char> BuildPathLetters()
        {
            if (this.Parent != null)
            {
                foreach (var item in this.Parent.BuildPathLetters())
                {
                    yield return item;
                }
            }

            for (int i = 0; i < this.Region.NameSpell.Length; i++)
            {
                yield return this.Region.NameSpell[i];
            }
        }


        public void BuildShortNames()
        {
            if (this.Region.ShortNames == null
                || this.Region.ShortNames.Count == 0)
            {

            }

            if (this.Region.ShortNamesSpell == null
                || this.Region.ShortNamesSpell.Count == 0)
            {

            }
        }


        public void BuildPathInfo()
        {
            var path = new string(this.BuildPathLetters().Distinct().ToArray()).ToUpper();

            this.PathLetters = !string.IsNullOrEmpty(path) ? UtilMethods.ConvertUpperLetters(path) : UpperLetter.None;

            this.PathId = string.Join("->", this.GetPath().Select(_p => _p.UnionKey.ToString().PadLeft(8, '0')));

            var deep = 1;
            var parent = this.Parent;
            while (parent != null)
            {
                deep++;
                parent = parent.Parent;
            }
            this.PathDeep = deep;

            RegionWrap.MaxPathDeep = Math.Max(RegionWrap.MaxPathDeep, this.PathDeep);
        }


        public bool Contains(RegionWrap obj)
        {
            if (obj != null
                && this.PathDeep >= obj.PathDeep)
            {
                if (RegionWrapComparer.Instance.Equals(this, obj))
                {
                    return true;
                }

                if (this.PathDeep == obj.PathDeep)
                {
                    return false;
                }

                var dep_range = this.PathDeep - obj.PathDeep;
                var cur = this;
                for (int i = 0; i < dep_range; i++)
                {
                    cur = cur.Parent;
                }

                return RegionWrapComparer.Instance.Equals(cur, obj);
            }

            return false;
        }


        public PathRelation PathRelationOf(RegionWrap wrap)
        {
            if (this.Contains(wrap))
            {
                if (RegionWrapComparer.Instance.Equals(this, wrap))
                {
                    return PathRelation.Equal;
                }
                return PathRelation.Contains;
            }
            else if (wrap.Contains(this))
            {
                return PathRelation.Included;
            }

            return PathRelation.None;
        }


        public override string ToString()
        {
            return $"{this.Region.Name}";
        }

        public string ToPathString()
        {
            return string.Join(" - ", this.GetPath().Select(_p => _p.Region.Name));
        }


    }

}
