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
        static void Main(string[] args)
        {
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

            //单次处理demo
            while (true)
            {
                Console.Write("地址：");
                var address = Console.ReadLine();

                
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    List<RegionMatchResult> matchitems = null;
                    for (int i = 0; i < 1; i++)
                    {
                        matchitems = AddressParser.ParsingAddress(address);
                    }
                    sw.Stop();
                    Console.WriteLine(sw.Elapsed);

                    foreach (var matchitem in matchitems)
                    {
                        Console.WriteLine(matchitem);
                        Console.WriteLine("处理：" + AddressParser.FinalCut(matchitem, address));
                    }
                }
                

#if DEBUG
                Console.WriteLine(AddressParser.Statics);
#endif

                Console.WriteLine();
            }



            /*
            //批处理
            {
                var linest = File.ReadAllLines("AddressTest3.txt");

                int process = linest.Length;

                string[] lines = new string[process];
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = linest[i % linest.Length];
                }

                List<RegionMatchResult>[] results = new List<RegionMatchResult>[lines.Length];

                //单线程
                while (true)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    for (int i = 0; i < lines.Length; i++)
                    {
                        results[i] = AddressParser.ParsingAddress(lines[i]);
                    }
                    sw.Stop();
                    Console.WriteLine(sw.Elapsed);
                    Console.ReadKey();
                }
            }
            */
        }
    }
}
