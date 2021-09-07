using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AddressParsing
{
    /// <summary>
    ///     区划
    /// </summary>
    public class Region
    {
        /// <summary>
        ///     内部ID
        /// </summary>
        public string ID
        {
            get;
        }

        /// <summary>
        ///     等级
        /// </summary>
        public int Level
        {
            get;
        }

        /// <summary>
        ///     名称
        /// </summary>
        public string Name
        {
            get;
        }

        /// <summary>
        ///     行政区划代码
        /// </summary>
        public string AdDivCode
        {
            get;
        }

        /// <summary>
        ///     区号
        /// </summary>
        public string AreaCode
        {
            get;
        }

        /// <summary>
        ///     邮政编码
        /// </summary>
        public string ZipCode
        {
            get;
        }

        /// <summary>
        ///     按照识别度（如：字符串长度）倒序排列的简称
        /// </summary>
        internal string[] ShortNames
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

        [JsonConstructor]
        internal Region(
            string iD,
            int level,
            string name,
            string adDivCode,
            string areaCode,
            string zipCode,
            string[] shortNames,
            string parentID)
        {
            this.ID = iD;
            this.Level = level;
            this.Name = name;
            this.AdDivCode = adDivCode;
            this.AreaCode = areaCode;
            this.ZipCode = zipCode;
            this.ShortNames = shortNames;
            this.ParentID = parentID;
        }

        /// <summary>
        ///     全称，如 上海市闵行区、上海闵行区、上海市闵行、上海闵行
        /// </summary>
        [JsonIgnore]
        internal string[] PathNames
        {
            get;
            set;
        }

        /// <summary>
        ///     PathNames 的匹配跳跃表
        /// </summary>
        [JsonIgnore]
        internal int[] PathNameSkip
        {
            get;
            set;
        }

        /// <summary>
        ///     ShortName 的跳跃表
        /// </summary>
        [JsonIgnore]
        internal bool[] ShortNameSkip
        {
            get;
            set;
        }

        [JsonIgnore]
        public Region Parent
        {
            get;
            internal set;
        }

        [JsonIgnore]
        public ReadOnlyCollection<Region> Children
        {
            get;
            internal set;
        }

        [JsonIgnore]
        internal int IndexOfParent
        {
            get;
            set;
        }

        [JsonIgnore]
        internal string[] ChildrenShortestNames
        {
            get;
            set;
        }

        public string GetFullPathText()
        {
            var pathtext = this.Parent == null ? "" : this.Parent.GetFullPathText();

            if (!string.IsNullOrEmpty(pathtext))
            {
                pathtext = $"{pathtext} - {this.Name}";
            }
            else
            {
                pathtext = this.Name;
            }

            return pathtext;
        }

        public IEnumerable<Region> GetPath()
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

        public IEnumerable<Region> GetPathDesc()
        {
            yield return this;

            if (this.Parent != null)
            {
                foreach (var item in this.Parent.GetPathDesc())
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<string> BuildPathNames()
        {
            if (this.Parent != null)
            {
                foreach (var item in this.Parent.GetPath().SelectMany(_p => _p.BuildPathNames()))
                {
                    yield return item + this.Name;

                    foreach (var shortname in this.ShortNames)
                    {
                        yield return item + shortname;
                    }
                }
            }

            if (this.Children != null
                && this.Children.Count > 0)
            {
                yield return this.Name;

                foreach (var shortname in this.ShortNames)
                {
                    yield return shortname;
                }
            }
        }

        public Region GetTopParent()
        {
            var parent = this.Parent;
            if (parent == null)
            {
                return this;
            }

            while (parent.Parent != null)
            {
                parent = parent.Parent;
            }

            return parent;
        }

        public bool PathContains(Region region)
        {
            if (region == null)
            {
                return false;
            }

            bool results = false;

            var selfpath = this.GetPath()?.ToArray();
            var targetpath = region.GetPath()?.ToArray();

            if (selfpath != null
               && targetpath != null
               && selfpath.Length >= targetpath.Length
               && targetpath.Length > 0)
            {
                var temp = true;

                for (int i = 0; i < targetpath.Length; i++)
                {
                    temp = temp && (selfpath[i] == targetpath[i]);
                }

                results = temp;
            }

            return results;
        }

        public override string ToString()
        {
            return this.GetFullPathText();
        }
    }
}
