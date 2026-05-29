using JNPF.Extras.CollectiveOAuth.Enums;
using Newtonsoft.Json;
using System.Text;
using System.Web;

namespace JNPF.Extras.CollectiveOAuth.Utils;

public static class GlobalAuthUtil
{
    /// <summary>
    /// 把字典集合拼接成&符号链接的字符串.
    /// </summary>
    /// <param name="dicParams">参数字典.</param>
    /// <returns></returns>
    public static string parseMapToString(Dictionary<string, object> dicParams)
    {
        StringBuilder builder = new StringBuilder();
        if (dicParams.Count > 0)
        {
            builder.Append(string.Empty);
            int i = 0;
            foreach (KeyValuePair<string, object> item in dicParams)
            {
                if (i > 0)
                    builder.Append("&");
                builder.AppendFormat("{0}={1}", item.Key, Convert.ToString(item.Value));
                i++;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// 是否为http协议.
    /// </summary>
    /// <param name="url">待验证的url.</param>
    /// <returns>true: http协议, false: 非http协议.</returns>
    public static bool isHttpProtocol(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        return url.StartsWith("http://");
    }

    /// <summary>
    /// 是否为https协议.
    /// </summary>
    /// <param name="url">待验证的url.</param>
    /// <returns>true: https协议, false: 非https协议.</returns>
    public static bool isHttpsProtocol(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        return url.StartsWith("https://");
    }

    /// <summary>
    /// 是否为本地主机（域名）.
    /// </summary>
    /// <param name="url">待验证的url.</param>
    /// <returns>true: 本地主机（域名）, false: 非本地主机（域名）.</returns>
    public static bool isLocalHost(string url)
    {
        return string.IsNullOrWhiteSpace(url) || url.Contains("127.0.0.1") || url.Contains("localhost");
    }

    /// <summary>
    /// 获取用户的实际性别，常规网站.
    /// </summary>
    /// <param name="originalGender">用户第三方标注的原始性别.</param>
    /// <returns>用户性别.</returns>
    public static AuthUserGender getRealGender(string originalGender)
    {
        if (null == originalGender || Convert.ToInt32(AuthUserGender.UNKNOWN).ToString().Equals(originalGender))
        {
            return AuthUserGender.UNKNOWN;
        }
        string[] males = { "m", "男", "1", "male" };
        if (males.ToList().Contains(originalGender.ToLower()))
        {
            return AuthUserGender.MALE;
        }
        return AuthUserGender.FEMALE;
    }

    /// <summary>
    /// 获取微信平台用户的实际性别，0表示未定义，1表示男性，2表示女性.
    /// </summary>
    /// <param name="originalGender">用户第三方标注的原始性别.</param>
    /// <returns>用户性别.</returns>
    public static AuthUserGender getWechatRealGender(string originalGender)
    {
        if (string.IsNullOrWhiteSpace(originalGender) || "0".Equals(originalGender))
        {
            return AuthUserGender.UNKNOWN;
        }
        return getRealGender(originalGender);
    }

    /// <summary>
    /// url编码.
    /// </summary>
    /// <param name="value">url.</param>
    /// <returns></returns>
    public static string urlEncode(string value)
    {
        if (value == null)
            return string.Empty;
        try
        {
            return System.Web.HttpUtility.UrlEncode(value);
        }
        catch (Exception e)
        {
            throw new Exception("Failed To Encode Uri", e);
        }
    }

    /// <summary>
    /// url解码.
    /// </summary>
    /// <param name="value">url.</param>
    /// <returns></returns>
    public static string urlDecode(string value)
    {
        if (value == null)
            return string.Empty;
        try
        {
            return HttpUtility.UrlDecode(value);  // utf-8 解码
        }
        catch (Exception e)
        {
            throw new Exception("Failed To Decode Uri", e);
        }
    }

    /// <summary>
    /// 字符串转换成枚举.
    /// </summary>
    /// <typeparam name="T">对象.</typeparam>
    /// <param name="type">类型.</param>
    /// <returns>对象.</returns>
    public static T enumFromString<T>(string type)
    {
        if (type.IsNullOrEmpty())
            throw new Exception($"没有找到授权类型: {type}");
        try
        {
            T result = (T)Enum.Parse(typeof(T), type); // utf-8 解码
            return result;
        }
        catch (Exception e)
        {
            throw new Exception($"授权类型解析失败: {type}", e);
        }
    }

    /// <summary>
    /// json字符串转换为字典集合.
    /// </summary>
    /// <param name="jsonStr"></param>
    /// <returns></returns>
    public static List<Dictionary<string, object>> parseListObject(this string jsonStr)
    {
        var retDic = new List<Dictionary<string, object>>();
        if (!string.IsNullOrWhiteSpace(jsonStr))
        {
            try
            {
                retDic = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonStr);
            }
            catch (Exception ex)
            {
            }
        }
        return retDic;
    }

    /// <summary>
    /// json字符串转换为字典集合.
    /// </summary>
    /// <param name="jsonStr"></param>
    /// <returns></returns>
    public static Dictionary<string, object> parseObject(this string jsonStr)
    {
        var retDic = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(jsonStr))
        {
            try
            {
                retDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonStr);
            }
            catch (Exception ex)
            {
            }
        }
        return retDic;
    }

    /// <summary>
    /// 将URL参数解析为Map（也可以解析Post中的键值对参数）.
    /// </summary>
    /// <param name="paramsStr">参数字符串（或者带参数的Path）.</param>
    /// <returns>参数Map.</returns>
    public static Dictionary<string, object> parseUrlObject(this string paramsStr)
    {
        Dictionary<string, object> res = new Dictionary<string, object>();
        try
        {
            if (paramsStr.IsNullOrWhiteSpace())
            {
                return res;
            }

            // 去掉Path部分
            int pathEndPos = paramsStr.IndexOf('?');
            if (pathEndPos > -1)
            {
                paramsStr = paramsStr.Substring(pathEndPos + 1);
            }

            return parseStringObject(paramsStr);
        }
        catch (Exception e)
        {
            return res;
        }
    }

    /// <summary>
    /// string字符串转map，str格式为 {@code xxx=xxx&xxx=xxx}.
    /// </summary>
    /// <param name="accessTokenStr">待转换的字符串.</param>
    /// <returns>字段对象.</returns>
    public static Dictionary<string, object> parseStringObject(this string accessTokenStr)
    {
        Dictionary<string, object> res = new Dictionary<string, object>();
        if (accessTokenStr.Contains("&"))
        {
            string[] fields = accessTokenStr.Split("&");
            foreach (var field in fields)
            {
                if (field.Contains("="))
                {
                    string[] keyValue = field.Split("=");
                    res.Add(urlDecode(keyValue[0]), keyValue.Length == 2 ? urlDecode(keyValue[1]) : null);
                }
            }
        }

        return res;
    }

    /// <summary>
    /// 把字典集合拼接成&符号链接的字符串.
    /// </summary>
    /// <param name="dicParams"></param>
    /// <returns></returns>
    public static string spellParams(this Dictionary<string, object> dicParams)
    {
        StringBuilder builder = new StringBuilder();
        if (dicParams.Count > 0)
        {
            builder.Append(string.Empty);
            int i = 0;
            foreach (KeyValuePair<string, object> item in dicParams)
            {
                if (i > 0)
                    builder.Append("&");
                builder.AppendFormat("{0}={1}", item.Key, Convert.ToString(item.Value));
                i++;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// object的字典集合.
    /// </summary>
    /// <param name="dic"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string getString(this Dictionary<string, object> dic, string key)
    {
        if (dic == null)
            return string.Empty;
        if (dic.ContainsKey(key))
        {
            return Convert.ToString(dic[key]);
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取参数Int32类型.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public static int getInt32(this Dictionary<string, object> request, string paramName)
    {
        var paramValue = request.getString(paramName);
        if (!string.IsNullOrWhiteSpace(paramValue))
        {
            try
            {
                return Convert.ToInt32(paramValue);
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        return -1;
    }

    /// <summary>
    /// 获取参数Int64类型.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public static long getLong(this Dictionary<string, object> request, string paramName)
    {
        var paramValue = request.getString(paramName);
        if (!string.IsNullOrWhiteSpace(paramValue))
        {
            try
            {
                return Convert.ToInt64(paramValue);
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        return -1;
    }

    /// <summary>
    /// 获取参数Bool类型.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public static bool getBool(this Dictionary<string, object> request, string paramName)
    {
        var paramValue = request.getString(paramName);
        if (!string.IsNullOrWhiteSpace(paramValue))
        {
            try
            {
                return Convert.ToBoolean(paramValue);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取参数字符串并且转换为字典集合.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public static Dictionary<string, object> getJSONObject(this Dictionary<string, object> request, string paramName)
    {
        var paramValue = request.getString(paramName);
        if (!string.IsNullOrWhiteSpace(paramValue))
        {
            try
            {
                return paramValue.parseObject();
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object>();
            }
        }

        return new Dictionary<string, object>();
    }

    /// <summary>
    /// 获取参数字符串并且转换为字典集合.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public static List<Dictionary<string, object>> getJSONArray(this Dictionary<string, object> request, string paramName)
    {
        var paramValue = request.getString(paramName);
        if (!string.IsNullOrWhiteSpace(paramValue))
        {
            try
            {
                return paramValue.parseListObject();
            }
            catch (Exception ex)
            {
                return new List<Dictionary<string, object>>();
            }
        }

        return new List<Dictionary<string, object>>();
    }

    /// <summary>
    /// 获取参数字符串类型.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    /*public static string getString(this Dictionary<string, object> request, string paramName)
    {
        var paramValue = request.getString(paramName);
        if (!string.IsNullOrWhiteSpace(paramValue))
        {
            try
            {
                return Convert.ToString(paramValue);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        return null;
    }*/

    /// <summary>
    /// 获取参数日期类型.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public static DateTime? GetParamDateTime(this Dictionary<string, object> request, string paramName)
    {
        var paramValue = request.getString(paramName);
        if (!string.IsNullOrWhiteSpace(paramValue))
        {
            try
            {
                return Convert.ToDateTime(paramValue);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取参数double类型.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public static double? GetParamDouble(this Dictionary<string, object> request, string paramName)
    {
        var paramValue = request.getString(paramName);
        if (!string.IsNullOrWhiteSpace(paramValue))
        {
            try
            {
                return Convert.ToDouble(paramValue);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取参数Decimal类型.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    public static decimal? GetParamDecimal(this Dictionary<string, object> request, string paramName)
    {
        var paramValue = request.getString(paramName);
        if (!string.IsNullOrWhiteSpace(paramValue))
        {
            try
            {
                return Convert.ToDecimal(paramValue);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// 字典排序,asc排序.
    /// </summary>
    /// <param name="dic">需排序的字典对象</param>
    /// <param name="isAsc">true=正序，反之倒序</param>
    public static Dictionary<string, object> Sort(this Dictionary<string, object> dic, bool isAsc = true)
    {
        Dictionary<string, object> rdic = new Dictionary<string, object>();
        if (dic.Count > 0)
        {
            List<KeyValuePair<string, object>> lst = new List<KeyValuePair<string, object>>(dic);
            lst.Sort(delegate (KeyValuePair<string, object> s1, KeyValuePair<string, object> s2) {
                if (isAsc)
                {
                    return string.CompareOrdinal(s1.Key, s2.Key);
                }
                else
                {
                    return string.CompareOrdinal(s2.Key, s1.Key);
                }
            });

            foreach (KeyValuePair<string, object> kvp in lst)
                rdic.Add(kvp.Key, kvp.Value);
        }

        return rdic;
    }
}