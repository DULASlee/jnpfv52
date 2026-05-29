using JNPF.Common.Const;
using JNPF.Common.Models;
using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Common.CodeGenUpload;

/// <summary>
/// 代码生成导入.
/// </summary>
[SuppressSniffer]
[AttributeUsage(AttributeTargets.Property)]
public class CodeGenUploadAttribute : Attribute
{
    /// <summary>
    /// 构造函数.
    /// "createUser"
    /// "modifyUser"
    /// "createTime"
    /// "modifyTime"
    /// "currOrganize"
    /// "currPosition"
    /// "currDept"
    /// "billRule"
    /// "input"
    /// "textarea"
    /// "colorPicker"
    /// "rate"
    /// "slider"
    /// "editor".
    /// </summary>
    public CodeGenUploadAttribute(string Model, string Config)
    {
        __Model__ = Model;
        __config__ = Config.ToObject<CodeGenConfigModel>();
    }

    /// <summary>
    /// 构造函数 单行唯一验证.
    /// "input".
    /// </summary>
    /// <param name="Model"></param>
    /// <param name="vModel"></param>
    /// <param name="Config"></param>
    /// <param name="isInputUnique"></param>
    public CodeGenUploadAttribute(string Model, string vModel, string Config, bool isInputUnique)
    {
        __Model__ = Model;
        __vModel__ = vModel;
        __config__ = Config.ToObject<CodeGenConfigModel>();
    }

    /// <summary>
    /// 构造函数
    /// "time"
    /// "date".
    /// </summary>
    public CodeGenUploadAttribute(string Model, string SecondParameter, string Config)
    {
        __Model__ = Model;
        __config__ = Config.ToObject<CodeGenConfigModel>();
        switch (__config__.jnpfKey)
        {
            case JnpfKeyConst.CHECKBOX:
            case JnpfKeyConst.RADIO:
                options = SecondParameter?.ToObject<List<Dictionary<string, object>>>();
                break;
            default:
                format = SecondParameter;
                break;
        }
    }

    /// <summary>
    /// 构造函数
    /// "inputNumber".
    /// </summary>
    public CodeGenUploadAttribute(string Model, string Config, int Min = default, int Max = default)
    {
        __Model__ = Model;
        min = Min;
        max = Max;
        __config__ = Config.ToObject<CodeGenConfigModel>();
    }

    /// <summary>
    /// 构造函数
    /// "radio"
    /// "checkbox"
    /// "switch".
    /// </summary>
    public CodeGenUploadAttribute(string Model, string ActiveTxt, string InactiveTxt, string Config)
    {
        __Model__ = Model;
        __config__ = Config.ToObject<CodeGenConfigModel>();
        switch (__config__.jnpfKey)
        {
            case JnpfKeyConst.CHECKBOX:
            case JnpfKeyConst.RADIO:
                props = ActiveTxt?.ToObject<CodeGenPropsBeanModel>();
                options = InactiveTxt?.ToObject<List<Dictionary<string, object>>>();
                break;
            case JnpfKeyConst.SWITCH:
                activeTxt = ActiveTxt;
                inactiveTxt = InactiveTxt;
                break;
        }
    }

    /// <summary>
    /// 构造函数
    /// "popupSelect".
    /// </summary>
    public CodeGenUploadAttribute(string Model, string dataConversionModel, string SecondParameter, string ThreeParameters, string FourParameters, string ShowField, string Config)
    {
        __Model__ = Model;
        __vModel__ = dataConversionModel;
        interfaceId = SecondParameter;
        propsValue = ThreeParameters;
        relationField = FourParameters;
        showField = ShowField;
        __config__ = Config.ToObject<CodeGenConfigModel>();
    }

    /// <summary>
    /// 构造函数
    /// "comSelect":
    /// "roleSelect":
    /// "groupSelect".
    /// </summary>
    public CodeGenUploadAttribute(string Model, bool Multiple, string Config)
    {
        __Model__ = Model;
        multiple = Multiple;
        __config__ = Config.ToObject<CodeGenConfigModel>();
    }

    /// <summary>
    /// 构造函数
    /// "address".
    /// </summary>
    public CodeGenUploadAttribute(string Model, bool Multiple, int Level, string Config)
    {
        __Model__ = Model;
        multiple = Multiple;
        level = Level;
        __config__ = Config.ToObject<CodeGenConfigModel>();
    }

    /// <summary>
    /// 构造函数
    /// "treeSelect"
    /// "depSelect":
    /// "comSelect":
    /// "roleSelect":
    /// "groupSelect".
    /// "select".
    /// </summary>
    public CodeGenUploadAttribute(string Model, bool Multiple, string ThreeParameters, string FourParameters, string Config)
    {
        __Model__ = Model;
        multiple = Multiple;
        __config__ = Config.ToObject<CodeGenConfigModel>();
        switch (__config__.jnpfKey)
        {
            case JnpfKeyConst.DEPSELECT:
            case JnpfKeyConst.COMSELECT:
            case JnpfKeyConst.ROLESELECT:
            case JnpfKeyConst.GROUPSELECT:
                selectType = ThreeParameters;
                ableIds = FourParameters?.ToObject<List<object>>();
                break;
            default:
                props = ThreeParameters?.ToObject<CodeGenPropsBeanModel>();
                options = FourParameters?.ToObject<List<Dictionary<string, object>>>();
                break;
        }
    }

    /// <summary>
    /// 构造函数
    /// "usersSelect":.
    /// </summary>
    public CodeGenUploadAttribute(string Model, string dataConversionModel, bool Multiple, string ThreeParameters, string FourParameters, string Config)
    {
        __Model__ = Model;
        __vModel__ = dataConversionModel;
        multiple = Multiple;
        __config__ = Config.ToObject<CodeGenConfigModel>();
        selectType = ThreeParameters;
        ableIds = FourParameters?.ToObject<List<object>>();
    }

    /// <summary>
    /// 构造函数
    /// "cascader"
    /// "posSelect":
    /// "relationForm"
    /// "popupTableSelect".
    /// </summary>
    public CodeGenUploadAttribute(string Model, bool Multiple, string InterfaceId, string PropsValue, string RelationField, string Config)
    {
        __Model__ = Model;
        multiple = Multiple;
        __config__ = Config.ToObject<CodeGenConfigModel>();
        switch (__config__.jnpfKey)
        {
            case JnpfKeyConst.CASCADER:
                separator = InterfaceId;
                props = PropsValue?.ToObject<CodeGenPropsBeanModel>();
                options = RelationField?.ToObject<List<Dictionary<string, object>>>();
                break;
            case JnpfKeyConst.POPUPTABLESELECT:
                interfaceId = InterfaceId;
                propsValue = PropsValue;
                relationField = RelationField;
                break;
            case JnpfKeyConst.POSSELECT:
                selectType = InterfaceId;
                ableIds = PropsValue?.ToObject<List<object>>();
                break;
        }
    }

    /// <summary>
    /// 构造函数
    /// "relationForm".
    /// </summary>
    public CodeGenUploadAttribute(string Model, string dataConversionModel, bool Multiple, string InterfaceId, string PropsValue, string RelationField, string Config)
    {
        __Model__ = Model;
        __vModel__ = dataConversionModel;
        multiple = Multiple;
        __config__ = Config.ToObject<CodeGenConfigModel>();
        modelId = InterfaceId;
        relationField = PropsValue;
        showField = RelationField;
    }

    /// <summary>
    /// 构造函数
    /// "userSelect":.
    /// </summary>
    public CodeGenUploadAttribute(string Model, bool Multiple, string SelectType, string AblIds, bool isUserSelect, string Config)
    {
        __Model__ = Model;
        multiple = Multiple;
        selectType = SelectType;
        ableIds = AblIds?.ToObject<List<object>>();
        __config__ = Config.ToObject<CodeGenConfigModel>();
    }

    /// <summary>
    /// 设置默认值为空字符串.
    /// </summary>
    public string __Model__ { get; set; }

    /// <summary>
    /// 数据转换.
    /// </summary>
    public string __vModel__ { get; set; }

    /// <summary>
    /// 最小值.
    /// </summary>
    public int min { get; set; }

    /// <summary>
    /// 最大值.
    /// </summary>
    public int max { get; set; }

    /// <summary>
    /// 开关控件 属性 - 开启展示值.
    /// </summary>
    public string? activeTxt { get; set; }

    /// <summary>
    /// 开关控件 属性 - 关闭展示值.
    /// </summary>
    public string? inactiveTxt { get; set; }

    /// <summary>
    /// 显示绑定值的格式.
    /// </summary>
    public string? format { get; set; }

    /// <summary>
    /// 是否多选.
    /// </summary>
    public bool multiple { get; set; }

    /// <summary>
    /// 选项分隔符.
    /// </summary>
    public string? separator { get; set; }

    /// <summary>
    /// 配置选项.
    /// </summary>
    public CodeGenPropsBeanModel? props { get; set; }

    /// <summary>
    /// 配置项.
    /// </summary>
    public List<Dictionary<string, object>>? options { get; set; }

    /// <summary>
    /// 弹窗选择主键.
    /// </summary>
    public string? propsValue { get; set; }

    /// <summary>
    /// 关联表单字段.
    /// </summary>
    public string? relationField { get; set; }

    /// <summary>
    /// 关联表单id.
    /// </summary>
    public string? modelId { get; set; }

    /// <summary>
    /// 数据接口ID.
    /// </summary>
    public string? interfaceId { get; set; }

    /// <summary>
    /// 层级.
    /// </summary>
    public int level { get; set; }

    /// <summary>
    /// 配置.
    /// </summary>
    public CodeGenConfigModel? __config__ { get; set; }

    /// <summary>
    /// 可选范围.
    /// </summary>
    public string selectType { get; set; }

    /// <summary>
    /// 新用户选择控件.
    /// </summary>
    public List<object> ableIds { get; set; }

    /// <summary>
    /// 展示字段.
    /// </summary>
    public string showField { get; set; }
}