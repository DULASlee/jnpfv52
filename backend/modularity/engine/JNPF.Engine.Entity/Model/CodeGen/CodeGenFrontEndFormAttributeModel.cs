using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成前端表单属性模型.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndFormAttributeModel
{
    /// <summary>
    /// 表单尺寸.
    /// </summary>
    public string Size { get; set; }

    /// <summary>
    /// 标签对齐.
    /// </summary>
    public string LabelPosition { get; set; }

    /// <summary>
    /// 布局方式-文本宽度.
    /// </summary>
    public int LabelWidth { get; set; }

    /// <summary>
    /// 弹窗类型.
    /// </summary>
    public string PopupType { get; set; }

    /// <summary>
    /// 间距.
    /// </summary>
    public int Gutter { get; set; }

    /// <summary>
    /// 取消按钮文本.
    /// </summary>
    public string CancelButtonText { get; set; }

    /// <summary>
    /// 确认按钮文本.
    /// </summary>
    public string ConfirmButtonText { get; set; }

    /// <summary>
    /// 普通弹窗表单宽度.
    /// </summary>
    public string GeneralWidth { get; set; }

    /// <summary>
    /// 全屏弹窗表单宽度.
    /// </summary>
    public string FullScreenWidth { get; set; }

    /// <summary>
    /// drawer宽度.
    /// </summary>
    public string DrawerWidth { get; set; }

    /// <summary>
    /// 主键策略(1 雪花ID 2 自增长ID).
    /// </summary>
    public int PrimaryKeyPolicy { get; set; } = 1;

    /// <summary>
    /// 是否开启打印.
    /// </summary>
    public bool HasPrintBtn { get; set; }

    /// <summary>
    /// 打印按钮文本.
    /// </summary>
    public string PrintButtonText { get; set; }

    /// <summary>
    /// 打印模板ID.
    /// </summary>
    public string PrintId { get; set; }
}