using JNPF.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using UAParser;

namespace JNPF.Common.Net;

/// <summary>
/// UserAgent操作类.
/// </summary>
[SuppressSniffer]
public class UserAgent : IUserAgent
{
    private readonly static Parser s_uap;

    #region Mobile UAs, OS & Devices

    private static readonly HashSet<string> s_MobileOS = new HashSet<string>
    {
        "Android",
        "iOS",
        "Windows Mobile",
        "Windows Phone",
        "Windows CE",
        "Symbian OS",
        "BlackBerry OS",
        "BlackBerry Tablet OS",
        "Firefox OS",
        "Brew MP",
        "webOS",
        "Bada",
        "Kindle",
        "Maemo"
    };

    private static readonly HashSet<string> s_MobileBrowsers = new HashSet<string>
    {
        "Android",
        "Firefox Mobile",
        "Opera Mobile",
        "Opera Mini",
        "Mobile Safari",
        "Amazon Silk",
        "webOS Browser",
        "MicroB",
        "Ovi Browser",
        "NetFront",
        "NetFront NX",
        "Chrome Mobile",
        "Chrome Mobile iOS",
        "UC Browser",
        "Tizen Browser",
        "Baidu Explorer",
        "QQ Browser Mini",
        "QQ Browser Mobile",
        "IE Mobile",
        "Polaris",
        "ONE Browser",
        "iBrowser Mini",
        "Nokia Services (WAP) Browser",
        "Nokia Browser",
        "Nokia OSS Browser",
        "BlackBerry WebKit",
        "BlackBerry",
        "Palm",
        "Palm Blazer",
        "Palm Pre",
        "Teleca Browser",
        "SEMC-Browser",
        "PlayStation Portable",
        "Nokia",
        "Maemo Browser",
        "Obigo",
        "Bolt",
        "Iris",
        "UP.Browser",
        "Minimo",
        "Bunjaloo",
        "Jasmine",
        "Dolfin",
        "Polaris",
        "Skyfire"
    };

    private static readonly HashSet<string> s_MobileDevices = new HashSet<string>
    {
        "BlackBerry",
        "MI PAD",
        "iPhone",
        "iPad",
        "iPod",
        "Kindle",
        "Kindle Fire",
        "Nokia",
        "Lumia",
        "Palm",
        "DoCoMo",
        "HP TouchPad",
        "Xoom",
        "Motorola",
        "Generic Feature Phone",
        "Generic Smartphone"
    };

    #endregion

    private readonly HttpContext _httpContext;

    private string _rawValue;
    private UserAgentInfo _userAgent;
    private DeviceInfo _device;
    private OSInfo _os;

    private bool? _isBot;
    private bool? _isMobileDevice;
    private bool? _isTablet;
    private bool? _isPdfConverter;

    static UserAgent()
    {
        s_uap = Parser.GetDefault();
    }

    public UserAgent(HttpContext httpContext)
    {
        this._httpContext = httpContext;
    }

    public string RawValue
    {
        get
        {
            if (_rawValue == null)
            {
                if (_httpContext.Request != null)
                {
                    _rawValue = _httpContext.Request.Headers["User-Agent"].ToString() ?? string.Empty;
                }
                else
                {
                    _rawValue = string.Empty;
                }
            }

            return _rawValue ?? string.Empty;
        }

        set
        {
            _rawValue = value;
            _userAgent = null;
            _device = null;
            _os = null;
            _isBot = null;
            _isMobileDevice = null;
            _isTablet = null;
            _isPdfConverter = null;
        }
    }

    public virtual UserAgentInfo userAgent
    {
        get
        {
            if (_userAgent == null)
            {
                var tmp = s_uap.ParseUserAgent(this.RawValue);
                _userAgent = new UserAgentInfo(tmp.Family, tmp.Major, tmp.Minor, tmp.Patch);
            }
            return _userAgent;
        }
    }

    public virtual DeviceInfo Device
    {
        get
        {
            if (_device == null)
            {
                var tmp = s_uap.ParseDevice(this.RawValue);
                _device = new DeviceInfo(tmp.Family, tmp.IsSpider);
            }
            return _device;
        }
    }

    public virtual OSInfo OS
    {
        get
        {
            if (_os == null)
            {
                var tmp = s_uap.ParseOS(this.RawValue);
                _os = new OSInfo(tmp.Family, tmp.Major, tmp.Minor, tmp.Patch, tmp.PatchMinor);
            }
            return _os;
        }
    }

    public virtual bool IsMobileDevice
    {
        get
        {
            if (!_isMobileDevice.HasValue)
            {
                _isMobileDevice =
                    s_MobileOS.Contains(this.OS.Family) ||
                    s_MobileBrowsers.Contains(this.userAgent.Family) ||
                    s_MobileDevices.Contains(this.Device.Family);
            }
            return _isMobileDevice.Value;
        }
    }

    public virtual bool IsTablet
    {
        get
        {
            if (!_isTablet.HasValue)
            {
                _isTablet =
                    Regex.IsMatch(this.Device.Family, "iPad|Kindle Fire|Nexus 10|Xoom|Transformer|MI PAD|IdeaTab", RegexOptions.CultureInvariant) ||
                    this.OS.Family == "BlackBerry Tablet OS";
            }

            return _isTablet.Value;
        }
    }
}