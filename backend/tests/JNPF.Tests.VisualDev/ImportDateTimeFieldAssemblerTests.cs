using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Engine.Import;
using Xunit;

namespace JNPF.Tests.VisualDev;

public class ImportDateTimeFieldAssemblerTests
{
    private static FieldsModel DateField(string format = "yyyy-MM-dd", string label = "日期")
        => new()
        {
            __vModel__ = "f_date",
            format = format,
            __config__ = new ConfigModel
            {
                label = label,
                jnpfKey = JnpfKeyConst.DATE,
                startTimeRule = false,
                endTimeRule = false,
            },
        };

    [Fact]
    public void MapDate_VisualDev_Empty_LeavesKeyUntouched()
    {
        var v = DateField();
        var row = new Dictionary<string, object> { ["f_date"] = "stale" };
        ImportDateTimeFieldAssembler.MapDate(v, "f_date", null, row, row, ImportDateTimeSemantics.VisualDev);
        Assert.Equal("stale", row["f_date"]);
    }

    [Fact]
    public void MapDate_CodeGen_Empty_ClearsToNull()
    {
        var v = DateField();
        var row = new Dictionary<string, object> { ["f_date"] = "stale" };
        ImportDateTimeFieldAssembler.MapDate(v, "f_date", null, row, row, ImportDateTimeSemantics.CodeGen);
        Assert.Null(row["f_date"]);
    }

    [Fact]
    public void MapDate_Valid_WritesUnixTimestamp()
    {
        var v = DateField();
        var row = new Dictionary<string, object>();
        ImportDateTimeFieldAssembler.MapDate(v, "f_date", "2026-08-07", row, row, ImportDateTimeSemantics.VisualDev);
        var expected = DateTime.ParseExact("2026-08-07", "yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture).ParseToUnixTime();
        Assert.Equal(expected, row["f_date"]);
    }

    [Fact]
    public void MapDate_InvalidFormat_AppendsError()
    {
        var v = DateField();
        var row = new Dictionary<string, object>();
        ImportDateTimeFieldAssembler.MapDate(v, "f_date", "not-a-date", row, row, ImportDateTimeSemantics.VisualDev);
        Assert.Equal("日期: 值不正确", row[ImportAssembleErrors.ErrorKey]);
    }

    [Fact]
    public void MapDate_CodeGen_RequiresStartTimeValueToApplyBounds()
    {
        var v = DateField();
        v.__config__.startTimeRule = true;
        v.__config__.startTimeType = 5;
        v.__config__.startTimeTarget = 3;
        v.__config__.startTimeValue = null; // CodeGen gate fails → no range error
        var row = new Dictionary<string, object>();
        ImportDateTimeFieldAssembler.MapDate(v, "f_date", "2000-01-01", row, row, ImportDateTimeSemantics.CodeGen);
        Assert.False(row.ContainsKey(ImportAssembleErrors.ErrorKey));
    }

    [Fact]
    public void MapDate_VisualDev_NullRelation_SkipsMinCompare()
    {
        var v = DateField();
        v.__config__.startTimeRule = true;
        v.__config__.startTimeType = 2;
        v.__config__.startRelationField = "rel";
        var data = new Dictionary<string, object> { ["rel"] = null! };
        var row = new Dictionary<string, object>();
        ImportDateTimeFieldAssembler.MapDate(v, "f_date", "2026-08-07", data, row, ImportDateTimeSemantics.VisualDev);
        Assert.False(row.ContainsKey(ImportAssembleErrors.ErrorKey));
        Assert.True(row.ContainsKey("f_date"));
    }

    [Fact]
    public void MapDate_FutureOffset_OutOfRange()
    {
        var v = DateField();
        v.__config__.startTimeRule = true;
        v.__config__.startTimeType = 5; // now + N days
        v.__config__.startTimeTarget = 3;
        v.__config__.startTimeValue = "30";
        var row = new Dictionary<string, object>();
        ImportDateTimeFieldAssembler.MapDate(v, "f_date", "2020-01-01", row, row, ImportDateTimeSemantics.VisualDev);
        Assert.Contains("日期选择值不在范围内", row[ImportAssembleErrors.ErrorKey].ToString());
    }

    [Fact]
    public void MapTime_CodeGen_Empty_ClearsToNull()
    {
        var v = DateField(format: "HH:mm:ss", label: "时间");
        v.__config__.jnpfKey = JnpfKeyConst.TIME;
        var row = new Dictionary<string, object> { ["f_time"] = "x" };
        ImportDateTimeFieldAssembler.MapTime(v, "f_time", null, row, row, ImportDateTimeSemantics.CodeGen);
        Assert.Null(row["f_time"]);
    }

    [Fact]
    public void MapTime_VisualDev_Empty_LeavesKeyUntouched()
    {
        var v = DateField(format: "HH:mm:ss", label: "时间");
        v.__config__.jnpfKey = JnpfKeyConst.TIME;
        var row = new Dictionary<string, object> { ["f_time"] = "stale" };
        ImportDateTimeFieldAssembler.MapTime(v, "f_time", null, row, row, ImportDateTimeSemantics.VisualDev);
        Assert.Equal("stale", row["f_time"]);
    }

    [Fact]
    public void MapDate_EndOffset_UsesStartTimeTarget_LegacyQuirk()
    {
        // endTimeType=5 (now+N) but offset unit comes from startTimeTarget=3 (days), not endTimeTarget.
        var v = DateField();
        v.__config__.endTimeRule = true;
        v.__config__.endTimeType = 5;
        v.__config__.endTimeValue = "1";
        v.__config__.startTimeTarget = 3; // days
        v.__config__.endTimeTarget = 1; // years — must be ignored for end offset
        var row = new Dictionary<string, object>();
        // Far-future date should be above max = now+1 day
        ImportDateTimeFieldAssembler.MapDate(v, "f_date", "2099-01-01", row, row, ImportDateTimeSemantics.VisualDev);
        Assert.Contains("日期选择值不在范围内", row[ImportAssembleErrors.ErrorKey].ToString());
    }
}
