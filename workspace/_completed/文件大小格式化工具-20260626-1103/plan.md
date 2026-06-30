# FileSizeFormatter 实施计划

> **Goal:** 新增 `FileSizeFormatter.Format(long)` 静态方法，将字节数转人类可读格式
> **Architecture:** 纯静态工具类，放 `JNPF.Systems.Common` 命名空间，xUnit 测试
> **Tech Stack:** C# 8.0+ / .NET 8 / xUnit 2.7

---

### Task 1: 创建测试项目

**Files:**
- Create: `backend/tests/JNPF.Tests.Systems/JNPF.Tests.Systems.csproj`
- Create: `backend/tests/JNPF.Tests.Systems/FileSizeFormatterTests.cs`

- [ ] **Step 1: 创建 .csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.7.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\modularity\system\JNPF.Systems\JNPF.Systems.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: 验证项目可恢复**

Run: `dotnet restore backend/tests/JNPF.Tests.Systems/JNPF.Tests.Systems.csproj`
Expected: exit 0

---

### Task 2: 编写测试用例

**Files:**
- Modify: `backend/tests/JNPF.Tests.Systems/FileSizeFormatterTests.cs`

- [ ] **Step 1: 写测试代码**

```csharp
using JNPF.Systems.Common;
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
        var result = FileSizeFormatter.Format(bytes);
        Assert.Equal(expected, result);
    }
}
```

- [ ] **Step 2: 运行测试——预期 FAIL（类不存在）**

Run: `dotnet test backend/tests/JNPF.Tests.Systems/JNPF.Tests.Systems.csproj --filter "Format_ReturnsExpected"`
Expected: FAIL with CS0246 or similar

---

### Task 3: 实现 FileSizeFormatter

**Files:**
- Create: `backend/modularity/system/JNPF.Systems/Common/FileSizeFormatter.cs`

- [ ] **Step 1: 写实现代码**

```csharp
using JNPF.DependencyInjection;

namespace JNPF.Systems.Common;

/// <summary>
/// 文件大小格式化工具——将字节数转为人类可读格式.
/// </summary>
[SuppressSniffer]
public static class FileSizeFormatter
{
    private static readonly string[] _suffixes = ["B", "KB", "MB", "GB"];

    /// <summary>
    /// 格式化字节数为人类可读字符串.
    /// </summary>
    public static string Format(long bytes)
    {
        if (bytes < 0)
            return "0 B";

        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < _suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return order == 0
            ? $"{bytes} B"
            : $"{size:F2} {_suffixes[order]}";
    }
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build backend/modularity/system/JNPF.Systems/JNPF.Systems.csproj --nologo -v q`
Expected: exit 0, 0 errors

---

### Task 4: 运行测试——全部通过

- [ ] **Step 1: 运行完整测试**

Run: `dotnet test backend/tests/JNPF.Tests.Systems/JNPF.Tests.Systems.csproj --nologo -v q`
Expected: 7 passed, 0 failed, 0 skipped

- [ ] **Step 2: 确认通过后提交**

```bash
git add backend/tests/JNPF.Tests.Systems/ backend/modularity/system/JNPF.Systems/Common/FileSizeFormatter.cs
git commit -m "feat: add FileSizeFormatter with xUnit tests"
```
