namespace JNPF.Systems.Entitys.Dto.Permission.UsersCurrent
{
    public class UsersCurrentSignImgOutput
    {
        /// <summary>
        /// 主键.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 签名.
        /// </summary>
        public string signImg { get; set; }

        /// <summary>
        /// 是否默认(0:否，1：是).
        /// </summary>
        public int? isDefault { get; set; }
    }
}
