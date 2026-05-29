using System.ComponentModel;

namespace JNPF.Engine.Enum.VisualDevModelData
{
    /// <summary>
    /// 在线开发控件 数据缓存枚举.
    /// </summary>
    public enum vModelType
    {
        /// <summary>
        /// 数据字典DataType. //test
        /// </summary>
        [Description("dictionary")]
        DICTIONARY,

        /// <summary>
        /// 静态数据DataType.
        /// </summary>
        [Description("static")]
        STATIC,

        /// <summary>
        /// 查询字段数据.
        /// </summary>
        [Description("keyJsonMap")]
        KEYJSONMAP,

        /// <summary>
        /// 级联选择静态模板值.
        /// </summary>
        [Description("value")]
        VALUE,

        /// <summary>
        /// 远程数据DataType.
        /// </summary>
        [Description("dynamic")]
        DYNAMIC,

        /// <summary>
        /// 远程数据.
        /// </summary>
        [Description("timeControl")]
        TIMECONTROL,

        /// <summary>
        /// 可视化数据列表结果key.
        /// </summary>
        [Description("list")]
        LIST
    }
}
