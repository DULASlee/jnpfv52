using JNPF.Common.Models.InteAssistant;
using Xunit;

namespace JNPF.Tests.Architecture;

/// <summary>
/// Characterization: Message.Interfaces boundary DTO mapping (W4 Message cut).
/// </summary>
public sealed class IntegrateTaskMessageDtoTests
{
    [Fact]
    public void From_BothNull_StillReturnsDto()
    {
        var dto = IntegrateTaskMessageDto.From(null, null);
        Assert.NotNull(dto);
        Assert.Null(dto.Data);
        Assert.Null(dto.TemplateJson);
    }

    [Fact]
    public void From_DataOnly_PreservesData()
    {
        var dto = IntegrateTaskMessageDto.From("[]", null);
        Assert.Equal("[]", dto.Data);
        Assert.Null(dto.TemplateJson);
    }

    [Fact]
    public void From_TemplateOnly_PreservesTemplate()
    {
        var dto = IntegrateTaskMessageDto.From(null, "{}");
        Assert.Null(dto.Data);
        Assert.Equal("{}", dto.TemplateJson);
    }

    [Fact]
    public void From_Both_PreservesFields()
    {
        var dto = IntegrateTaskMessageDto.From("[{\"Data\":\"{}\"}]", "{\"properties\":{}}");
        Assert.Equal("[{\"Data\":\"{}\"}]", dto.Data);
        Assert.Equal("{\"properties\":{}}", dto.TemplateJson);
    }
}
