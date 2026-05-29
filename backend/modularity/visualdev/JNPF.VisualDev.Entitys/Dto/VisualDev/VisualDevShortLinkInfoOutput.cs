using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 功能信息输出.
/// </summary>
[SuppressSniffer]
public class VisualDevShortLinkInfoOutput
{
    public string id { get; set; }

    public string shortLink { get; set; }

    public int formUse { get; set; } = 0;

    public string formLink { get; set; }

    public int formPassUse { get; set; } = 0;

    public string formPassword { get; set; }

    public int columnUse { get; set; } = 0;

    public string columnLink { get; set; }

    public int columnPassUse { get; set; } = 0;

    public string columnPassword { get; set; }

    public string columnCondition { get; set; }

    public string columnText { get; set; }

    public string userId { get; set; }

    public string tenantId { get; set; }

    public int enabledMark { get; set; }

    /// <summary>
    /// 是否已保存.
    /// </summary>
    public bool alreadySave { get; set; } = false;
}

/// <summary>
/// 获取外链配置输出.
/// </summary>
public class VisualdevShortLinkConfigOutput
{
    public string id { get; set; }
    public int formUse { get; set; } = 0;
    public int formPassUse { get; set; } = 0;
    public int columnUse { get; set; } = 0;
    public int columnPassUse { get; set; } = 0;
    public string columnCondition { get; set; }
    public string columnText { get; set; }
    public string userId { get; set; }
    public string tenantId { get; set; }
    public int enabledMark { get; set; }
    public string formLink { get; set; }
    public string columnLink { get; set; }
}

/// <summary>
/// 获取外链配置输出.
/// </summary>
public class VisualdevShortLinkFormConfigOutput
{
    public string formData { get; set; }
    public string columnData { get; set; }
    public string appColumnData { get; set; }
    public string webType { get; set; }
    public string flowTemplateJson { get; set; }
    public string flowEnCode { get; set; }
    public string flowId { get; set; }
    public string fullName { get; set; }
    public int enableFlow { get; set; }
}