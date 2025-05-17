using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AddressParsing
{
    internal static class UtilMethods
    {
        /// <summary>
        ///     字母字符集
        /// </summary>
        internal static char[] Letters { get; } = new char[104]
        {
            'A', 'a', 'Ａ', 'ａ',
            'B', 'b', 'Ｂ', 'ｂ',
            'C', 'c', 'Ｃ', 'ｃ',
            'D', 'd', 'Ｄ', 'ｄ',
            'E', 'e', 'Ｅ', 'ｅ',
            'F', 'f', 'Ｆ', 'ｆ',
            'G', 'g', 'Ｇ', 'ｇ',
            'H', 'h', 'Ｈ', 'ｈ',
            'I', 'i', 'Ｉ', 'ｉ',
            'J', 'j', 'Ｊ', 'ｊ',
            'K', 'k', 'Ｋ', 'ｋ',
            'L', 'l', 'Ｌ', 'ｌ',
            'M', 'm', 'Ｍ', 'ｍ',
            'N', 'n', 'Ｎ', 'ｎ',
            'O', 'o', 'Ｏ', 'ｏ',
            'P', 'p', 'Ｐ', 'ｐ',
            'Q', 'q', 'Ｑ', 'ｑ',
            'R', 'r', 'Ｒ', 'ｒ',
            'S', 's', 'Ｓ', 'ｓ',
            'T', 't', 'Ｔ', 'ｔ',
            'U', 'u', 'Ｕ', 'ｕ',
            'V', 'v', 'Ｖ', 'ｖ',
            'W', 'w', 'Ｗ', 'ｗ',
            'X', 'x', 'Ｘ', 'ｘ',
            'Y', 'y', 'Ｙ', 'ｙ',
            'Z', 'z', 'Ｚ', 'ｚ'
        };

        /// <summary>
        ///     地址常用分割符，用来首次处理地址时移除
        /// </summary>
        internal static char[] SplitterChars { get; } = new char[]
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
        internal static string[] RegionInvalidSuffix { get; } = new string[]
        {
            "街", "路", "村", "弄", "幢", "号", "道",
            "大厦", "工业", "产业", "广场", "科技", "公寓", "中心", "小区", "花园", "大道", "农场",
            "0","1","2","3","4","5","6","7","8","9",
            "０","１","２","３","４","５","６","７","８","９",
            "A","B","C","D","E","F","G","H","I","J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "a","b","c","d","e","f","g","h","i","j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
            "ａ","ｂ","ｃ","ｄ","ｅ","ｆ","ｇ","ｈ","ｉ","ｊ", "ｋ", "ｌ", "ｍ", "ｎ", "ｏ", "ｐ", "ｑ", "ｒ", "ｓ", "ｔ", "ｕ", "ｖ", "ｗ", "ｘ", "ｙ", "ｚ"
        };



        public static int CheckRegionLevel(int level)
        {
            for (int i = 1; i <= 3; i++)
            {
                if (level == i)
                {
                    return level;
                }
            }

            throw new ArgumentException("区划等级错误，可选的值为[1,2,3]，对应[省，市，县]");
        }

        public static string CheckRegionParentID(int level, string parentid)
        {
            if (level > 1
                && string.IsNullOrEmpty(parentid))
            {
                throw new ArgumentException("区划等级为[2,3]，对应区划为[市，县]时，必须包含有效的 parentID");
            }

            return parentid;
        }

        public static string ThrowIfNull(string val, string name)
        {
            if (string.IsNullOrEmpty(val))
            {
                throw new ArgumentNullException(name);
            }

            return val;
        }

        public static string DefaultIfNull(string val, string def)
        {
            if (string.IsNullOrEmpty(val))
            {
                return def;
            }

            return val;
        }

        public static T ThrowIfNull<T>(T val, string name) where T : class
        {
            if (val == null)
            {
                throw new ArgumentNullException(name);
            }

            return val;
        }


        public static List<T> MaxGroup<T>(this List<T> source, Func<T, int> keyselector)
        {
            if (source.Count <= 1)
            {
                return source;
            }

            if (source.Count == 2)
            {
                var val1 = keyselector(source[0]);
                var val2 = keyselector(source[1]);
                if (val1 == val2)
                {
                    return source;
                }
                else if (val1 > val2)
                {
                    source.RemoveAt(1);
                    return source;
                }
                else
                {
                    source.RemoveAt(0);
                    return source;
                }
            }

            if (source.Count == 3)
            {
                var val1 = keyselector(source[0]);
                var val2 = keyselector(source[1]);
                var val3 = keyselector(source[2]);

                if (val1 == val2)
                {
                    if (val1 == val3)
                    {
                        return source;
                    }
                    else if (val1 > val3)
                    {
                        source.RemoveAt(2);
                        return source;
                    }
                    else
                    {
                        source.RemoveAt(0);
                        source.RemoveAt(0);
                        return source;
                    }
                }
                else if (val1 > val2)
                {
                    if (val1 == val3)
                    {
                        source.RemoveAt(1);
                        return source;
                    }
                    else if (val1 > val3)
                    {
                        source.RemoveAt(1);
                        source.RemoveAt(1);
                        return source;
                    }
                    else
                    {
                        source.RemoveAt(0);
                        source.RemoveAt(0);
                        return source;
                    }
                }
                else
                {
                    if (val2 == val3)
                    {
                        source.RemoveAt(0);
                        return source;
                    }
                    else if (val2 > val3)
                    {
                        source.RemoveAt(0);
                        source.RemoveAt(1);
                        return source;
                    }
                    else
                    {
                        source.RemoveAt(0);
                        source.RemoveAt(0);
                        return source;
                    }
                }
            }


            if (source.Count > 3)
            {
                int max = int.MinValue;
                List<T> groups = new List<T>(source.Count);

                for (int i = 0; i < source.Count; i++)
                {
                    var value = keyselector(source[i]);
                    if (value > max)
                    {
                        groups.Clear();
                        max = value;
                        groups.Add(source[i]);
                    }
                    else if (value == max)
                    {
                        groups.Add(source[i]);
                    }
                }
                return groups;
            }

            return new List<T>(0);
        }



        public static List<T> MinGroup<T>(this List<T> source, Func<T, int> keyselector)
        {
            if (source.Count <= 1)
            {
                return source;
            }

            if (source.Count == 2)
            {
                var val1 = keyselector(source[0]);
                var val2 = keyselector(source[1]);
                if (val1 == val2)
                {
                    return source;
                }
                else if (val1 > val2)
                {
                    source.RemoveAt(0);
                    return source;
                }
                else
                {
                    source.RemoveAt(1);
                    return source;
                }
            }

            if (source.Count == 3)
            {
                var val1 = keyselector(source[0]);
                var val2 = keyselector(source[1]);
                var val3 = keyselector(source[2]);

                if (val1 == val2)
                {
                    if (val1 == val3)
                    {
                        return source;
                    }
                    else if (val1 > val3)
                    {
                        source.RemoveAt(0);
                        source.RemoveAt(0);
                        return source;
                    }
                    else
                    {
                        source.RemoveAt(2);
                        return source;
                    }
                }
                else if (val1 > val2)
                {
                    if (val2 == val3)
                    {
                        source.RemoveAt(0);
                        return source;
                    }
                    else if (val2 > val3)
                    {
                        source.RemoveAt(0);
                        source.RemoveAt(0);
                        return source;
                    }
                    else
                    {
                        source.RemoveAt(0);
                        source.RemoveAt(1);
                        return source;
                    }
                }
                else
                {
                    if (val1 == val3)
                    {
                        source.RemoveAt(1);
                        return source;
                    }
                    else if (val1 > val3)
                    {
                        source.RemoveAt(0);
                        source.RemoveAt(0);
                        return source;
                    }
                    else
                    {
                        source.RemoveAt(1);
                        source.RemoveAt(1);
                        return source;
                    }
                }
            }


            if (source.Count > 3)
            {
                int min = int.MaxValue;
                List<T> groups = new List<T>(3);

                for (int i = 0; i < source.Count; i++)
                {
                    var value = keyselector(source[i]);
                    if (value < min)
                    {
                        groups.Clear();
                        min = value;
                        groups.Add(source[i]);
                    }
                    else if (value == min)
                    {
                        groups.Add(source[i]);
                    }
                }
                return groups;
            }

            return new List<T>(0);
        }



        public static void RemoveChars(ref string sourcestr)
        {
            if (!string.IsNullOrEmpty(sourcestr))
            {
                char[] newchars = null;

                int j = 0;
                for (int i = 0; i < sourcestr.Length; i++)
                {
                    if (!SplitterChars.Contains(sourcestr[i]))
                    {
                        if (newchars == null)
                        {
                            newchars = new char[sourcestr.Length];
                        }

                        newchars[j] = sourcestr[i];
                        j++;
                    }
                }

                if (j > 0)
                {
                    sourcestr = new string(newchars, 0, j);
                }
            }
        }



        public static bool Contains(this UpperLetter letter1, UpperLetter letter2)
        {
            return (letter1 & letter2) == letter2;
        }



        public static UpperLetter ConvertUpperLetters(string letters)
        {
            UpperLetter letter = UpperLetter.None;

            if (letters.Length > 0)
            {
                for (int i = 0; i < letters.Length; i++)
                {
                    letter |= ConvertUpperLetter(letters[i]);
                }
            }

            return letter;
        }



        public static UpperLetter ConvertUpperLetter(char ch)
        {
            UpperLetter letter = UpperLetter.None;

            if (ch == 'A') letter |= UpperLetter.A;
            else if (ch == 'B') letter |= UpperLetter.B;
            else if (ch == 'C') letter |= UpperLetter.C;
            else if (ch == 'D') letter |= UpperLetter.D;
            else if (ch == 'E') letter |= UpperLetter.E;
            else if (ch == 'F') letter |= UpperLetter.F;
            else if (ch == 'G') letter |= UpperLetter.G;
            else if (ch == 'H') letter |= UpperLetter.H;
            else if (ch == 'I') letter |= UpperLetter.I;
            else if (ch == 'J') letter |= UpperLetter.J;
            else if (ch == 'K') letter |= UpperLetter.K;
            else if (ch == 'L') letter |= UpperLetter.L;
            else if (ch == 'M') letter |= UpperLetter.M;
            else if (ch == 'N') letter |= UpperLetter.N;
            else if (ch == 'O') letter |= UpperLetter.O;
            else if (ch == 'P') letter |= UpperLetter.P;
            else if (ch == 'Q') letter |= UpperLetter.Q;
            else if (ch == 'R') letter |= UpperLetter.R;
            else if (ch == 'S') letter |= UpperLetter.S;
            else if (ch == 'T') letter |= UpperLetter.T;
            else if (ch == 'U') letter |= UpperLetter.U;
            else if (ch == 'V') letter |= UpperLetter.V;
            else if (ch == 'W') letter |= UpperLetter.W;
            else if (ch == 'X') letter |= UpperLetter.X;
            else if (ch == 'Y') letter |= UpperLetter.Y;
            else if (ch == 'Z') letter |= UpperLetter.Z;
            //else
            //    throw new NotSupportedException();

            return letter;
        }



        public static void KeepLetters(ref string sourcestr)
        {
            if (!string.IsNullOrEmpty(sourcestr))
            {
                char[] newchars = new char[sourcestr.Length];
                int k = 0;

                for (int i = 0; i < sourcestr.Length; i++)
                {
                    for (int j = 0; j < Letters.Length; j = j + 4)
                    {
                        var current = sourcestr[i];
                        if (current == Letters[j]
                            || current == Letters[j + 1]
                            || current == Letters[j + 2]
                            || current == Letters[j + 3])
                        {
                            newchars[k] = Letters[j];
                            k++;
                        }
                    }
                }

                if (k > 0)
                {
                    sourcestr = new string(newchars, 0, k);
                }
                else
                {
                    sourcestr = string.Empty;
                }
            }
        }



        //在所有的 Spell 中，评估出最全的那些 Spell
        //最全的 Spell 包含被排除的 Spell ，被排除的 Spell 在最全的检索中可检索到
        public static string[] CheckFullSpell(IEnumerable<string> pathspells)
        {
            var spells = pathspells.OrderByDescending(_p => _p.Length).ToArray();

            List<string> final = new List<string>();

            NestCheck(spells, final);

            return final.ToArray();

            void NestCheck(string[] subspells, List<string> results)
            {
                if (subspells.Length > 1)
                {
                    var first = subspells[0];
                    results.Add(first);

                    List<string> result = new List<string>(4);

                    for (int i = 1; i < subspells.Length; i++)
                    {
                        if (ParttenContains(first, subspells[i]))
                        {
                            continue;
                        }

                        result.Add(subspells[i]);
                    }

                    NestCheck(result.ToArray(), results);
                }
                else
                {
                    results.AddRange(subspells);
                }
            }
        }


        //检查 full = ABCDEFG 是否包含 sub = AEG
        //此时返回 true
        public static bool ParttenContains(this string full, string sub)
        {
            if (full.Length >= sub.Length)
            {
                if (full.IndexOf(sub, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }

                for (int i = 0, j = 0; i < sub.Length;)
                {
                    for (; j < full.Length;)
                    {
                        if (sub[i] == full[j])
                        {
                            i++;
                            j++;
                            break;
                        }

                        j++;
                    }

                    if (j == full.Length
                        && i != sub.Length)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
