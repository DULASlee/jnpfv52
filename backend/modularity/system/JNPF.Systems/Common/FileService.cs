using JNPF.Common.Captcha.General;
using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace JNPF.Systems.Common;

/// <summary>
/// 业务实现：通用控制器.
/// </summary>
[ApiDescriptionSettings(Tag = "Common", Name = "File", Order = 161)]
[Route("api/[controller]")]
[AllowAnonymous]
public class FileService : IFileService, IDynamicApiController, ITransient
{
    private readonly AppOptions _appOptions;

    /// <summary>
    /// 验证码处理程序.
    /// </summary>
    private readonly IGeneralCaptcha _captchaHandler;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 缓存服务.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 初始化一个<see cref="FileService"/>类型的新实例.
    /// </summary>
    public FileService(
        IOptions<AppOptions> appOptions,
        IGeneralCaptcha captchaHandler,
        IUserManager userManager,
        ICacheManager cacheManager,
        IFileManager fileManager)
    {
        _appOptions = appOptions.Value;
        _captchaHandler = captchaHandler;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _fileManager = fileManager;
    }

    #region GET

    /// <summary>
    /// 上传文件预览.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Uploader/Preview")]
    [LogPolicy(LogPolicy.IgnoreResponse)]
    public async Task<dynamic> Preview(string fileName, string fileDownloadUrl)
    {
        string[]? typeList = new string[] { "doc", "docx", "xls", "xlsx", "ppt", "pptx", "pdf", "jpg", "jpeg", "gif", "png", "bmp" };
        string? type = fileName.Split('.').LastOrDefault();
        if (typeList.Contains(type))
        {
            if (fileName.IsNotEmptyOrNull())
            {
                string previewUrl = string.Empty;
                switch (_appOptions.PreviewType)
                {
                    case PreviewType.kkfile:
                        previewUrl = KKFileUploaderPreview(fileName, fileDownloadUrl);
                        break;
                    case PreviewType.yozo:
                        previewUrl = await YoZoUploaderPreview(fileName, 5, 1);
                        break;
                }

                return previewUrl;
            }
            else
            {
                throw Oops.Oh(ErrorCode.D8000);
            }
        }
        else
        {
            throw Oops.Oh(ErrorCode.D1802);
        }
    }

    /// <summary>
    /// 生成图片链接.
    /// </summary>
    /// <param name="type">图片类型.</param>
    /// <param name="fileName">注意 后缀名前端故意把 .替换@ .</param>
    /// <returns></returns>
    [HttpGet("Image/{type}/{fileName}")]
    [LogPolicy(LogPolicy.IgnoreResponse)]
    public async Task<IActionResult> GetImg(string type, string fileName)
    {
        var resolvedName = fileName.Replace("@", ".");
        var dir = GetPathByType(type);
        var filePath = Path.Combine(dir, resolvedName);

        // 本地存储：缺文件时头像回退 001.png / 内置占位图，避免 FileNotFound → FriendlyException ERR 刷屏
        if (KeyVariable.FileStoreType == OSSProviderType.Invalid && !global::System.IO.File.Exists(filePath))
        {
            if (string.Equals(type, "userAvatar", StringComparison.OrdinalIgnoreCase))
            {
                var fallback = Path.Combine(dir, "001.png");
                if (global::System.IO.File.Exists(fallback))
                {
                    filePath = fallback;
                    resolvedName = "001.png";
                }
                else
                {
                    return new FileContentResult(DefaultUserAvatarPng, "image/png") { FileDownloadName = "001.png" };
                }
            }
            else
            {
                return new NotFoundResult();
            }
        }

        return await _fileManager.DownloadFileByType(filePath, resolvedName);
    }

    /// <summary>1×1 透明 PNG，仅作本地缺省头像占位（FileStorage/UserAvatar 未初始化时）。</summary>
    private static readonly byte[] DefaultUserAvatarPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    /// <summary>
    /// 生成大屏图片链接.
    /// </summary>
    /// <param name="type">图片类型.</param>
    /// <param name="fileName">注意 后缀名前端故意把 .替换@ .</param>
    /// <returns></returns>
    [HttpGet("VisusalImg/BiVisualPath/{type}/{fileName}")]
    [LogPolicy(LogPolicy.IgnoreResponse)]
    public async Task<IActionResult> GetScreenImg(string type, string fileName)
    {
        string filePath = Path.Combine(GetPathByType(type), type, fileName.Replace("@", "."));
        return await _fileManager.DownloadFileByType(filePath, fileName);
    }

    /// <summary>
    /// 获取图形验证码.
    /// </summary>
    /// <param name="timestamp">时间戳.</param>
    /// <returns></returns>
    [HttpGet("ImageCode/{timestamp}")]
    [NonUnify]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public async Task<IActionResult> GetCode(string timestamp)
    {
        return new FileContentResult(await _captchaHandler.CreateCaptchaImage(timestamp, 114, 32), "image/jpeg");
    }

    /// <summary>
    /// 下载.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="type"></param>
    [HttpGet("down/{fileName}")]
    [LogPolicy(LogPolicy.IgnoreResponse)]
    public async Task FileDown(string fileName, [FromQuery] string type)
    {
        string? systemFilePath = Path.Combine(FileVariable.SystemFilePath, fileName);
        if (type.IsNotEmptyOrNull())
        {
            systemFilePath = Path.Combine(_fileManager.GetPathByType(type), fileName);
        }
        var fileStreamResult = await _fileManager.DownloadFileByType(systemFilePath, fileName);
        byte[] bytes = new byte[fileStreamResult.FileStream.Length];

        fileStreamResult.FileStream.Read(bytes, 0, bytes.Length);

        fileStreamResult.FileStream.Close();
        var httpContext = App.HttpContext;
        httpContext.Response.ContentType = "application/octet-stream";
        httpContext.Response.Headers.Add("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(fileName, Encoding.UTF8));
        httpContext.Response.Headers.Add("Content-Length", bytes.Length.ToString());
        await httpContext.Response.Body.WriteAsync(bytes);
        httpContext.Response.Body.Flush();
        httpContext.Response.Body.Close();
    }

    #region 下载附件

    /// <summary>
    /// 获取下载文件链接.
    /// </summary>
    /// <param name="type">图片类型.</param>
    /// <param name="fileName">文件名称.</param>
    /// <returns></returns>
    [HttpGet("Download/{type}/{fileName}")]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public dynamic DownloadUrl(string type, string fileName)
    {
        string? url = string.Format("{0}|{1}|{2}", _userManager.UserId, fileName, type);
        string? encryptStr = DESCEncryption.Encrypt(url, "JNPF");
        _cacheManager.Set(fileName, string.Empty);
        return new { name = fileName, url = string.Format("/api/file/Download?encryption={0}", encryptStr) };
    }

    /// <summary>
    /// 全部下载.
    /// </summary>
    /// <param name="type">图片类型.</param>
    /// <param name="fileName">文件名称.</param>
    /// <returns></returns>
    [HttpPost("PackDownload/{type}")]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public async Task<dynamic> DownloadAll(string type, [FromBody] List<FileControlsModel> input)
    {
        var fileName = RandomExtensions.NextLetterAndNumberString(new Random(), 7);
        //临时目录
        string directoryPath = Path.Combine(App.GetConfig<AppOptions>("JNPF_App", true).SystemPath, "TemporaryFile", fileName);
        Directory.CreateDirectory(directoryPath);
        foreach (var item in input)
        {
            string filePath = Path.Combine(GetPathByType(type), item.fileId.Replace("@", "."));
            await _fileManager.CopyFile(filePath, Path.Combine(directoryPath, item.fileName));
        }
        // 压缩文件
        string downloadPath = directoryPath + ".zip";

        // 判断是否存在同名称文件
        if (File.Exists(downloadPath))
            File.Delete(downloadPath);

        ZipFile.CreateFromDirectory(directoryPath, downloadPath);
        if (!App.Configuration["OSS:Provider"].Equals("Invalid"))
            await UploadFileByType(downloadPath, "SystemPath", string.Format("文件{0}.zip", fileName));
        var downloadFileName = string.Format("{0}|{1}.zip|TemporaryFile", _userManager.UserId, fileName);
        _cacheManager.Set(fileName + ".zip", string.Empty);
        return new { downloadName = string.Format("文件{0}.zip", fileName), downloadVo = new { name = fileName, url = "/api/File/Download?encryption=" + DESCEncryption.Encrypt(downloadFileName, "JNPF") } };
    }

    /// <summary>
    /// 下载文件链接.
    /// </summary>
    [HttpGet("Download")]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public async Task<dynamic> DownloadFile([FromQuery] string encryption, [FromQuery] string name)
    {
        string decryptStr = DESCEncryption.Decrypt(encryption, "JNPF");
        List<string> paramsList = decryptStr.Split("|").ToList();
        if (paramsList.Count > 0)
        {
            string fileName = paramsList.Count > 1 ? paramsList[1] : string.Empty;
            if (_cacheManager.Exists(fileName))
            {
                _cacheManager.Del(fileName);
            }
            else
            {
                throw Oops.Oh(ErrorCode.D1805);
            }
            string type = paramsList.Count > 2 ? paramsList[2] : string.Empty;
            string filePath = Path.Combine(GetPathByType(type), fileName.Replace("@", "."));
            string fileDownloadName = name.IsNullOrEmpty() ? fileName : name;
            return await _fileManager.DownloadFileByType(filePath, fileDownloadName);
        }
        else
        {
            throw Oops.Oh(ErrorCode.D8000);
        }
    }

    /// <summary>
    /// App启动信息.
    /// </summary>
    [HttpGet("AppStartInfo/{appName}")]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public async Task<dynamic> AppStartInfo(string appName)
    {
        return new { appVersion = KeyVariable.AppVersion, appUpdateContent = KeyVariable.AppUpdateContent };
    }

    #endregion

    /// <summary>
    /// 分片上传获取.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("chunk")]
    [LogPolicy(LogPolicy.IgnoreAll)]
    public async Task<dynamic> CheckChunk([FromQuery] ChunkModel input)
    {
        try
        {
            if (!AllowFileType(input.extension, input.extension))
                throw Oops.Oh(ErrorCode.D1800);
            string path = GetPathByType(string.Empty);
            string filePath = Path.Combine(path, input.identifier);
            var chunkFiles = FileHelper.GetAllFiles(filePath);
            List<int> existsChunk = chunkFiles.FindAll(x => !FileHelper.GetFileType(x).Equals("tmp"))
                .Select(x => x.FullName.Replace(input.identifier + "-", string.Empty).ParseToInt()).ToList();
            return new { chunkNumbers = existsChunk, merge = false };
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    #endregion

    #region POST

    /// <summary>
    /// 上传文件/图片.
    /// </summary>
    /// <returns></returns>
    [HttpPost("Uploader/{type}")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.Minimal)]
    public async Task<dynamic> Uploader(string type, [FromForm] ChunkModel input)
    {
        string? fileType = Path.GetExtension(input.file.FileName).Replace(".", string.Empty);
        if (!AllowFileType(fileType, type))
            throw Oops.Oh(ErrorCode.D1800);
        string saveFileName = string.Format("{0}{1}{2}", DateTime.Now.ToString("yyyyMMdd"), RandomExtensions.NextLetterAndNumberString(new Random(), 5), Path.GetExtension(input.file.FileName));
        var stream = input.file.OpenReadStream();
        input.type = type;
        _fileManager.GetChunkModel(input, saveFileName);
        await _fileManager.UploadFileByType(stream, input.folder, saveFileName);

     


        if (AllowImageType(fileType) && type.Equals("annexpic"))
        {
            var slStram = await _fileManager.GetFileStream(Path.Combine(input.folder, saveFileName));
            await _fileManager.MakeThumbnail(slStram, saveFileName, input.folder);

            if (KeyVariable.FileStoreType ==    OSSProviderType.Invalid)
            {
                //检查是否存在缩略图片，不存在直接引用原图
                string checkSlImg = Path.Combine(input.folder, input.slImgName);
                if (!File.Exists(checkSlImg))
                {
                    input.slImgName = input.fileName;
                }
            }

            var a = new FileControlsModel { name = input.fileName, url = string.Format("/api/File/Image/{0}/{1}", type, input.fileName), thumbUrl = string.Format("/api/File/Image/{0}/{1}", type, input.slImgName), fileExtension = fileType, fileSize = input.file.Length, fileName = input.slImgName };
            return a;
        }
        else
        {
            return new FileControlsModel { name = input.fileName, url = string.Format("/api/File/Image/{0}/{1}", type, input.fileName), fileExtension = fileType, fileSize = input.file.Length, fileName = input.fileName };
        }
    }

    /// <summary>
    /// 上传图片.
    /// </summary>
    /// <returns></returns>
    [HttpPost("Uploader/userAvatar")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.Minimal)]
    public async Task<dynamic> UploadImage(IFormFile file)
    {
        string? ImgType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!this.AllowImageType(ImgType))
            throw Oops.Oh(ErrorCode.D5013);
        string? filePath = FileVariable.UserAvatarFilePath;
        string? fileName = string.Format("{0}{1}{2}", DateTime.Now.ToString("yyyyMMdd"), RandomExtensions.NextLetterAndNumberString(new Random(), 5), Path.GetExtension(file.FileName));
        var stream = file.OpenReadStream();
        await _fileManager.UploadFileByType(stream, filePath, fileName);
        return new FileControlsModel { name = fileName, url = string.Format("/api/file/Image/userAvatar/{0}", fileName), fileSize = file.Length, fileExtension = ImgType };
    }

    /// <summary>
    /// 分片上传附件.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("chunk")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.Minimal)]
    public async Task<dynamic> UploadChunk([FromForm] ChunkModel input)
    {
        if (!AllowFileType(input.extension, input.extension))
            throw Oops.Oh(ErrorCode.D1800);
        return await _fileManager.UploadChunk(input);
    }

    /// <summary>
    /// 分片组装.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("merge")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.Minimal)]
    public async Task<dynamic> Merge([FromForm] ChunkModel input)
    {
        return await _fileManager.Merge(input);
    }
    #endregion

    #region PublicMethod

    #region 多种存储文件

    /// <summary>
    /// 根据存储类型上传文件.
    /// </summary>
    /// <param name="uploadFilePath">上传文件地址.</param>
    /// <param name="directoryPath">保存文件夹.</param>
    /// <param name="fileName">新文件名.</param>
    /// <returns></returns>
    [NonAction]
    public async Task UploadFileByType(string uploadFilePath, string directoryPath, string fileName)
    {
        using var file = new FileStream(uploadFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await _fileManager.UploadFileByType(file, directoryPath, fileName);
    }
    #endregion

    /// <summary>
    /// 根据类型获取文件存储路径.
    /// </summary>
    /// <param name="type">文件类型.</param>
    /// <returns></returns>
    [NonAction]
    public string GetPathByType(string type)
    {
        return _fileManager.GetPathByType(type);
    }

    #region kkfile 文件预览

    /// <summary>
    /// KKFile 文件预览.
    /// </summary>
    /// <param name="fileName">文件名称.</param>
    /// <param name="fileDownloadUrl">文件地址.</param>
    /// <returns></returns>
    public string KKFileUploaderPreview(string fileName, string fileDownloadUrl)
    {
        var domain = App.Configuration["JNPF_APP:Domain"];
        var filePath = (domain + "/api/File/down/" + fileName).ToBase64String();
        if (fileDownloadUrl.IsNotEmptyOrNull())
        {
            var list = fileDownloadUrl.Split('/');
            var type = list.Length > 4 ? list[4] : string.Empty;
            filePath = string.Format("{0}{1}{2}?type={3}", domain, "/api/File/down/", fileName, type).ToBase64String();
        }
        var kkFileDoMain = App.Configuration["JNPF_APP:KKFileDomain"];
        var kkurl = kkFileDoMain + "/onlinePreview?url=";

        return kkurl + filePath;
    }

    #endregion

    #region YoZo 生成 sign 方法

    /// <summary>
    /// 调用YoZo 文件预览.
    /// </summary>
    /// <param name="fileName">文件名.</param>
    /// <param name="maxNumber">最多请求次数.</param>
    /// <param name="number">当前请求次数.</param>
    /// <returns></returns>
    public async Task<string> YoZoUploaderPreview(string fileName, int maxNumber, int number)
    {
        string domain = _appOptions.YOZO.Domain;
        string uploadAPI = _appOptions.YOZO.UploadAPI;
        string downloadAPI = _appOptions.YOZO.DownloadAPI;
        string yozoAppId = _appOptions.YOZO.AppId;
        string yozoAppKey = _appOptions.YOZO.AppKey;
        string outputFilePath = string.Format("{0}/api/File/Image/annex/{1}", domain, fileName);

        Dictionary<string, string[]> dic = new Dictionary<string, string[]>();
        dic.Add("fileUrl", new string[] { outputFilePath });
        dic.Add("appId", new string[] { yozoAppId });
        string? sign = generateSign(yozoAppKey, dic);
        uploadAPI = string.Format(uploadAPI, outputFilePath, yozoAppId, sign);
        string? resStr = await uploadAPI.PostAsStringAsync();
        if (resStr.IsNotEmptyOrNull())
        {
            Dictionary<string, object>? result = resStr.ToObject<Dictionary<string, object>>();
            if (result.ContainsKey("data"))
            {
                Dictionary<string, object>? data = result["data"].ToObject<Dictionary<string, object>>();
                if (data != null)
                {
                    string? fileVersionId = data.ContainsKey("fileVersionId") ? data["fileVersionId"].ToString() : string.Empty;

                    #region 生成签名sign

                    dic = new Dictionary<string, string[]>();
                    dic.Add("fileVersionId", new string[] { fileVersionId });
                    dic.Add("appId", new string[] { yozoAppId });
                    sign = generateSign(yozoAppKey, dic);

                    #endregion

                    return string.Format(downloadAPI, fileVersionId, yozoAppId, sign);
                }
                else
                {
                    return await YoZoUploaderPreview(fileName, maxNumber, number + 1);
                }
            }
            else
            {
                if (number >= maxNumber) return string.Empty;
                else return await YoZoUploaderPreview(fileName, maxNumber, number + 1);
            }
        }
        else
        {
            if (number >= maxNumber) return string.Empty;
            else return await YoZoUploaderPreview(fileName, maxNumber, number + 1);
        }
    }

    #endregion

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 生成令牌.
    /// </summary>
    /// <param name="secret">签名.</param>
    /// <param name="paramMap">参数集合.</param>
    /// <returns></returns>
    private string generateSign(string secret, Dictionary<string, string[]> paramMap)
    {
        string fullParamStr = uniqSortParams(paramMap);
        return HmacSHA256(fullParamStr, secret);
    }

    /// <summary>
    /// uniq类型参数.
    /// </summary>
    /// <param name="paramMap"></param>
    /// <returns></returns>
    private string uniqSortParams(Dictionary<string, string[]> paramMap)
    {
        paramMap.Remove("sign");
        paramMap = paramMap.OrderBy(o => o.Key).ToDictionary(o => o.Key, p => p.Value);
        StringBuilder strB = new StringBuilder();
        foreach (KeyValuePair<string, string[]> kvp in paramMap)
        {
            string key = kvp.Key;
            string[] value = kvp.Value;
            if (value.Length > 0)
            {
                Array.Sort(value);
                foreach (string temp in value)
                {
                    strB.Append(key).Append("=").Append(temp);
                }
            }
            else
            {
                strB.Append(key).Append("=");
            }

        }

        return strB.ToString();
    }

    /// <summary>
    /// 加密.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private string HmacSHA256(string data, string key)
    {
        string signRet = string.Empty;
        using (HMACSHA256 mac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            byte[] hash = mac.ComputeHash(Encoding.UTF8.GetBytes(data));
            signRet = hash.ToHexString();
        }

        return signRet;
    }

    /// <summary>
    /// 允许文件类型.
    /// </summary>
    /// <param name="fileExtension">文件后缀名.</param>
    /// <param name="type">文件类型.</param>
    /// <returns></returns>
    private bool AllowFileType(string fileExtension, string type)
    {
        List<string> allowExtension = KeyVariable.AllowUploadFileType.Distinct().ToList();
        if (fileExtension.IsNullOrEmpty() || type.IsNullOrEmpty()) return false;
        if (type.Equals("weixin"))
            allowExtension = KeyVariable.WeChatUploadFileType;
        foreach (var item in Enum.GetValues(typeof(ExportFileType)))
        {
            allowExtension.Add(item.ToString());
        }
        return allowExtension.Any(a => a == fileExtension.ToLower());
    }

    /// <summary>
    /// 允许文件类型.
    /// </summary>
    /// <param name="fileExtension">文件后缀名.</param>
    /// <returns></returns>
    private bool AllowImageType(string fileExtension)
    {
        return KeyVariable.AllowImageType.Any(a => a == fileExtension.ToLower());
    }

    #endregion
}