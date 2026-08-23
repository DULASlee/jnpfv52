using System;
using System.Linq;
using System.Reflection;
using JNPF.DynamicApiController;
using JNPF.VisualDev;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// M1 安全网 — 三委托方路由归属契约测试（规格 4.2.2）.
/// 守护 OnlineDev/Base/ShortLink 三个 DynamicApiController 的 Name/Route 契约：
/// 重构期路由面零差异的第一道类型级断言（路由快照为第二道）.
/// 纪律（4.2.1 BR-2）：反射+属性名字符串匹配，零 MVC 类型依赖.
/// </summary>
public class VisualDevRouteOwnerTests
{
    public static TheoryData<Type, string, int> Owners => new()
    {
        // 类型, ApiDescriptionSettings Name, Order
        { typeof(VisualDevService), "Base", 171 },
        { typeof(VisualDevModelDataService), "OnlineDev", 172 },
        { typeof(VisualdevShortLinkService), "ShortLink", 175 },
    };

    [Theory]
    [MemberData(nameof(Owners))]
    public void Owner_NameAndOrder_AreFrozen(Type ownerType, string expectedName, int expectedOrder)
    {
        var attribute = ownerType
            .GetCustomAttributes()
            .FirstOrDefault(a => a.GetType().Name == "ApiDescriptionSettingsAttribute");

        Assert.NotNull(attribute);

        var nameProperty = attribute!.GetType().GetProperty("Name");
        var orderProperty = attribute.GetType().GetProperty("Order");
        var tagProperty = attribute.GetType().GetProperty("Tag");

        Assert.Equal(expectedName, (string?)nameProperty?.GetValue(attribute));
        Assert.Equal(expectedOrder, (int)(orderProperty?.GetValue(attribute) ?? -1));
        Assert.Equal("VisualDev", (string?)tagProperty?.GetValue(attribute));
    }

    [Theory]
    [MemberData(nameof(Owners))]
    public void Owner_RouteTemplate_IsFrozen(Type ownerType, string _, int __)
    {
        var routeAttribute = ownerType
            .GetCustomAttributes()
            .FirstOrDefault(a => a.GetType().Name == "RouteAttribute");

        Assert.NotNull(routeAttribute);

        var templateProperty = routeAttribute!.GetType().GetProperty("Template");
        Assert.Equal("api/visualdev/[controller]", (string?)templateProperty?.GetValue(routeAttribute));
    }

    [Theory]
    [MemberData(nameof(Owners))]
    public void Owner_ImplementsDynamicApiController(Type ownerType, string _, int __)
    {
        Assert.True(typeof(IDynamicApiController).IsAssignableFrom(ownerType),
            $"{ownerType.Name} 必须实现 IDynamicApiController（路由自动映射前提）");
    }
}
