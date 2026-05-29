using JNPF.DependencyInjection;

namespace JNPF.Common.Const;

/// <summary>
/// jnpfKey类型.
/// </summary>
[SuppressSniffer]
public class JnpfKeyConst
{
    #region 基础控件

    /// <summary>
    /// 单行输入.
    /// </summary>
    public const string COMINPUT = "input";

    /// <summary>
    /// 多行输入.
    /// </summary>
    public const string TEXTAREA = "textarea";

    /// <summary>
    /// 数字输入.
    /// </summary>
    public const string NUMINPUT = "inputNumber";

    /// <summary>
    /// 金额输入.
    /// </summary>
    public const string JNPFAMOUNT = "JNPFAmount";

    /// <summary>
    /// 开关.
    /// </summary>
    public const string SWITCH = "switch";

    /// <summary>
    /// 单选.
    /// </summary>
    public const string RADIO = "radio";

    /// <summary>
    /// 多选框.
    /// </summary>
    public const string CHECKBOX = "checkbox";

    /// <summary>
    /// 下拉框.
    /// </summary>
    public const string SELECT = "select";

    /// <summary>
    /// 级联选择.
    /// </summary>
    public const string CASCADER = "cascader";

    /// <summary>
    /// 日期选择.
    /// </summary>
    public const string DATE = "datePicker";

    /// <summary>
    /// 时间选择.
    /// </summary>
    public const string TIME = "timePicker";

    /// <summary>
    /// 文件上传.
    /// </summary>
    public const string UPLOADFZ = "uploadFile";

    /// <summary>
    /// 图片上传.
    /// </summary>
    public const string UPLOADIMG = "uploadImg";

    /// <summary>
    /// 颜色选择.
    /// </summary>
    public const string COLORPICKER = "colorPicker";

    /// <summary>
    /// 评分.
    /// </summary>
    public const string RATE = "rate";

    /// <summary>
    /// 滑块.
    /// </summary>
    public const string SLIDER = "slider";

    /// <summary>
    /// 富文本.
    /// </summary>
    public const string EDITOR = "editor";

    /// <summary>
    /// 链接.
    /// </summary>
    public const string LINK = "link";

    /// <summary>
    /// 按钮.
    /// </summary>
    public const string BUTTON = "button";

    /// <summary>
    /// 文本.
    /// </summary>
    public const string JNPFTEXT = "text";

    /// <summary>
    /// 提示.
    /// </summary>
    public const string ALERT = "alert";

    /// <summary>
    /// 条形码.
    /// </summary>
    public const string BARCODE = "barcode";

    /// <summary>
    /// 二维码.
    /// </summary>
    public const string QRCODE = "qrcode";

    /// <summary>
    /// iframe.
    /// </summary>
    public const string IFRAME = "iframe";

    #endregion

    #region 高级控件

    /// <summary>
    /// 组织选择.
    /// </summary>
    public const string COMSELECT = "organizeSelect";

    /// <summary>
    /// 部门选择.
    /// </summary>
    public const string DEPSELECT = "depSelect";

    /// <summary>
    /// 岗位选择.
    /// </summary>
    public const string POSSELECT = "posSelect";

    /// <summary>
    /// 用户选择.
    /// </summary>
    public const string USERSELECT = "userSelect";

    /// <summary>
    /// 角色选择.
    /// </summary>
    public const string ROLESELECT = "roleSelect";

    /// <summary>
    /// 分组选择.
    /// </summary>
    public const string GROUPSELECT = "groupSelect";

    /// <summary>
    /// 用户组件.
    /// </summary>
    public const string USERSSELECT = "usersSelect";

    /// <summary>
    /// 设计子表.
    /// </summary>
    public const string TABLE = "table";

    /// <summary>
    /// 下拉树形.
    /// </summary>
    public const string TREESELECT = "treeSelect";

    /// <summary>
    /// 下拉表格.
    /// </summary>
    public const string POPUPTABLESELECT = "popupTableSelect";

    /// <summary>
    /// 下拉补全.
    /// </summary>
    public const string AUTOCOMPLETE = "autoComplete";

    /// <summary>
    /// 行政区划.
    /// </summary>
    public const string ADDRESS = "areaSelect";

    /// <summary>
    /// 单据组件.
    /// </summary>
    public const string BILLRULE = "billRule";

    /// <summary>
    /// 关联表单.
    /// </summary>
    public const string RELATIONFORM = "relationForm";

    /// <summary>
    /// 弹窗选择.
    /// </summary>
    public const string POPUPSELECT = "popupSelect";

    /// <summary>
    /// 关联表单属性.
    /// </summary>
    public const string RELATIONFORMATTR = "relationFormAttr";

    /// <summary>
    /// 弹窗选择属性.
    /// </summary>
    public const string POPUPATTR = "popupAttr";

    /// <summary>
    /// 手写签名.
    /// </summary>
    public const string SIGN = "sign";

    /// <summary>
    /// 签章.
    /// </summary>
    public const string SIGNATURE = "signature";

    /// <summary>
    /// 定位.
    /// </summary>
    public const string LOCATION = "location";

    /// <summary>
    /// 计算公式.
    /// </summary>
    public const string CALCULATE = "calculate";

    #endregion

    #region 系统控件

    /// <summary>
    /// 创建人员.
    /// </summary>
    public const string CREATEUSER = "createUser";

    /// <summary>
    /// 创建时间.
    /// </summary>
    public const string CREATETIME = "createTime";

    /// <summary>
    /// 修改人员.
    /// </summary>
    public const string MODIFYUSER = "modifyUser";

    /// <summary>
    /// 修改时间.
    /// </summary>
    public const string MODIFYTIME = "modifyTime";

    /// <summary>
    /// 所属部门.
    /// </summary>
    public const string CURRDEPT = "currDept";

    /// <summary>
    /// 所属组织.
    /// </summary>
    public const string CURRORGANIZE = "currOrganize";

    /// <summary>
    /// 所属岗位.
    /// </summary>
    public const string CURRPOSITION = "currPosition";

    #endregion

    #region 布局控件

    /// <summary>
    /// 分组标题.
    /// </summary>
    public const string GROUPTITLE = "groupTitle";

    /// <summary>
    /// 分割线.
    /// </summary>
    public const string DIVIDER = "divider";

    /// <summary>
    /// 折叠面板.
    /// </summary>
    public const string COLLAPSE = "collapse";

    /// <summary>
    /// 标签面板.
    /// </summary>
    public const string TAB = "tab";

    /// <summary>
    /// 栅格容器.
    /// </summary>
    public const string ROW = "row";

    /// <summary>
    /// 卡片容器.
    /// </summary>
    public const string CARD = "card";

    /// <summary>
    /// 表格容器.
    /// </summary>
    public const string TABLEGRID = "tableGrid";

    /// <summary>
    /// 表格容器Tr.
    /// </summary>
    public const string TABLEGRIDTR = "tableGridTr";

    /// <summary>
    /// 表格容器Td.
    /// </summary>
    public const string TABLEGRIDTD = "tableGridTd";

    /// <summary>
    /// 标签面板Item.
    /// </summary>
    public const string TABITEM = "tabItem";

    #endregion

    /// <summary>
    /// 数据字典.
    /// </summary>
    public const string DICTIONARY = "dicSelect";

    /// <summary>
    /// 时间范围.
    /// </summary>
    public const string TIMERANGE = "timeRange";

    /// <summary>
    /// 日期范围.
    /// </summary>
    public const string DATERANGE = "dateRange";

    /// <summary>
    /// 关键词.
    /// </summary>
    public const string JNPFKEYWORD = "jnpfKeyword";
}