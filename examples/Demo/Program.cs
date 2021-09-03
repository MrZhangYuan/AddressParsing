using AddressParsing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Demo
{
    class Program
    {

        public static IEnumerable<string> Ranges(string address, int length)
        {
            for (int i = 0; i <= address.Length - length; i++)
            {
                yield return address.Substring(i, length);
            }
        }

        static void Main(string[] args)
        {


            while (true)
            {

                int dsds = 0;
                Console.Write("输入：");
                string key = Console.ReadLine();

                Stopwatch sw = Stopwatch.StartNew();


                //for (int i = 0; i < length.Count; i++)
                //{
                //    if (length[i] <= key.Length)
                //    {
                //        foreach (var range in Ranges(key, length[i]))
                //        {
                //            dsds++;
                //            AddressParser._pathNameKeyedRegions.TryGetValue(range, out var list);
                //        }
                //    }
                //}


                bool flag = false;

                for (int i = 0; i < AddressParser._nameSortedLength.Count; i++)
                {
                    if (AddressParser._nameSortedLength[i] <= key.Length)
                    {

                        for (int j = 0; j <= key.Length - AddressParser._nameSortedLength[i]; j++)
                        {
                            dsds++;
                            if (AddressParser._nameKeyedRegions.TryGetValue(key.Substring(j, AddressParser._nameSortedLength[i]), out var list))
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                }


                if (!flag)
                {
                    for (int i = 0; i < AddressParser._shortNameSortedLength.Count; i++)
                    {
                        if (AddressParser._shortNameSortedLength[i] <= key.Length)
                        {

                            for (int j = 0; j <= key.Length - AddressParser._shortNameSortedLength[i]; j++)
                            {
                                dsds++;
                                if (AddressParser._shortNameKeyedRegions.TryGetValue(key.Substring(j, AddressParser._shortNameSortedLength[i]), out var list))
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                Region result = null;
                if (flag)
                {
                    for (int i = 0; i < AddressParser._pathNameSortedLength.Count; i++)
                    {
                        if (AddressParser._pathNameSortedLength[i] <= key.Length)
                        {

                            for (int j = 0; j <= key.Length - AddressParser._pathNameSortedLength[i]; j++)
                            {
                                dsds++;
                                if (AddressParser._pathNameKeyedRegions.TryGetValue(key.Substring(j, AddressParser._pathNameSortedLength[i]), out var list))
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                    }
                }


                //for (int i = 0; i < 10000; i++)
                //{
                //    AddressParser._pathNameKeyedRegions.TryGetValue(key, out var list);
                //}
                sw.Stop();

                Console.WriteLine(sw.Elapsed);
                Console.WriteLine(dsds);
                if (result != null)
                {
                    Console.WriteLine(result);
                }
            }























            //设置一级区划匹配优先级，当地址不是以一级区划（省、直辖市、自治区）开头时，优先匹配的一级区划
            AddressParser.MakePrioritySort(
                    _p =>
                    {
                        switch (_p.Name)
                        {
                            case "上海市":
                                return 0;

                            case "江苏省":
                                return 1;

                                //......
                        }

                        return 99;
                    }
                );

            while (true)
            {
                Console.Write("地址：");
                var address = Console.ReadLine();

                Stopwatch sw = Stopwatch.StartNew();
                var matchitems = AddressParser.ParsingAddress(address);
                sw.Stop();
                Console.WriteLine(sw.Elapsed);

                foreach (var matchitem in matchitems)
                {
                    Console.WriteLine(matchitem);
                    Console.WriteLine("处理：" + AddressParser.FinalCut(matchitem, address));
                }


                Console.WriteLine(AddressParser.Statics);

                Console.WriteLine();
            }
        }
    }
}
