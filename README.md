<div align="center">
  <h1>AddressParsing</h1>
  <p>
    地址归一化C#简单算法
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

## 简单用法

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

# 简单的输出样例：
```csharp
原始：闸北区大统路938弄6号1301室
上海市 - 上海市 - 闸北区 # 大统路938弄6号1301室

原始：崇明县 陈家镇 陈家镇裕民路 79号
上海市 - 上海市 - 崇明县 #  陈家镇 陈家镇裕民路 79号

原始：虹口区 江湾镇 场中路 803弄4号402室
上海市 - 上海市 - 虹口区 #  江湾镇 场中路 803弄4号402室

原始：浦东新区 高桥镇  潼港一村15号401室
上海市 - 上海市 - 浦东新区 #  高桥镇  潼港一村15号401室

原始：静安区 江宁路街道 陕西北路 525弄4号31室
上海市 - 上海市 - 静安区 #  江宁路街道 陕西北路 525弄4号31室

原始：闸北区 共和新路街道 洛川东路 352弄7号403
上海市 - 上海市 - 闸北区 #  共和新路街道 洛川东路 352弄7号403

原始：闸北区彭浦新村210号甲306室
上海市 - 上海市 - 闸北区 # 彭浦新村210号甲306室

原始：江苏省海门市常乐镇颐生村一组14号
江苏省 - 南通市 - 海门市 # 常乐镇颐生村一组14号

原始：虹口区 四川北街道 多伦路 201弄99号
上海市 - 上海市 - 虹口区 #  四川北街道 多伦路 201弄99号

原始：宝山区 通河街道  呼玛二村193号101室
黑龙江省 - 双鸭山市 - 宝山区 #  通河街道  呼玛二村193号101室
上海市 - 上海市 - 宝山区 #  通河街道  呼玛二村193号101室
```

# 算法解析（待补充）



















