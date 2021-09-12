using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AddressParsing
{
    internal static class ExtensionMethods
    {
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
    }
}
