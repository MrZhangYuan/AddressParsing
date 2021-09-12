﻿using AddressParsing;
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
        static void Main(string[] args)
        {
            //设置一级区划匹配优先级，优先匹配的一级区划，该配置优先级小于算法快速命中的优先级
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
                List<RegionMatchResult> matchitems = AddressParser.ParsingAddress(address);
                sw.Stop();
                Console.WriteLine("TimeCost：\t" + sw.Elapsed);

                foreach (var matchitem in matchitems)
                {
                    Console.WriteLine(matchitem);
                    Console.WriteLine("Format：\t" + AddressParser.Format(matchitem, address));
                }

#if DEBUG
                Console.WriteLine(AddressParser.Statics);
#endif

                Console.WriteLine();
            }




            //{
            //    var linest = File.ReadAllLines("AddressTest3.txt");

            //    int process = linest.Length;

            //    string[] lines = new string[process];
            //    for (int i = 0; i < lines.Length; i++)
            //    {
            //        lines[i] = linest[i % linest.Length];
            //    }

            //    List<RegionMatchResult>[] results = new List<RegionMatchResult>[lines.Length];

            //    while (true)
            //    {
            //        Stopwatch sw = Stopwatch.StartNew();
            //        for (int i = 0; i < lines.Length; i++)
            //        {
            //            results[i] = AddressParser.ParsingAddress(lines[i]);
            //        }
            //        sw.Stop();
            //        Console.WriteLine(sw.Elapsed);
            //        Console.ReadKey();
            //    }
            //}

        }
    }
}
