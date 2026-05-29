namespace JNPF.Common.Net;

public interface IUserAgent
{
    string RawValue { get; set; }

    UserAgentInfo userAgent { get; }

    DeviceInfo Device { get; }

    OSInfo OS { get; }

    /// <summary>
    /// 是否移动端.
    /// </summary>
    bool IsMobileDevice { get; }

    /// <summary>
    /// 是否平板.
    /// </summary>
    bool IsTablet { get; }
}

public sealed class DeviceInfo
{
    public DeviceInfo(string family, bool isBot)
    {
        this.Family = family;
        this.IsBot = isBot;
    }

    public override string ToString()
    {
        return this.Family;
    }

    public string Family { get; private set; }
    public bool IsBot { get; private set; }
}

public sealed class OSInfo
{
    public OSInfo(string family, string major, string minor, string patch, string patchMinor)
    {
        this.Family = family;
        this.Major = major;
        this.Minor = minor;
        this.Patch = patch;
        this.PatchMinor = patchMinor;
    }

    public override string ToString()
    {
        var str = VersionString.Format(Major, Minor, Patch, PatchMinor);
        return (this.Family + (!string.IsNullOrEmpty(str) ? (" " + str) : null));
    }

    public string Family { get; private set; }
    public string Major { get; private set; }
    public string Minor { get; private set; }
    public string Patch { get; private set; }
    public string PatchMinor { get; private set; }

    private static string FormatVersionString(params string[] parts)
    {
        return string.Join(".", (from v in parts
                                 where !string.IsNullOrEmpty(v)
                                 select v).ToArray<string>());
    }
}

public sealed class UserAgentInfo
{
    public UserAgentInfo(string family, string major, string minor, string patch)
    {
        this.Family = family;
        this.Major = major;
        this.Minor = minor;
        this.Patch = patch;
    }

    public override string ToString()
    {
        var str = VersionString.Format(Major, Minor, Patch);
        return (this.Family + (!string.IsNullOrEmpty(str) ? (" " + str) : null));
    }

    public string Family { get; private set; }

    public string Major { get; private set; }

    public string Minor { get; private set; }

    public string Patch { get; private set; }
}

internal static class VersionString
{
    public static string Format(params string[] parts)
    {
        return string.Join(".", (from v in parts
                                 where !string.IsNullOrEmpty(v)
                                 select v).ToArray<string>());
    }
}