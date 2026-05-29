using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.DependencyInjection;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace JNPF.Common.Captcha.General;

/// <summary>
/// 常规验证码.
/// </summary>
public class GeneralCaptcha : IGeneralCaptcha, ITransient
{
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 构造函数.
    /// </summary>
    /// <param name="cacheManager"></param>
    public GeneralCaptcha(ICacheManager cacheManager)
    {
        _cacheManager = cacheManager;
    }

    /// <summary>
    /// 常规验证码.
    /// </summary>
    /// <param name="timestamp">时间戳.</param>
    /// <param name="width">宽度.</param>
    /// <param name="height">高度.</param>
    /// <param name="length">长度.</param>
    /// <returns></returns>
    public async Task<byte[]> CreateCaptchaImage(string timestamp, int width, int height, int length = 4)
    {
        return await Draw(timestamp, width, height, length);
    }

    /// <summary>
    /// 画.
    /// </summary>
    /// <param name="timestamp">时间抽.</param>
    /// <param name="width">宽度.</param>
    /// <param name="height">高度.</param>
    /// <param name="length">长度.</param>
    /// <returns></returns>
    private async Task<byte[]> Draw(string timestamp, int width, int height, int length = 4)
    {
        // 颜色列表，用于验证码、噪线、噪点
        var color = new[] { SKColors.Black, SKColors.Red, SKColors.DarkBlue, SKColors.Green, SKColors.Orange, SKColors.Brown, SKColors.DarkCyan, SKColors.Purple };

        var backcolors = new[] { SKColors.AntiqueWhite, SKColors.WhiteSmoke, SKColors.FloralWhite };

        // 验证码字体集合
        string[] fonts = new[] { "Cantarell" };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            fonts = new[] { "宋体" };
        }

        // 验证码随机数
        Random codeRandom = new Random();
        string code = codeRandom.NextLetterAndNumberString(length); // 随机字符串集合

        // 相当于js的 canvas.getContext('2d')
        using SKBitmap bmp = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        // 相当于前端的canvas
        using SKCanvas canvas = new SKCanvas(bmp);

        // 填充白色背景
        canvas.DrawColor(backcolors[codeRandom.Next(0, backcolors.Length - 1)]);

        // 样式 跟xaml差不多
        using SKPaint drawStyle = new SKPaint();

        // 填充验证码到图片
        for (int i = 0; i < code.Length; i++)
        {
            drawStyle.IsAntialias = true;
            drawStyle.TextSize = 30;
            var font = SKTypeface.FromFamilyName(fonts[codeRandom.Next(0, fonts.Length - 1)], SKFontStyleWeight.SemiBold, SKFontStyleWidth.ExtraCondensed, SKFontStyleSlant.Upright);
            drawStyle.Typeface = font;
            drawStyle.Color = color[codeRandom.Next(0, color.Length - 1)];

            // 写字
            canvas.DrawText(code[i].ToString(), (i + 1) * 16, 28, drawStyle);
        }

        // 生成6条干扰线
        for (int i = 0; i < 6; i++)
        {
            drawStyle.Color = color[codeRandom.Next(color.Length)];
            drawStyle.StrokeWidth = 1;
            canvas.DrawLine(codeRandom.Next(0, code.Length * 15), codeRandom.Next(0, 60), codeRandom.Next(0, code.Length * 16), codeRandom.Next(0, 30), drawStyle);
        }

        using var img = SKImage.FromBitmap(bmp);
        using var p = img.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream();

        // 缓存验证码正确集合 
        await SetCode(timestamp, code, TimeSpan.FromMinutes(5));

        // 保存到流
        p.SaveTo(ms);

        return ms.ToArray();
    }

    /// <summary>
    /// 保存验证码缓存.
    /// </summary>
    /// <param name="timestamp">时间戳.</param>
    /// <param name="code">验证码.</param>
    /// <param name="timeSpan">过期时间.</param>
    public async Task<bool> SetCode(string timestamp, string code, TimeSpan timeSpan)
    {
        string cacheKey = string.Format("{0}{1}", CommonConst.CACHEKEYCODE, timestamp);
        return await _cacheManager.SetAsync(cacheKey, code, timeSpan);
    }
}