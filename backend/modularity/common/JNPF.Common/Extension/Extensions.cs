using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace JNPF.Common.Extension;

/// <summary>
/// 转换扩展类.
/// </summary>
public static partial class Extensions
{
    #region 转换为long

    /// <summary>
    /// 将object转换为long,若转换失败,则返回0.不抛出异常.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static long ParseToLong(this object obj)
    {
        try
        {
            return long.Parse(obj.ToString() ?? string.Empty);
        }
        catch
        {
            return 0L;
        }
    }

    /// <summary>
    /// 将object转换为long,若转换失败,则返回指定值.不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static long ParseToLong(this string str, long defaultValue)
    {
        try
        {
            return long.Parse(str);
        }
        catch
        {
            return defaultValue;
        }
    }

    #endregion

    #region 转换为int

    /// <summary>
    /// 将object转换为int，若转换失败，则返回0。不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static int ParseToInt(this object str)
    {
        try
        {
            if (str.IsNullOrEmpty()) return 0;

            //modify by harry 布尔类型转换出现错误

            string boolCheck  = str.ToString().ToLower();
            if (boolCheck == "true" || boolCheck == "false")
            {
                bool boolValue = bool.Parse(boolCheck);
                return boolValue ? 1 : 0;
            }

            if (boolCheck.IsNullOrEmpty()) return 0;

            return Convert.ToInt32(str);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 将object转换为int，若转换失败，则返回指定值。不抛出异常
    /// null返回默认值.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static int ParseToInt(this object str, int defaultValue)
    {
        if (str == null)
        {
            return defaultValue;
        }

        try
        {
            return Convert.ToInt32(str);
        }
        catch
        {
            return defaultValue;
        }
    }

    #endregion

    #region 转换为short

    /// <summary>
    /// 将object转换为short，若转换失败，则返回0。不抛出异常.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static short ParseToShort(this object obj)
    {
        try
        {
            return short.Parse(obj.ToString() ?? string.Empty);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 将object转换为short，若转换失败，则返回指定值。不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static short ParseToShort(this object str, short defaultValue)
    {
        try
        {
            return short.Parse(str.ToString() ?? string.Empty);
        }
        catch
        {
            return defaultValue;
        }
    }

    #endregion

    #region 转换为demical

    /// <summary>
    /// 将object转换为demical，若转换失败，则返回指定值。不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static decimal ParseToDecimal(this object str, decimal defaultValue)
    {
        try
        {
            return decimal.Parse(str.ToString() ?? string.Empty);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 将object转换为demical，若转换失败，则返回0。不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static decimal ParseToDecimal(this object str)
    {
        try
        {
            return decimal.Parse(str.ToString() ?? string.Empty);
        }
        catch
        {
            return 0;
        }
    }

    #endregion

    #region 转化为bool

    /// <summary>
    /// 将object转换为bool，若转换失败，则返回false。不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool ParseToBool(this object str)
    {
        try
        {
            if (str == null)
                return false;
            bool? value = GetBool(str);
            if (value != null)
                return value.Value;
            bool result;
            return bool.TryParse(str.ToString(), out result) && result;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 将object转换为bool，若转换失败，则返回指定值。不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool ParseToBool(this object str, bool result)
    {
        try
        {
            return bool.Parse(str.ToString() ?? string.Empty);
        }
        catch
        {
            return result;
        }
    }

    /// <summary>
    /// 获取布尔值.
    /// </summary>
    private static bool? GetBool(this object data)
    {
        switch (data.ToString().Trim().ToLower())
        {
            case "0":
                return false;
            case "1":
                return true;
            case "是":
                return true;
            case "否":
                return false;
            case "yes":
                return true;
            case "no":
                return false;
            default:
                return null;
        }
    }

    #endregion

    #region 转换为float

    /// <summary>
    /// 将object转换为float，若转换失败，则返回0。不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static float ParseToFloat(this object str)
    {
        try
        {
            return float.Parse(str.ToString() ?? string.Empty);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 将object转换为float，若转换失败，则返回指定值。不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static float ParseToFloat(this object str, float result)
    {
        try
        {
            return float.Parse(str.ToString() ?? string.Empty);
        }
        catch
        {
            return result;
        }
    }

    #endregion

    #region 转换为Guid

    /// <summary>
    /// 将string转换为Guid，若转换失败，则返回Guid.Empty。不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static Guid ParseToGuid(this string str)
    {
        try
        {
            return new Guid(str);
        }
        catch
        {
            return Guid.Empty;
        }
    }

    #endregion

    #region 转换为DateTime

    /// <summary>
    /// 将string转换为DateTime，若转换失败，则返回日期最小值。不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static DateTime ParseToDateTime(this string str)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return DateTime.MinValue;
            }

            if (str.Contains("-") || str.Contains("/"))
            {
                return DateTime.Parse(str);
            }

            int length = str.Length;
            return length switch
            {
                4 => DateTime.ParseExact(str, "yyyy", CultureInfo.CurrentCulture),
                6 => DateTime.ParseExact(str, "yyyyMM", CultureInfo.CurrentCulture),
                8 => DateTime.ParseExact(str, "yyyyMMdd", CultureInfo.CurrentCulture),
                10 => DateTime.ParseExact(str, "yyyyMMddHH", CultureInfo.CurrentCulture),
                12 => DateTime.ParseExact(str, "yyyyMMddHHmm", CultureInfo.CurrentCulture),

                // ReSharper disable once StringLiteralTypo
                14 => DateTime.ParseExact(str, "yyyyMMddHHmmss", CultureInfo.CurrentCulture),

                // ReSharper disable once StringLiteralTypo
                _ => DateTime.ParseExact(str, "yyyyMMddHHmmss", CultureInfo.CurrentCulture)
            };
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// 将时间戳转为DateTime.
    /// </summary>
    /// <param name="timeStamp">时间戳.</param>
    /// <returns></returns>
    public static DateTime TimeStampToDateTime(this long timeStamp)
    {
        try
        {
            DateTimeOffset dto = DateTimeOffset.FromUnixTimeMilliseconds(timeStamp);
            return dto.ToLocalTime().DateTime;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 将时间戳转为DateTime.
    /// </summary>
    /// <param name="timeStamp">时间戳.</param>
    /// <returns></returns>
    public static DateTime TimeStampToDateTime(this string timeStamp)
    {
        try
        {
            DateTimeOffset dto = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(timeStamp));
            return dto.ToLocalTime().DateTime;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 将 DateTime? 转换为 DateTime.
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static DateTime ParseToDateTime(this DateTime? date)
    {
        return Convert.ToDateTime(date);
    }

    /// <summary>
    /// 将 DateTime 根据指定格式转换.
    /// </summary>
    /// <param name="date">时间.</param>
    /// <param name="format">格式字符串.</param>
    /// <returns></returns>
    public static DateTime ParseToDateTime(this DateTime? date, string format)
    {
        return Convert.ToDateTime(string.Format("{0:" + format + "}", date));
    }

    /// <summary>
    /// 将 DateTime 根据指定格式转换.
    /// </summary>
    /// <param name="date">时间.</param>
    /// <param name="format">格式字符串.</param>
    /// <returns></returns>
    public static DateTime ParseToDateTime(this DateTime date, string format)
    {
        return Convert.ToDateTime(string.Format("{0:" + format + "}", date));
    }

    /// <summary>
    /// 将string转换为DateTime，若转换失败，则返回默认值.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static DateTime ParseToDateTime(this string str, DateTime? defaultValue)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return defaultValue.GetValueOrDefault();
            }

            if (str.Contains("-") || str.Contains("/"))
            {
                return DateTime.Parse(str);
            }

            int length = str.Length;
            return length switch
            {
                4 => DateTime.ParseExact(str, "yyyy", CultureInfo.CurrentCulture),
                6 => DateTime.ParseExact(str, "yyyyMM", CultureInfo.CurrentCulture),
                8 => DateTime.ParseExact(str, "yyyyMMdd", CultureInfo.CurrentCulture),
                10 => DateTime.ParseExact(str, "yyyyMMddHH", CultureInfo.CurrentCulture),
                12 => DateTime.ParseExact(str, "yyyyMMddHHmm", CultureInfo.CurrentCulture),

                // ReSharper disable once StringLiteralTypo
                14 => DateTime.ParseExact(str, "yyyyMMddHHmmss", CultureInfo.CurrentCulture),

                // ReSharper disable once StringLiteralTypo
                _ => DateTime.ParseExact(str, "yyyyMMddHHmmss", CultureInfo.CurrentCulture)
            };
        }
        catch
        {
            return defaultValue.GetValueOrDefault();
        }
    }

    #endregion

    #region 转换为string

    /// <summary>
    /// 将object转换为string，若转换失败，则返回""。不抛出异常.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ParseToString(this object obj)
    {
        try
        {
            return obj == null ? string.Empty : obj.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 将object转换为string.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ParseToStrings<T>(this object obj)
    {
        try
        {
            if (obj is IEnumerable<T> list)
            {
                return string.Join(",", list);
            }

            return obj.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    #endregion

    #region 转换为double

    /// <summary>
    /// 将object转换为double，若转换失败，则返回0。不抛出异常.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static double ParseToDouble(this object obj)
    {
        try
        {
            return double.Parse(obj.ToString() ?? string.Empty);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 将object转换为double，若转换失败，则返回指定值。不抛出异常.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static double ParseToDouble(this object str, double defaultValue)
    {
        try
        {
            return double.Parse(str.ToString() ?? string.Empty);
        }
        catch
        {
            return defaultValue;
        }
    }

    #endregion

    #region 强制转换类型

    /// <summary>
    /// 强制转换类型.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static IEnumerable<TResult> CastSuper<TResult>(this IEnumerable source)
    {
        return from object item in source select (TResult)Convert.ChangeType(item, typeof(TResult));
    }

    #endregion

    #region 转换为ToUnixTime

    public static long ParseToUnixTime(this DateTime nowTime)
    {
        DateTimeOffset dto = new DateTimeOffset(nowTime);
        return dto.ToUnixTimeMilliseconds();
    }

    #endregion

    #region 转换为帕斯卡命名法

    /// <summary>
    /// 将字符串转为帕斯卡命名法.
    /// </summary>
    /// <param name="original">源字符串.</param>
    /// <returns></returns>
    public static string ParseToPascalCase(this string original)
    {
        Regex invalidCharsRgx = new Regex("[^_a-zA-Z0-9]");
        Regex whiteSpace = new Regex(@"(?<=\s)");
        Regex startsWithLowerCaseChar = new Regex("^[a-z]");
        Regex firstCharFollowedByUpperCasesOnly = new Regex("(?<=[A-Z])[A-Z0-9]+$");
        Regex lowerCaseNextToNumber = new Regex("(?<=[0-9])[a-z]");
        Regex upperCaseInside = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

        // 用undescore替换空白，然后用空字符串替换所有无效字符
        var pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(original, "_"), string.Empty)

            // 用下划线分割
            .Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)

            // 首字母设置为大写
            .Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))

            // 如果没有下一个小写字母(ABC -> Abc)，则将第二个及所有后面的大写字母替换为小写字母
            .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))

            // 数字后面的第一个小写字母 设置大写(Ab9cd -> Ab9Cd)
            .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))

            // 第二个小写字母和下一个大写字母，除非最后一个字母后跟任何小写字母 (ABcDEf -> AbcDef)
            .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

        return string.Concat(pascalCase);
    }

    #endregion

    #region IsEmpty

    /// <summary>
    /// 是否为空.
    /// </summary>
    /// <param name="value">值.</param>
    public static bool IsEmpty(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// 是否为空.
    /// </summary>
    /// <param name="value">值.</param>
    public static bool IsEmpty(this Guid? value)
    {
        if (value == null)
            return true;
        return IsEmpty(value.Value);
    }

    /// <summary>
    /// 是否为空.
    /// </summary>
    /// <param name="value">值.</param>
    public static bool IsEmpty(this Guid value)
    {
        if (value == Guid.Empty)
            return true;
        return false;
    }

    /// <summary>
    /// 是否为空.
    /// </summary>
    /// <param name="value">值.</param>
    public static bool IsEmpty(this object value)
    {
        if (value != null && !string.IsNullOrEmpty(value.ToString()))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// 判断是否为Null或者空.
    /// </summary>
    /// <param name="obj">对象.</param>
    /// <returns></returns>
    public static bool IsNullOrEmpty(this object obj)
    {
        if (obj == null)
            return true;
        else
        {
            string objStr = obj.ToString();
            return string.IsNullOrEmpty(objStr);
        }
    }

    #endregion

    #region IsNotEmptyOrNull

    /// <summary>
    /// 不为空.
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static string ObjToString(this object thisValue)
    {
        if (thisValue != null) return thisValue.ToString().Trim();
        return string.Empty;
    }

    /// <summary>
    /// 不为空.
    /// </summary>
    /// <param name="thisValue"></param>
    /// <returns></returns>
    public static bool IsNotEmptyOrNull(this object thisValue)
    {
        return ObjToString(thisValue) != string.Empty && ObjToString(thisValue) != "undefined" && ObjToString(thisValue) != "null";
    }

    #endregion

    #region List

    /// <summary>
    /// 嵌套List解析
    /// 仅限于列表查询条件多选.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static List<string> ParseToNestedList(this List<List<string>> list)
    {
        List<string> result = new List<string>();
        if (list != null && list.Count > 0)
        {
            foreach (var item in list)
                result.Add(item.Last());
        }
        return result;
    }

    #endregion
}