using System;
using System.Collections.Generic;
using System.Linq;

namespace AddressParsing
{
    public static class ExtensionMethods
    {
        public static List<T> MaxGroup<T>(this IEnumerable<T> source, Func<T, int> keyselector)
        {
            int max = int.MinValue;
            List<T> groups = new List<T>();

            foreach (var item in source)
            {
                var value = keyselector(item);
                if (value > max)
                {
                    groups.Clear();
                    max = value;
                    groups.Add(item);
                }
                else if (value == max)
                {
                    groups.Add(item);
                }
            }

            return groups;
        }

        public static List<T> MinGroup<T>(this IEnumerable<T> source, Func<T, int> keyselector)
        {
            int min = int.MaxValue;
            List<T> groups = new List<T>(3);

            foreach (var item in source)
            {
                var value = keyselector(item);
                if (value < min)
                {
                    groups.Clear();
                    min = value;
                    groups.Add(item);
                }
                else if (value == min)
                {
                    groups.Add(item);
                }
            }

            return groups;
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
