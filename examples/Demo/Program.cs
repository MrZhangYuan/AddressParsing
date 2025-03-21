using AddressParsing;
using Newtonsoft.Json;
using NPinyin;
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
        public static string GetSpellCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return Pinyin.GetInitials(value);
        }





        static void Main(string[] args)
        {
            //自定义 Regions 数据源
            //BasicData.RegionsCreater = () => new List<Region>();

            BasicData.OnBuilding = _p =>
            {
                //设置一级区划匹配优先级，优先匹配的一级区划，该配置优先级小于算法快速命中的优先级
                switch (_p.Name)
                {
                    case "上海市":
                        _p.PrioritySort = 0;
                        break;

                    case "江苏省":
                        _p.PrioritySort = 1;
                        break;

                        //......
                }
                _p.PrioritySort = 99;
            };


            //TestSpellSearch();

            TestParser();

            Console.ReadKey();
        }

        public static void TestSpellSearch()
        {
            while (true)
            {
                Console.Write("字符：");
                var spell = Console.ReadLine();

                Stopwatch sw = Stopwatch.StartNew();

                var results = AddressSearcher.SpellSearch(spell);

                sw.Stop();

                foreach (var item in results)
                {
                    Console.WriteLine(item.Region);
                }

                Console.WriteLine(sw.Elapsed);
                Console.WriteLine();
            }
        }


        public static void TestParser()
        {
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
        }
    }
}
