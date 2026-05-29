namespace JNPF.Systems.Entitys.Model.Permission.User;

public class UserInfo
{
    private static readonly long serialVersionUID = 6402443942083382236L;

    public static readonly string CLASS_TYPE = "UserInfo";

    public static readonly string DEFAULT_PASSWORD_SUFFIX = "MaxKey@888";

    string sessionId { get; set; }

    public string id { get; set; }
    public string username { get; set; }
    public string password { get; set; }
    public string decipherable { get; set; }
    public string sharedSecret { get; set; }
    public string sharedCounter { get; set; }
    /**
     * "Employee", "Supplier","Dealer","Contractor",Partner,Customer "Intern",
     * "Temp", "External", and "Unknown" .
     */
    public string userType { get; set; }

    public string userState { get; set; }

    public string windowsAccount { get; set; }

    // for user name
    public string displayName { get; set; }
    public string nickName { get; set; }
    public string nameZhSpell { get; set; }
    public string nameZhShortSpell { get; set; }
    public string givenName { get; set; }
    public string middleName { get; set; }
    public string familyName { get; set; }
    public string honorificPrefix { get; set; }
    public string honorificSuffix { get; set; }
    public string formattedName { get; set; }

    public int married { get; set; }
    public string gender { get; set; }
    public string birthDate { get; set; }
    public byte[] picture { get; set; }
    public string pictureBase64 { get; set; }
    public string pictureId { get; set; }
    public int idType { get; set; }
    public string idCardNo { get; set; }
    public string webSite { get; set; }
    public string startWorkDate { get; set; }

    // for security
    public int authnType { get; set; }
    public string email { get; set; }

    public int emailVerified { get; set; }
    public string mobile { get; set; }

    public int mobileVerified { get; set; }

    public string passwordQuestion { get; set; }

    public string passwordAnswer { get; set; }
    // for apps login public
    public int appLoginAuthnType { get; set; }
    public string appLoginPassword { get; set; }
    public string protectedApps { get; set; }
    public Dictionary<string, string> protectedAppsMap { get; set; }

    public string passwordLastSetTime { get; set; }
    public int badPasswordCount { get; set; }
    public string badPasswordTime { get; set; }
    public string unLockTime { get; set; }
    public int isLocked { get; set; }
    public string lastLoginTime { get; set; }
    public string lastLoginIp { get; set; }
    public string lastLogoffTime { get; set; }
    public int passwordSetType { get; set; }
    public int loginCount { get; set; }
    public string regionHistory { get; set; }
    public string passwordHistory { get; set; }

    public string locale { get; set; }
    public string timeZone { get; set; }
    public string preferredLanguage { get; set; }

    // for work
    public string workCountry { get; set; }
    public string workRegion { get; set; }// province
    public string workLocality { get; set; }// city
    public string workStreetAddress { get; set; }
    public string workAddressFormatted { get; set; }
    public string workEmail { get; set; }
    public string workPhoneNumber { get; set; }
    public string workPostalCode { get; set; }
    public string workFax { get; set; }

    public string workOfficeName { get; set; }
    // for home
    public string homeCountry { get; set; }
    public string homeRegion { get; set; }// province
    public string homeLocality { get; set; }// city
    public string homeStreetAddress { get; set; }
    public string homeAddressFormatted { get; set; }
    public string homeEmail { get; set; }
    public string homePhoneNumber { get; set; }
    public string homePostalCode { get; set; }
    public string homeFax { get; set; }
    // for company
    public string employeeNumber { get; set; }
    public string costCenter { get; set; }
    public string organization { get; set; }
    public string division { get; set; }
    public string departmentId { get; set; }
    public string department { get; set; }
    public string jobTitle { get; set; }
    public string jobLevel { get; set; }
    public string managerId { get; set; }
    public string manager { get; set; }
    public string assistantId { get; set; }
    public string assistant { get; set; }
    public string entryDate { get; set; }
    public string quitDate { get; set; }

    // for social contact
    public string defineIm { get; set; }
    public int weixinFollow { get; set; }

    public string theme { get; set; }
    /*
     * for extended Attribute from userType extraAttribute for database
     * extraAttributeName & extraAttributeValue for page submit
     */
    public string extraAttribute { get; set; }
    public string extraAttributeName { get; set; }
    public string extraAttributeValue { get; set; }
    public Dictionary<string, string> extraAttributeMap { get; set; }

    public int online { get; set; }

    public string ldapDn { get; set; }

    public int gridList { get; set; }

    public string createdBy { get; set; }
    public string createdDate { get; set; }
    public string modifiedBy { get; set; }
    public string modifiedDate { get; set; }
    public int status { get; set; }
    string description { get; set; }

    /// <summary>
    /// 租户Id.
    /// </summary>
    public string instId { get; set; }

    public string instName { get; set; }

    string syncId { get; set; }

    string syncName { get; set; }

    string originId { get; set; }

    string originId2 { get; set; }

    string gradingUserId { get; set; }

    public override string ToString()
    {
        return "UserInfo{" +
                "id='" + id + '\'' +
                ", username='" + username + '\'' +
                '}';
    }
}

public class MqMessage
{
    public string id { get; set; }
    public string topic { get; set; }
    public string actionType { get; set; }
    public string sendTime { get; set; }
    public object content { get; set; }
    public UserInfo userInfo { get; set; }

    public MqMessage()
    {
    }

    public MqMessage(string id, string topic, string actionType, string sendTime, object content, UserInfo userInfo)
    {
        this.id = id;
        this.topic = topic;
        this.actionType = actionType;
        this.sendTime = sendTime;
        this.content = content;
        this.userInfo = userInfo;
    }
}

