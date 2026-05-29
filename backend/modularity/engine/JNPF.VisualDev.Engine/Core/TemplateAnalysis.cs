using JNPF.Common.Const;
using JNPF.Engine.Entity.Model;

namespace JNPF.VisualDev.Engine;

/// <summary>
/// 模板解析.
/// </summary>
public static class TemplateAnalysis
{
    /// <summary>
    /// 解析模板数据
    /// 移除模板内的布局类型控件.
    /// </summary>
    public static List<FieldsModel> AnalysisTemplateData(List<FieldsModel> fieldsModelList)
    {
        var template = new List<FieldsModel>();

        // 将模板内的无限children解析出来
        // 不包含子表children
        foreach (FieldsModel? item in fieldsModelList)
        {
            ConfigModel? config = item.__config__;
            switch (config.jnpfKey)
            {
                case JnpfKeyConst.TABLE: // 设计子表
                    item.__config__.defaultCurrent = item.__config__.children.Any(it => it.__config__.defaultCurrent);
                    template.Add(item);
                    break;
                case JnpfKeyConst.ROW: // 栅格布局
                case JnpfKeyConst.CARD: // 卡片容器
                case JnpfKeyConst.TABITEM: // 标签面板Item
                case JnpfKeyConst.TABLEGRIDTR: // 表格容器Tr
                case JnpfKeyConst.TABLEGRIDTD: // 表格容器Td
                    template.AddRange(AnalysisTemplateData(config.children));
                    break;
                case JnpfKeyConst.COLLAPSE: // 折叠面板
                case JnpfKeyConst.TAB: // 标签面板
                case JnpfKeyConst.TABLEGRID: // 表格容器
                    config.children.ForEach(item => template.AddRange(AnalysisTemplateData(item.__config__.children)));
                    break;
                case JnpfKeyConst.JNPFTEXT: // 文本
                case JnpfKeyConst.DIVIDER: // 分割线
                case JnpfKeyConst.GROUPTITLE: // 分组标题
                case JnpfKeyConst.BUTTON: // 按钮
                case JnpfKeyConst.ALERT: // 提示
                case JnpfKeyConst.LINK: // 链接
                case JnpfKeyConst.IFRAME: // iframe
                    break;
                default:
                    template.Add(item);
                    break;
            }
        }

        return template;
    }

    /// <summary>
    /// 处理日期格式.
    /// </summary>
    public static void DataFormatReplace(List<FieldsModel> fList)
    {
        foreach (FieldsModel item in fList)
        {
            if (item.__config__.jnpfKey.Equals(JnpfKeyConst.DATE)) item.format = item.format.Replace("YYYY-MM-DD", "yyyy-MM-dd").Replace("YYYY", "yyyy");
            else if (item.__config__.children != null && item.__config__.children.Any()) DataFormatReplace(item.__config__.children);
        }
    }
}