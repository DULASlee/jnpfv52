using System.ComponentModel;

namespace JNPF.Extras.CollectiveOAuth.Enums;

/// <summary>
/// 授权响应状态.
/// </summary>
public enum AuthResponseStatus
{
    /// <summary>
    /// 2000：正常；
    /// other：调用异常，具体异常内容见<c>msg</c>.
    /// </summary>
    [Description("Success")]
    SUCCESS = 2000,

    [Description("Failure")]
    FAILURE = 5000,

    [Description("Not Implemented")]
    NOT_IMPLEMENTED = 5001,

    [Description("Parameter incomplete")]
    PARAMETER_INCOMPLETE = 5002,

    [Description("Unsupported operation")]
    UNSUPPORTED = 5003,

    [Description("AuthDefaultSource cannot be null")]
    NO_AUTH_SOURCE = 5004,

    [Description("Unidentified platform")]
    UNIDENTIFIED_PLATFORM = 5005,

    [Description("Illegal redirect uri")]
    ILLEGAL_REDIRECT_URI = 5006,

    [Description("Illegal request")]
    ILLEGAL_REQUEST = 5007,

    [Description("Illegal code")]
    ILLEGAL_CODE = 5008,

    [Description("Illegal state")]
    ILLEGAL_STATUS = 5009,

    [Description("The refresh token is required; it must not be null")]
    REQUIRED_REFRESH_TOKEN = 5010,
}