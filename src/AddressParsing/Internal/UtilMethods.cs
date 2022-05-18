using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AddressParsing
{
    internal static class UtilMethods
    {
        private static char[] _letters = new char[104]
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



        public static void RemoveChars(ref string sourcestr, char[] chars)
        {
            if (!string.IsNullOrEmpty(sourcestr))
            {
                char[] newchars = null;

                int j = 0;
                for (int i = 0; i < sourcestr.Length; i++)
                {
                    if (!chars.Contains(sourcestr[i]))
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
            else
                throw new NotSupportedException();

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
                    for (int j = 0; j < _letters.Length; j = j + 4)
                    {
                        var current = sourcestr[i];
                        if (current == _letters[j]
                            || current == _letters[j + 1]
                            || current == _letters[j + 2]
                            || current == _letters[j + 3])
                        {
                            newchars[k] = _letters[j];
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
