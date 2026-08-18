using Xunit;

namespace JNPF.Tests.Systems;

public class FileSizeFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1, "1 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.00 KB")]
    [InlineData(1536, "1.50 KB")]
    [InlineData(1048576, "1.00 MB")]
    [InlineData(1073741824, "1.00 GB")]
    public void Format_ReturnsExpected(long bytes, string expected)
    {
        var result = JNPF.Systems.Common.FileSizeFormatter.Format(bytes);
        Assert.Equal(expected, result);
    }
}
