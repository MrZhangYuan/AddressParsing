using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace AddressParsing
{
    public class Region
    {
        public string ID
        {
            get;
        }

        public int Level
        {
            get;
        }

        /// <summary>
        /// 统称，如 上海市
        /// </summary>
        public string Name
        {
            get;
        }
        public string ZipCode
        {
            get;
        }

        /// <summary>
        /// 按照识别度（如：字符串长度）倒序排列的简称
        /// </summary>
        public string[] ShortNames
        {
            get;
        }

        /// <summary>
        /// 全称，如 上海市闵行区、上海闵行区、上海市闵行、上海闵行
        /// </summary>
        public string[] FullNames
        {
            get;
            internal set;
        }

        public string ParentID
        {
            get;
        }

        [JsonConstructor]
        public Region(
            string iD,
            int level,
            string name,
            string zipCode,
            string[] shortNames,
            string parentID)
        {
            ID = iD;
            Level = level;
            Name = name;
            ZipCode = zipCode;
            ShortNames = shortNames;
            ParentID = parentID;
        }

        [JsonIgnore]
        public Region Parent
        {
            get;
            internal set;
        }

        [JsonIgnore]
        public List<Region> Children
        {
            get;
            internal set;
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

        public IEnumerable<string> BuildFullNames()
        {
            if (this.Parent != null)
            {
                foreach (var item in this.Parent.GetPath().SelectMany(_p => _p.BuildFullNames()))
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
