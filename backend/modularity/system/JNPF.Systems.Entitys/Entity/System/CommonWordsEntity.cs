using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Entity.System;

/// <summary>
/// 常用语
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_COMMON_WORDS")]
public class CommonWordsEntity : CLDSEntityBase
{
    /// <summary>
    /// 常用语.
    /// </summary>
    [SugarColumn(ColumnName = "F_COMMON_WORDS_TEXT")]
    public string CommonWordsText { get; set; }

    /// <summary>
    /// 常用语类型(0:系统,1:个人).
    /// </summary>
    [SugarColumn(ColumnName = "F_COMMON_WORDS_TYPE")]
    public int CommonWordsType { get; set; }
}
