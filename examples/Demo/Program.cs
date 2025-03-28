using AddressParsing;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPinyin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Demo
{
    public class AdministrativeDivision
    {
        public int ID { get; set; }
        public string CODE { get; set; }
        public string NAME { get; set; }
        public string SHOW_NAME { get; set; }
        public int? LEVEL { get; set; }
        public string SPELL_CODE { get; set; }
        public string DIV_CODE { get; set; }
        public string AREA_CODE { get; set; }
        public string ZIP_CODE { get; set; }
        public string SHORT_NAMES { get; set; }
        public string SHORT_NAMES_SPELL { get; set; }
        public string PARENT_CODE { get; set; }
        public int? PRIORITY_SORT { get; set; }
        public int? SORT { get; set; }
        public string SUFFIX { get; set; }

        public List<AdministrativeDivision> Children { get; set; }

        public AdministrativeDivision()
        {
            Children = new List<AdministrativeDivision>();
        }

        public override string ToString()
        {
            return this.NAME;
        }
    }


    public class SuffFix
    {
        public readonly static string[] LV1_SuffFix =
                                        {
                                            "省",
                                            "市",
                                            "自治区",
                                            "行政区",
                                            "港澳台",
                                            "台湾",
                                            "钓鱼岛",
                                            ""
                                        };
        public readonly static string[] LV2_SuffFix =
                                        {
                                            "市",
                                            "县",
                                            "市辖区",
                                            "区",
                                            "州",
                                            "盟",
                                            "沟",
                                            ""
                                        };
        public readonly static string[] LV3_SuffFix =
                                        {
                                            "县",
                                            "区",
                                            "市",
                                            "旗",
                                            "镇",
                                            "街道",
                                            "群岛",
                                            "东莞港",
                                            "管委会",
                                            "海域",
                                            "生态园",
                                            ""
                                        };

        public readonly static string[] LV4_SuffFix = { };
    }


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

            /*
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
            */

            //TestSpellSearch();

            //TestParser();

            //Test();

            TestRead();

            Console.ReadKey();
        }

        public static void TestRead()
        {
            using (var conn = new SqlConnection(""))
            {
                conn.Open();

                var dict = conn.Query<AdministrativeDivision>("select * from SYS_CHINA_AREAS")
                            .Select(_p =>
                                new Region(
                                        _p.CODE,
                                        string.IsNullOrEmpty(_p.PARENT_CODE) ? "0" : _p.PARENT_CODE,
                                        _p.NAME,
                                        GetSpellCode(_p.NAME),
                                        _p.LEVEL.GetValueOrDefault(0),
                                        _p.SHORT_NAMES.Split("|"),
                                        _p.SHORT_NAMES_SPELL.Split("|")
                                    )
                            )
                            .ToList();

                Console.WriteLine("初始化开始......");
                Stopwatch sw11 = Stopwatch.StartNew();
                AddressParser.Build(dict);
                sw11.Stop();
                Console.WriteLine($"初始化完成：{sw11.Elapsed}");

                while (true)
                {
                    Console.Write("输入：");
                    var address = Console.ReadLine();

                    var sw = Stopwatch.StartNew();
                    var res = AddressParser.ParsingAddress(address);
                    sw.Stop();

                    //if (res.Count > 0)
                    //{
                    //    foreach (var item in res)
                    //    {
                    //        Console.WriteLine($"{item.Level} - {item.Name} - {sw.Elapsed}");
                    //    }
                    //}
                    //else
                    //{
                    //    Console.WriteLine("未匹配");
                    //}
                }













            }
        }

        public static void Test()
        {
            var txt = RegionData.ReadRegionsFile4().Substring(1);
            var regions = JsonConvert.DeserializeObject<List<Region>>(txt);

            //var dsds = AddressParser.ParsingAddress("");
            //var regions = BasicData.Regions;

            var dsds = SuffFix.LV1_SuffFix.Concat(SuffFix.LV2_SuffFix).Concat(SuffFix.LV3_SuffFix).Distinct().OrderByDescending(_p => _p.Length).ToArray();

            var dbitems = regions
                            .Select(_p => new AdministrativeDivision
                            {
                                CODE = _p.ID,
                                NAME = _p.Name,
                                SHOW_NAME = _p.Name,
                                AREA_CODE = _p.AreaCode,
                                DIV_CODE = _p.AdDivCode,
                                ZIP_CODE = _p.ZipCode,
                                PARENT_CODE = _p.ParentID,
                                LEVEL = _p.Level,
                                PRIORITY_SORT = 0,
                                SORT = 0,
                                SPELL_CODE = _p.NameSpell,
                                SHORT_NAMES = _p.ShortNames != null ? string.Join('|', _p.ShortNames) : "",
                                SHORT_NAMES_SPELL = _p.ShortNamesSpell != null ? string.Join('|', _p.ShortNamesSpell) : ""
                            })
                            .ToList();

            foreach (var item in dbitems)
            {
                foreach (var suffix in dsds)
                {
                    if (item.NAME.EndsWith(suffix))
                    {
                        item.SUFFIX = suffix;
                        break;
                    }
                }
            }

            var sql = @"
INSERT INTO SYS_CHINA_AREAS(
	CODE,
	NAME,
	SHOW_NAME,
	[LEVEL],
	SPELL_CODE,
	DIV_CODE,
	AREA_CODE,
	ZIP_CODE,
	SHORT_NAMES,
	SHORT_NAMES_SPELL,
	PARENT_CODE,
	PRIORITY_SORT,
	SORT,
    SUFFIX)
VALUES(
	@CODE,
	@NAME,
	@SHOW_NAME,
	@LEVEL,
	@SPELL_CODE,
	@DIV_CODE,
	@AREA_CODE,
	@ZIP_CODE,
	@SHORT_NAMES,
	@SHORT_NAMES_SPELL,
	@PARENT_CODE,
	@PRIORITY_SORT,
	@SORT,
    @SUFFIX)
";

            using (var conn = new SqlConnection("Server=192.168.222.128;Database=SYNC_EMR;User Id=sa;Password=!@#QWEasd;TrustServerCertificate=true"))
            {
                conn.Open();

                for (int i = 0; i < dbitems.Count; i++)
                {
                    conn.ExecuteScalar(sql, dbitems[i]);
                }
            }
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


        //        public static void TestParser()
        //        {
        //            while (true)
        //            {
        //                Console.Write("地址：");
        //                var address = Console.ReadLine();

        //                Stopwatch sw = Stopwatch.StartNew();
        //                List<RegionMatchResult> matchitems = AddressParser.ParsingAddress(address);
        //                sw.Stop();
        //                Console.WriteLine("TimeCost：\t" + sw.Elapsed);

        //                foreach (var matchitem in matchitems)
        //                {
        //                    Console.WriteLine(matchitem);
        //                    Console.WriteLine("Format：\t" + AddressParser.Format(matchitem, address));
        //                }

        //#if DEBUG
        //                Console.WriteLine(AddressParser.Statics);
        //#endif

        //                Console.WriteLine();
        //            }
        //        }
    }
}
