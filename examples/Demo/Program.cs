using AddressParsing;
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
            Console.BufferHeight = 30000;

            //var lines = File.ReadAllLines("AddressTest3.txt").Where(_p => !string.IsNullOrEmpty(_p)).ToList();
            //List<List<string>> results = new List<List<string>>(lines.Count * 3 + 300);
            //foreach (var address in lines)
            //{
            //    var matchitems = AddressParser.ParsingAddress(address);
            //    List<string> result = new List<string>(5);
            //    result.Add("原始：" + address);
            //    if (matchitems.Count > 0)
            //    {
            //        foreach (var matchitem in matchitems)
            //        {
            //            result.Add(AddressParser.FinalCut(matchitem, address));
            //        }
            //    }
            //    result.Add(Environment.NewLine);
            //    results.Add(result);
            //}
            //File.WriteAllLines("AddressResult.txt", results.SelectMany(_p => _p).ToArray());
            //foreach (var item in results.SelectMany(_p => _p))
            //{
            //    Console.WriteLine(item);
            //}
            //Console.WriteLine("完成");
            //Console.ReadKey();




            //var lines = File.ReadAllLines("AddressTest3.txt").Where(_p => !string.IsNullOrEmpty(_p)).ToList();
            //int mutcount = 0;
            //int nocount = 0;
            //foreach (var address in lines)
            //{
            //    var matchitems = AddressParser.ParsingAddress(address);
            //    if (matchitems.Count > 1)
            //    {
            //        Console.WriteLine("原始：" + address);
            //        mutcount++;
            //        foreach (var matchitem in matchitems)
            //        {
            //            Console.WriteLine(AddressParser.FinalCut(matchitem, address));
            //        }
            //        Console.WriteLine();
            //    }

            //    //if (matchitems.Count == 1)
            //    //{
            //    //    Console.WriteLine("原始：" + address);
            //    //    foreach (var matchitem in matchitems)
            //    //    {
            //    //        Console.WriteLine(AddressParser.FinalCut(matchitem, address));
            //    //    }
            //    //    Console.WriteLine();
            //    //}

            //    //if (matchitems.Count == 0)
            //    //{
            //    //    Console.WriteLine("原始：" + address);
            //    //    nocount++;
            //    //    Console.WriteLine();
            //    //}
            //}
            //Console.WriteLine("完成");
            //Console.ReadKey();


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
                Console.WriteLine();
            }
        }

    }
}
