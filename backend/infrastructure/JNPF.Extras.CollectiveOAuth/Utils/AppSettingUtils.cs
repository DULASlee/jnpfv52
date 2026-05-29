namespace JNPF.Extras.CollectiveOAuth.Utils;

/// <summary>
/// 应用程序设置帮助类.
/// </summary>
public class AppSettingUtils
{
    /// <summary>
    /// 根据Key取Value值.
    /// </summary>
    /// <param name="key"></param>
    public static string GetStrValue(string key)
    {
        var value = string.Empty;
        value = JNPF.Extras.CollectiveOAuth.Utils.ConfigurationManager.AppSettings[key];
        if (!string.IsNullOrWhiteSpace(value))
            return value.ToString().Trim();
        return value;
    }
}