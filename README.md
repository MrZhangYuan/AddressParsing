<div align="center">
  <h1>AddressParsing</h1>
  <p>
    地址结构化C#算法
  </p>
  <p>
    .NET Core (2.0)
  </p>
</div>


## Nuget 安装
- [Nuget](https://www.nuget.org/packages/AddressParsing/)

```
PM> Install-Package AddressParsing
```


## 使用场景

本库的功能就是面对各式各样的地址，准确识别并结构化，并提供结构化的区划详情（邮编、行政代码、区号等）



## 基本原理

**概念：**

**Name：**  
​	安徽省 -> 安徽省
​	宿州市 -> 宿州市  
​	砀山县 -> 砀山县  
**ShortName：**可有多个  
​	安徽省 -> 安徽  
​	宿州市 -> 宿州、宿县  
​	砀山县 -> 砀山  
**PathName：**顶级地址不用设置PathName，二三级区划（路径名称按照字符串长度降序排列，其实就是直属的和间接直属的Name和ShortName的组合）：  
​	宿州市的路径名称为：安徽省宿州市、安徽省宿州、安徽宿州市、安徽宿州  
​	砀山县的路径名称为：安徽省宿州市砀山县、安徽省宿州市砀山、安徽宿州市砀山县、安徽宿州砀山，宿州市砀山县、宿州砀山、……  
**Path：**  
​	安徽省 -> 安徽省  
​	宿州市 -> 安徽省 - 宿州市  
​	砀山县 -> 安徽省 - 宿州市 - 砀山县  
**首先准备以下数据结构：**
![](https://github.com/MrZhangYuan/AddressParsing/blob/master/resources/struct.png)

**拿一个最简单的地址描述一下最基本的原理：**
1. **移除常用分隔符**
![](https://github.com/MrZhangYuan/AddressParsing/blob/master/resources/1.png)

2. **循环和递归匹配**
![](https://github.com/MrZhangYuan/AddressParsing/blob/master/resources/2.png)

3. **结果规则处理**
   若以上两步包含多个结果，进行同路径结果合并，权重计算等
   **没错！整个算法思想就是这么简单，但是我加入了一些小小的技巧（这些技巧有的是一行代码，有的是一块代码，有的是一个设计，具体不在这里描述，需要了解请阅读源码），使得算法在面对大量形态各异的地址文本时，无论是匹配的精准度还是性能都有着极强的表现：**

    手工精调的完备字典
    近乎全覆盖的数据组合
    地址格式化
    地址裁剪
    一二级索引快速命中（v2.0beta）
    循环优先级
    匹配模式优先级
    同级PathName优先级
    Name、ShortName、PathName优先级匹配
    可配置的一级区划优先级
    ShortName匹配跳跃（v2.0beta）
    PathName匹配跳跃（v2.0beta）
    特殊匹配名称排除
    同路径合并
    命中索引优先级
    命中字数优先级
    命中等级优先级
    ......



## **优点**

高精确度、高性能、字典完备、使用场景广泛



## **性能**

**配置i7-8750H，单核最高睿频4.1，内存随意，以下测试在Debug模式（Release会快30%）**  
1：一二级区划（省、自治区、直辖市，市）开头的地址，单次匹配≈0.017毫秒，每秒可处理地址数量：60000
**MatchType** 基本都是 **PathName** 具有最高的优先级  
从 **IndexQuickTop2Matched**、**IndexQuickMatched** 可以看出有效的命中了索引（此索引非彼索引，此索引为数组位置）  
从 **MatchLoopTimes**、**CallStringIndexOfTimes** 可以看出只进行了极少的循环匹配次数，在 **54000+** 的数据组合中基本算是“直达”的了。
```
地址：江苏省南京市江宁区天元中路168号诚基大厦利源集团
TimeCost：      00:00:00.0000173
Weight:         1
MatchType:      PathName
MatchIndex:     0
MatchName:      江苏省南京市江宁区
MatchRegion:    江苏省 - 南京市 - 江宁区
Format：        江苏省 - 南京市 - 江宁区 - 天元中路168号诚基大厦利源集团
CallStringIndexOfTimes：        9
MatchLoopTimes：                3
PathNameSkip：                  0
IndexQuickTop2Matched：         1
IndexQuickMatched：             2

地址：四川省成都市高新区天府大道天府软件园E区6栋10楼12号
TimeCost：      00:00:00.0000259
Weight:         1
MatchType:      PathName
MatchIndex:     0
MatchName:      四川省成都市高新区
MatchRegion:    四川省 - 成都市 - 武侯区
Format：        四川省 - 成都市 - 武侯区 - 天府大道天府软件园E区6栋10楼12号
CallStringIndexOfTimes：        12
MatchLoopTimes：                6
PathNameSkip：                  0
IndexQuickTop2Matched：         1
IndexQuickMatched：             1

地址：湖北省武汉市洪山区武大科技园兴业楼南楼二单元702
TimeCost：      00:00:00.0000162
Weight:         1
MatchType:      PathName
MatchIndex:     0
MatchName:      湖北省武汉市洪山区
MatchRegion:    湖北省 - 武汉市 - 洪山区
Format：        湖北省 - 武汉市 - 洪山区 - 武大科技园兴业楼南楼二单元702
CallStringIndexOfTimes：        9
MatchLoopTimes：                3
PathNameSkip：                  0
IndexQuickTop2Matched：         1
IndexQuickMatched：             2

地址：北京市海淀区文慧园北路8号庆亚大厦A座2层
TimeCost：      00:00:00.0000175
Weight:         1
MatchType:      PathName
MatchIndex:     0
MatchName:      北京市海淀区
MatchRegion:    北京市 - 北京市 - 海淀区
Format：        北京市 - 北京市 - 海淀区 - 文慧园北路8号庆亚大厦A座2层
CallStringIndexOfTimes：        20
MatchLoopTimes：                10
PathNameSkip：                  0
IndexQuickTop2Matched：         1
IndexQuickMatched：             0

地址：上海市浦东新区亮景路232号A幢5楼森亿智能
TimeCost：      00:00:00.0000187
Weight:         1
MatchType:      PathName
MatchIndex:     0
MatchName:      上海市浦东新区
MatchRegion:    上海市 - 上海市 - 浦东新区
Format：        上海市 - 上海市 - 浦东新区 - 亮景路232号A幢5楼森亿智能
CallStringIndexOfTimes：        14
MatchLoopTimes：                15
PathNameSkip：                  0
IndexQuickTop2Matched：         1
IndexQuickMatched：             0
```
2：三级区划（市辖区、县）开头的地址，由于三级区划数量太多，重名概率极大未设计快速索引，单次匹配≈0.4毫秒，每秒处理地址数量：2500  
可以看出 **CallStringIndexOfTimes**、**MatchLoopTimes** 明显很大，基本算是“暴力循环，选择匹配”了。  
有的虽然 **IndexQuickTop2Matched** 但是 **MatchType** 是 **Name** 优先级较低，还需要继续匹配
```
地址：番禺区南村镇南光路32号A首层
TimeCost：      00:00:00.0004676
Weight:         1
MatchType:      Name
MatchIndex:     0
MatchName:      番禺区
MatchRegion:    广东省 - 广州市 - 番禺区
Format：        广东省 - 广州市 - 番禺区 - 南村镇南光路32号A首层
CallStringIndexOfTimes：        151
MatchLoopTimes：                3323
PathNameSkip：                  15
IndexQuickTop2Matched：         0
IndexQuickMatched：             1

地址：朝阳区东大桥路8号尚都国际中心2806室 ,100-499人
TimeCost：      00:00:00.0004420
Weight:         1
MatchType:      Name
MatchIndex:     0
MatchName:      朝阳区
MatchRegion:    吉林省 - 长春市 - 朝阳区
Format：        吉林省 - 长春市 - 朝阳区 - 东大桥路8号尚都国际中心2806室 ,100-499人
Weight:         1
MatchType:      Name
MatchIndex:     0
MatchName:      朝阳区
MatchRegion:    北京市 - 北京市 - 朝阳区
Format：        北京市 - 北京市 - 朝阳区 - 东大桥路8号尚都国际中心2806室 ,100-499人
CallStringIndexOfTimes：        316
MatchLoopTimes：                3323
PathNameSkip：                  24
IndexQuickTop2Matched：         1
IndexQuickMatched：             2

地址：长泰县武安镇官山工业园
TimeCost：      00:00:00.0004007
Weight:         1
MatchType:      Name
MatchIndex:     0
MatchName:      长泰县
MatchRegion:    福建省 - 漳州市 - 长泰县
Format：        福建省 - 漳州市 - 长泰县 - 武安镇官山工业园
CallStringIndexOfTimes：        304
MatchLoopTimes：                3323
PathNameSkip：                  20
IndexQuickTop2Matched：         0
IndexQuickMatched：             1

地址：蓝山县塔峰镇滨河大道教师进修学校东侧50米国仕地产
TimeCost：      00:00:00.0004102
Weight:         1
MatchType:      Name
MatchIndex:     0
MatchName:      蓝山县
MatchRegion:    湖南省 - 永州市 - 蓝山县
Format：        湖南省 - 永州市 - 蓝山县 - 塔峰镇滨河大道教师进修学校东侧50米国仕地产
CallStringIndexOfTimes：        340
MatchLoopTimes：                3323
PathNameSkip：                  10
IndexQuickTop2Matched：         0
IndexQuickMatched：             1

地址：砀山赵屯镇张新庄行政村888号
TimeCost：      00:00:00.0003866
Weight:         1
MatchType:      ShortName
MatchIndex:     0
MatchName:      砀山
MatchRegion:    安徽省 - 宿州市 - 砀山县
Format：        安徽省 - 宿州市 - 砀山县 - 赵屯镇张新庄行政村888号
CallStringIndexOfTimes：        204
MatchLoopTimes：                3323
PathNameSkip：                  16
IndexQuickTop2Matched：         0
IndexQuickMatched：             1
```
**综合地址持续平均处理能力在 20000/s**



## **示例**

以安徽省宿州市砀山县赵屯镇张新庄行政村888号为例：
标准三级地址或包含适当分隔符
```
地址：安徽省宿州市砀山县赵屯镇张新庄行政村888号
TimeCost：      00:00:00.0000286
Weight:         1
MatchType:      PathName
MatchIndex:     0
MatchName:      安徽省宿州市砀山县
MatchRegion:    安徽省 - 宿州市 - 砀山县
Format：        安徽省 - 宿州市 - 砀山县 - 赵屯镇张新庄行政村888号
CallStringIndexOfTimes：        18
MatchLoopTimes：                87
PathNameSkip：                  0
IndexQuickTop2Matched：         1
IndexQuickMatched：             1

地址：[安徽省][宿州市][砀山县]赵屯镇张新庄行政村888号
TimeCost：      00:00:00.0000273
Weight:         1
MatchType:      PathName
MatchIndex:     0
MatchName:      安徽省宿州市砀山县
MatchRegion:    安徽省 - 宿州市 - 砀山县
Format：        安徽省 - 宿州市 - 砀山县 - [安徽省][宿州市][砀山县]赵屯镇张新庄行政村888号
CallStringIndexOfTimes：        18
MatchLoopTimes：                87
PathNameSkip：                  0
IndexQuickTop2Matched：         1
IndexQuickMatched：             1
```

省去一些级别的区划或加入其它干扰字符，可以看出**MatchLoopTimes**明显变大，表名优先级不能明确确定，需要更多的处理和比对，因为 **MatchType = ShortName** 是无法确定一个地址的，最后一个匹配会匹配到 **ShortName = 砀山 和 PathName = 河南开封** 所以取了河南开封，这在预期之内。  
以下示例中 MatchName:砀山 并不准确，其实部分也会命中 安徽、宿州等，只不过最后做了同路径合并，所以 **Weight** 会大于1  
另外一些规则这里不做赘述，请参见源码。

```
地址：宿州市砀山县赵屯镇张新庄行政村888号
TimeCost：      00:00:00.0000161
Weight:         1
MatchType:      PathName
MatchIndex:     0
MatchName:      宿州市砀山县
MatchRegion:    安徽省 - 宿州市 - 砀山县
Format：        安徽省 - 宿州市 - 砀山县 - 赵屯镇张新庄行政村888号
CallStringIndexOfTimes：        7
MatchLoopTimes：                3
PathNameSkip：                  17
IndexQuickTop2Matched：         1
IndexQuickMatched：             1

地址：砀山赵屯镇张新庄行政村888号
TimeCost：      00:00:00.0003889
Weight:         1
MatchType:      ShortName
MatchIndex:     0
MatchName:      砀山
MatchRegion:    安徽省 - 宿州市 - 砀山县
Format：        安徽省 - 宿州市 - 砀山县 - 赵屯镇张新庄行政村888号
CallStringIndexOfTimes：        204
MatchLoopTimes：                3323
PathNameSkip：                  16
IndexQuickTop2Matched：         0
IndexQuickMatched：             1

地址：安徽de宿州哈哈砀山呵呵赵屯镇张新庄行政村888号
TimeCost：      00:00:00.0004405
Weight:         1
MatchType:      ShortName
MatchIndex:     8
MatchName:      砀山
MatchRegion:    安徽省 - 宿州市 - 砀山县
Format：        安徽省 - 宿州市 - 砀山县 - 安徽de宿州哈哈呵呵赵屯镇张新庄行政村888号
CallStringIndexOfTimes：        360
MatchLoopTimes：                3323
PathNameSkip：                  0
IndexQuickTop2Matched：         1
IndexQuickMatched：             0

地址：安徽de哈哈砀山呵呵赵屯镇张新庄行政村888号
TimeCost：      00:00:00.0004607
Weight:         1
MatchType:      ShortName
MatchIndex:     6
MatchName:      砀山
MatchRegion:    安徽省 - 宿州市 - 砀山县
Format：        安徽省 - 宿州市 - 砀山县 - 安徽de哈哈呵呵赵屯镇张新庄行政村888号
CallStringIndexOfTimes：        340
MatchLoopTimes：                3323
PathNameSkip：                  3
IndexQuickTop2Matched：         1
IndexQuickMatched：             0

地址：我家在砀山呵呵赵屯镇张新庄行政村888号
TimeCost：      00:00:00.0003966
Weight:         1
MatchType:      ShortName
MatchIndex:     3
MatchName:      砀山
MatchRegion:    安徽省 - 宿州市 - 砀山县
Format：        安徽省 - 宿州市 - 砀山县 - 我家在呵呵赵屯镇张新庄行政村888号
CallStringIndexOfTimes：        196
MatchLoopTimes：                3323
PathNameSkip：                  16
IndexQuickTop2Matched：         0
IndexQuickMatched：             0

地址：我家在砀山呵呵赵屯，你吃饭了吗镇他家在河南张新庄行政村888号
TimeCost：      00:00:00.0004397
Weight:         1
MatchType:      ShortName
MatchIndex:     3
MatchName:      砀山
MatchRegion:    安徽省 - 宿州市 - 砀山县
Format：        安徽省 - 宿州市 - 砀山县 - 我家在呵呵赵屯，你吃饭了吗镇他家在河南张新庄行政村888号
CallStringIndexOfTimes：        335
MatchLoopTimes：                3323
PathNameSkip：                  16
IndexQuickTop2Matched：         0
IndexQuickMatched：             0

地址：我的家在安徽，在它最北部有一个叫做宿州市的地方，市最北部有一个县叫“砀山县”，那里就是我家。砀山人民欢迎您
TimeCost：      00:00:00.0069626
Weight:         2
MatchType:      Name
MatchIndex:     31
MatchName:      砀山县
MatchRegion:    安徽省 - 宿州市 - 砀山县
Format：        安徽省 - 宿州市 - 砀山县 - 我的家在安徽，在它最北部有一个叫做宿州市的地方，市最北部有一个县叫“”，那里 就是我家。砀山人民欢迎您
CallStringIndexOfTimes：        242
MatchLoopTimes：                3323
PathNameSkip：                  0
IndexQuickTop2Matched：         0
IndexQuickMatched：             0

地址：我家在砀山呵呵赵屯，你吃饭了吗镇他家在河南开封张新庄行政村888号
TimeCost：      00:00:00.0002066
Weight:         1
MatchType:      PathName
MatchIndex:     18
MatchName:      河南开封
MatchRegion:    河南省 - 开封市
Format：        河南省 - 开封市 - 我家在砀山呵呵赵屯，你吃饭了吗镇他家在张新庄行政村888号
CallStringIndexOfTimes：        175
MatchLoopTimes：                1388
PathNameSkip：                  16
IndexQuickTop2Matched：         0
IndexQuickMatched：             1
```



## 用法

```csharp
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
```



## 题外话

算法内部包装了什么高级算法吗？没有！！！核心就是`string.IndexOf()`方法



## **What's Next**

v2.0：准确度不变，一二级区划快速索引，二级区划开头地址平均性能性能提升1000%
v？？？：准确度不变，设计三级地址快速索引，预计性能提升至一二级 50000/s 的水平