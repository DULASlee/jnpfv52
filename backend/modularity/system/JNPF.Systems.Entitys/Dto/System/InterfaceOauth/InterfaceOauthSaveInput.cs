namespace JNPF.Systems.Entitys.Dto.System.InterfaceOauth
{
    public class InterfaceOauthSaveInput
    {
        /// <summary>
        /// id.
        /// </summary>
        public string interfaceIdentId { get; set; }

        /// <summary>
        /// 接口id.
        /// </summary>
        public string dataInterfaceIds { get; set; }

        /// <summary>
        /// 用户id.
        /// </summary>
        public List<string> userIds { get; set; }
    }
}
