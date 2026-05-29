using JNPF.Systems.Entitys.Dto.DataInterFace;

namespace JNPF.Systems.Entitys.Dto.System.InterfaceOauth
{
    public class InterfaceOauthOutput
    {
        /// <summary>
        /// id.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 应用id.
        /// </summary>
        public string appId { get; set; }

        /// <summary>
        /// 应用名称.
        /// </summary>
        public string appName { get; set; }

        /// <summary>
        /// 应用秘钥.
        /// </summary>
        public string appSecret { get; set; }

        /// <summary>
        /// 黑名单.
        /// </summary>
        public string blackList { get; set; }

        /// <summary>
        /// 排序.
        /// </summary>
        public long? sortCode { get; set; }

        /// <summary>
        /// 状态.
        /// </summary>
        public int? enabledMark { get; set; }

        /// <summary>
        /// 使用期限.
        /// </summary>
        public DateTime? usefulLife { get; set; }

        /// <summary>
        /// 验证签名.
        /// </summary>
        public int? verifySignature { get; set; }

        /// <summary>
        /// 白名单.
        /// </summary>
        public string whiteList { get; set; }

        /// <summary>
        /// 说明.
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// 接口列表.
        /// </summary>
        public List<DataInterfaceListOutput> list { get; set; }

        /// <summary>
        /// 用户列表.
        /// </summary>
        public dynamic userList { get; set; }
    }
}
