<div align="center">
  <h1>AddressParsing</h1>
  <p>
    简单地址归一化C#算法
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

## 用法

```csharp
var address = "上海市闵行区浦江镇陈行路2388号浦江科技广场9号楼";
var matchitems = AddressParser.ParsingAddress(address);

//AddressParser.ParsingAddress(address); 方法有可能会匹配到多条记录
foreach (var matchitem in matchitems)
{
	//matchitem 包含一些匹配信息：权重、多个匹配项、路径终点等，已重写ToString()
	Console.WriteLine(matchitem);
	
	//AddressParser.FinalCut(matchitem, address)方法根据匹配结果裁剪得到地址：上海市 - 上海市 - 闵行区 - 浦江镇陈行路2388号浦江科技广场9号楼
	Console.WriteLine(AddressParser.FinalCut(matchitem, address));
}
```

## 输出示例：
```
原始：闸北区大统路938弄6号1301室
上海市 - 上海市 - 闸北区 - 大统路938弄6号1301室
原始：崇明县 陈家镇 陈家镇裕民路 79号
上海市 - 上海市 - 崇明县 - 陈家镇 陈家镇裕民路 79号
原始：虹口区 江湾镇 场中路 803弄4号402室
上海市 - 上海市 - 虹口区 - 江湾镇 场中路 803弄4号402室
原始：浦东新区 高桥镇  潼港一村15号401室
上海市 - 上海市 - 浦东新区 - 高桥镇  潼港一村15号401室
原始：静安区 江宁路街道 陕西北路 525弄4号31室
上海市 - 上海市 - 静安区 - 江宁路街道 陕西北路 525弄4号31室
原始：闸北区 共和新路街道 洛川东路 352弄7号403
上海市 - 上海市 - 闸北区 - 共和新路街道 洛川东路 352弄7号403
原始：闸北区彭浦新村210号甲306室
上海市 - 上海市 - 闸北区 - 彭浦新村210号甲306室
原始：江苏省海门市常乐镇颐生村一组14号
江苏省 - 南通市 - 海门市 - 常乐镇颐生村一组14号
原始：虹口区 四川北街道 多伦路 201弄99号
上海市 - 上海市 - 虹口区 - 四川北街道 多伦路 201弄99号
原始：宝山区 通河街道  呼玛二村193号101室
黑龙江省 - 双鸭山市 - 宝山区 - 通河街道  呼玛二村193号101室
上海市 - 上海市 - 宝山区 - 通河街道  呼玛二村193号101室
```

## 算法解析

### 首先准备一下结构性数据
安徽省	——	宿州市	——	砀山县
		|——	……		|——萧县	
		
### 准备好以上数据结构后，有几个概念需要理解下：
- Name：
	安徽省 -> 安徽省
	宿州市 -> 宿州市
	砀山县 -> 砀山县
- ShortName：可有多个
	安徽省 -> 安徽
	宿州市 -> 宿州
	砀山县 -> 砀山
- PathName：顶级地址不用设置PathName，二三级区划（路径名称按照字符串长度降序排列，其实就是直属的和间接直属的Name和ShortName的组合）：
	宿州市的路径名称为：安徽省宿州市、安徽省宿州、安徽宿州市、安徽宿州
	砀山县的路径名称为：安徽省宿州市砀山县、安徽省宿州市砀山、安徽宿州市砀山县、安徽宿州砀山，宿州市砀山县、宿州砀山、……
- Path：
	安徽省 -> 安徽省
	宿州市 -> 安徽省 - 宿州市
	砀山县 -> 安徽省 - 宿州市 - 砀山县
- PathContains：
	在Path中，下级的Path始终是包含自己和上级的Path，如砀山县的Path：安徽省 - 宿州市 - 砀山县，包含宿州市的Path：安徽省 - 宿州市

### 算法解析
- 预处理
在匹配地址之前首先移除干扰性的分隔符或其他基本不会出现在地址中的字符，如以下地址：
安徽省宿州市砀山县芒砀路999号A幢8楼
安徽省；宿州市；砀山县；芒砀路999号A幢8楼
安徽省 - 宿州市 - 砀山县芒砀路999号A幢8楼
【安徽省】【宿州市】【砀山县】芒砀路999号A幢8楼
[安徽省][宿州市][砀山县]芒砀路999号A幢8楼
安徽省,宿州市,砀山县,芒砀路999号A幢8楼
……
这些地址经过移除常用分割字符后，变为：安徽省宿州市砀山县芒砀路999号A幢8楼，算法内部其实是在处理这个地址。进行这一步的目的是为了后期的PathName匹配
``` csharp
/// <summary>
/// 地址常用分割符，用来首次处理地址时移除
/// </summary>
internal static char[] SplitterChars { get; } = new char[]
{
	'~','!','@','#','$','%','^','&','*','(',')','-','+','_','=',':',';','\'','"','?','|','\\','{','}','[',']','<','>',',','.',' ',
	'！','￥','…','（','）','—','【','】','、','：','；','“','’','《','》','？','，','　'
};
```

- 递归匹配全称和简称












