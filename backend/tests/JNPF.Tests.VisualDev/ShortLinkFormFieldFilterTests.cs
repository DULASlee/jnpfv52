using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Engine.Core;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class ShortLinkFormFieldFilterTests
{
    [Fact]
    public void Apply_KeepsComInput_DropsPopupTableSelect()
    {
        var form = new List<FieldsModel>
        {
            Field(JnpfKeyConst.COMINPUT),
            Field(JnpfKeyConst.POPUPTABLESELECT),
            Field(JnpfKeyConst.NUMINPUT),
        };

        var filtered = ShortLinkFormFieldFilter.Apply(form);
        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain(filtered, x => x.__config__.jnpfKey == JnpfKeyConst.POPUPTABLESELECT);
        Assert.Contains(filtered, x => x.__config__.jnpfKey == JnpfKeyConst.COMINPUT);
        Assert.Contains(filtered, x => x.__config__.jnpfKey == JnpfKeyConst.NUMINPUT);
    }

    [Fact]
    public void Apply_KeepsStaticSelect_DropsDynamicSelect()
    {
        var form = new List<FieldsModel>
        {
            Field(JnpfKeyConst.SELECT, "static"),
            Field(JnpfKeyConst.SELECT, "dynamic"),
        };

        var filtered = ShortLinkFormFieldFilter.Apply(form);
        Assert.Single(filtered);
        Assert.Equal("static", filtered[0].__config__.dataType);
    }

    private static FieldsModel Field(string jnpfKey, string dataType = "static")
        => new()
        {
            __config__ = new ConfigModel { jnpfKey = jnpfKey, dataType = dataType },
        };
}
