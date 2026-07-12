using JNPF.Common.Configuration;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.RemoteRequest.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnceMi.AspNetCore.OSS;
using System.Text;

namespace JNPF.Common.Core.Manager.Files
{
    /// <summary>
    /// 文件管理.
    /// </summary>
    public class FileManager : IFileManager, IScoped
    {
        private readonly IOSSServiceFactory _oSSServiceFactory;
        private readonly IUserManager _userManager;
        private readonly ICacheManager _cacheManager;

        /// <summary>
        /// 文件服务.
        /// </summary>
        public FileManager(
            IUserManager userManager,
            ICacheManager cacheManager,
            IOSSServiceFactory oSSServiceFactory)
        {
            _userManager = userManager;
            _cacheManager = cacheManager;
            _oSSServiceFactory = oSSServiceFactory;
        }

        #region OSS

        /// <summary>
        /// 根据存储类型上传文件.
        /// </summary>
        /// <param name="stream">文件流.</param>
        /// <param name="directoryPath">保存文件夹.</param>
        /// <param name="fileName">新文件名.</param>
        /// <returns></returns>
        public async Task<bool> UploadFileByType(Stream stream, string directoryPath, string fileName)
        {
            try
            {
                var bucketName = KeyVariable.BucketName; // 桶名
                var fileStoreType = KeyVariable.FileStoreType; // 文件存储类型
                var uploadPath = string.Empty; // 上传路径

                switch (fileStoreType)
                {
                    case OSSProviderType.Invalid:
                        uploadPath = Path.Combine(directoryPath, fileName);
                        if (!Directory.Exists(directoryPath))
                            Directory.CreateDirectory(directoryPath);
                        using (var streamLocal = File.Create(uploadPath))
                        {
                            await stream.CopyToAsync(streamLocal);
                        }

                        break;
                    default:
                        uploadPath = string.Format("{0}/{1}", directoryPath, fileName);
                        uploadPath = uploadPath.Replace(@"\", "/");
                        await _oSSServiceFactory.Create(KeyVariable.FileStoreType.ToString()).PutObjectAsync(bucketName, uploadPath, stream);
                        break;
                }
                return true;
            }
            catch (AppFriendlyException ex)
            {
                throw Oops.Oh(ErrorCode.D8001);
            }
        }

        /// <summary>
        /// 根据存储类型下载文件.
        /// </summary>
        /// <param name="filePath">文件路径.</param>
        /// <param name="fileDownLoadName">文件下载名.</param>
        /// <returns></returns>
        public async Task<FileStreamResult> DownloadFileByType(string filePath, string fileDownLoadName)
        {
            try
            {
                filePath = filePath.Replace(@",", "/");
                switch (KeyVariable.FileStoreType)
                {
                    case OSSProviderType.Invalid:
                        switch (Path.GetExtension(fileDownLoadName))
                        {
                            case ".svg":
                                return new FileStreamResult(new FileStream(filePath, FileMode.Open), "image/svg+xml") { FileDownloadName = fileDownLoadName };
                            default:
                                return new FileStreamResult(new FileStream(filePath, FileMode.Open), "application/octet-stream") { FileDownloadName = fileDownLoadName };
                        }
                    default:
                        filePath = filePath.Replace(@"\", "/");
                        var url = await _oSSServiceFactory.Create(KeyVariable.FileStoreType.ToString()).PresignedGetObjectAsync(KeyVariable.BucketName, filePath, 86400);
                        var (stream, encoding) = await url.GetAsStreamAsync();

                        switch (Path.GetExtension(fileDownLoadName))
                        {
                            case ".svg":
                                return new FileStreamResult(stream, "image/svg+xml") { FileDownloadName = fileDownLoadName };
                            default:
                                return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = fileDownLoadName };
                        }
                }
            }
            catch (FileNotFoundException)
            {
                throw Oops.Bah(ErrorCode.D8003);
            }
            catch (DirectoryNotFoundException)
            {
                throw Oops.Bah(ErrorCode.D8003);
            }
            catch (AppFriendlyException)
            {
                throw Oops.Oh(ErrorCode.D8003);
            }
        }

        /// <summary>
        /// 获取指定文件夹下所有文件.
        /// </summary>
        /// <param name="filePath">文件前缀.</param>
        /// <returns></returns>
        [NonAction]
        public async Task<List<AnnexModel>> GetObjList(string filePath)
        {
            try
            {
                switch (KeyVariable.FileStoreType)
                {
                    case OSSProviderType.Invalid:
                        var files = FileHelper.GetAllFiles(filePath);
                        List<AnnexModel> data = new List<AnnexModel>();
                        if (files != null)
                        {
                            for (int i = 0; i < files.Count; i++)
                            {
                                var item = files[i];
                                AnnexModel fileModel = new AnnexModel();
                                fileModel.FileId = i.ToString();
                                fileModel.FileName = item.Name;
                                fileModel.FileType = FileHelper.GetFileType(item);
                                fileModel.FileSize = FileHelper.GetFileSize(item.FullName).ToString();
                                fileModel.FileTime = item.LastWriteTime;
                                data.Add(fileModel);
                            }
                        }

                        return data;
                    default:
                        var bucketName = KeyVariable.BucketName;
                        var list = await _oSSServiceFactory.Create(KeyVariable.FileStoreType.ToString()).ListObjectsAsync(bucketName, filePath);
                        return list.Select(x => new AnnexModel()
                        {
                            FileId = x.ETag,
                            FileName = x.Key.Replace(filePath + "/", string.Empty),
                            FileType = x.Key.Substring(x.Key.LastIndexOf(".") + 1),
                            FileSize = x.Size.ToString(),
                            FileTime = x.LastModifiedDateTime.ParseToDateTime(),
                        }).ToList();
                }
            }
            catch (AppFriendlyException ex)
            {
                throw Oops.Oh(ErrorCode.D8000);
            }

        }

        /// <summary>
        /// 删除文件.
        /// </summary>
        /// <param name="filePath">文件地址.</param>
        /// <returns></returns>
        [NonAction]
        public async Task DeleteFile(string filePath)
        {
            try
            {
                filePath = filePath.Replace(@",", "/");
                switch (KeyVariable.FileStoreType)
                {
                    case OSSProviderType.Invalid:
                        FileHelper.DeleteFile(filePath);
                        break;
                    default:
                        filePath = filePath.Replace(@"\", "/");
                        var bucketName = KeyVariable.BucketName;
                        await _oSSServiceFactory.Create(KeyVariable.FileStoreType.ToString()).RemoveObjectAsync(bucketName, filePath);
                        break;
                }
            }
            catch (AppFriendlyException ex)
            {
                throw Oops.Oh(ErrorCode.D1803);
            }
        }

        /// <summary>
        /// 判断文件是否存在.
        /// </summary>
        /// <param name="filePath">文件路径.</param>
        /// <returns></returns>
        public async Task<bool> ExistsFile(string filePath)
        {
            try
            {
                filePath = filePath.Replace(@",", "/");
                switch (KeyVariable.FileStoreType)
                {
                    case OSSProviderType.Invalid:
                        return FileHelper.Exists(filePath);
                    default:
                        filePath = filePath.Replace(@"\", "/");
                        var bucketName = KeyVariable.BucketName;
                        return await _oSSServiceFactory.Create(KeyVariable.FileStoreType.ToString()).ObjectsExistsAsync(bucketName, filePath);
                }
            }
            catch (AppFriendlyException ex)
            {
                throw Oops.Oh(ErrorCode.D8000);
            }
        }

        /// <summary>
        /// 获取指定文件流.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task<Stream> GetFileStream(string filePath)
        {
            try
            {
                filePath = filePath.Replace(@",", "/");
                switch (KeyVariable.FileStoreType)
                {
                    case OSSProviderType.Invalid:
                        return FileHelper.FileToStream(filePath);
                    default:
                        filePath = filePath.Replace(@"\", "/");
                        var bucketName = KeyVariable.BucketName;
                        var url = await _oSSServiceFactory.Create(KeyVariable.FileStoreType.ToString()).PresignedGetObjectAsync(bucketName, filePath, 86400);
                        return (await url.GetAsStreamAsync()).Stream;
                }
            }
            catch (AppFriendlyException ex)
            {
                throw Oops.Oh(ErrorCode.D1804);
            }
        }

        /// <summary>
        /// 剪切文件.
        /// </summary>
        /// <param name="filePath">源文件地址.</param>
        /// <param name="toFilePath">剪切地址.</param>
        /// <returns></returns>
        public async Task MoveFile(string filePath, string toFilePath)
        {
            try
            {
                filePath = filePath.Replace(@",", "/");
                switch (KeyVariable.FileStoreType)
                {
                    case OSSProviderType.Invalid:
                        FileHelper.MoveFile(filePath, toFilePath);
                        break;
                    default:
                        filePath = filePath.Replace(@"\", "/");
                        var bucketName = KeyVariable.BucketName;
                        await _oSSServiceFactory.Create(KeyVariable.FileStoreType.ToString()).CopyObjectAsync(bucketName, filePath, bucketName, toFilePath);
                        await _oSSServiceFactory.Create(KeyVariable.FileStoreType.ToString()).RemoveObjectAsync(bucketName, filePath);
                        break;
                }
            }
            catch (AppFriendlyException ex)
            {
                throw Oops.Oh(ErrorCode.D1804);
            }
        }

        /// <summary>
        /// 复制文件.
        /// </summary>
        /// <param name="filePath">源文件地址.</param>
        /// <param name="toFilePath">剪切地址.</param>
        /// <returns></returns>
        public async Task CopyFile(string filePath, string toFilePath)
        {
            try
            {
                filePath = filePath.Replace(@",", "/");
                switch (KeyVariable.FileStoreType)
                {
                    case OSSProviderType.Invalid:
                        FileHelper.CopyFile(filePath, toFilePath);
                        break;
                    default:
                        filePath = filePath.Replace(@"\", "/");
                        var bucketName = KeyVariable.BucketName;
                        await _oSSServiceFactory.Create(KeyVariable.FileStoreType.ToString()).CopyObjectAsync(bucketName, filePath, bucketName, toFilePath);
                        await _oSSServiceFactory.Create(KeyVariable.FileStoreType.ToString()).RemoveObjectAsync(bucketName, filePath);
                        break;
                }
            }
            catch (AppFriendlyException ex)
            {
                throw Oops.Oh(ErrorCode.D1804);
            }
        }

        /// <summary>
        /// 生成缩略图并上传.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="saveFileName"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        public async Task MakeThumbnail(Stream stream, string saveFileName, string folder)
        {
            var imgPath = Path.Combine(App.GetConfig<AppOptions>("JNPF_App", true).SystemPath, FileVariable.TemporaryFilePath, "sl_" + saveFileName);
            var flag = FileHelper.MakeThumbnail(stream, 120, 120, "DB", imgPath);
            if (flag)
            {
                var imgStream = FileHelper.FileToStream(imgPath);
                await UploadFileByType(imgStream, folder, "sl_" + saveFileName);
                FileHelper.Delete(imgPath);
            }
        }
        #endregion

        #region 导入导出(json文件)

        /// <summary>
        /// 导出.
        /// </summary>
        /// <param name="jsonStr">json数据.</param>
        /// <param name="name">文件名.</param>
        /// <param name="exportFileType">文件后缀.</param>
        /// <returns></returns>
        public async Task<dynamic> Export(string jsonStr, string name, ExportFileType exportFileType = ExportFileType.json)
        {
            var _filePath = GetPathByType(string.Empty);
            name = DetectionSpecialStr(name);
            var _fileName = string.Format("{0}_{1}.{2}", name, DateTime.Now.ToString("yyyyMMddHHmmss"), exportFileType.ToString());
            var byteList = new UTF8Encoding(true).GetBytes(jsonStr.ToCharArray());
            var stream = new MemoryStream(byteList);
            await UploadFileByType(stream, _filePath, _fileName);
            _cacheManager.Set(_fileName, string.Empty);
            return new {
                name = _fileName,
                url = string.Format("/api/file/Download?encryption={0}", DESCEncryption.Encrypt(string.Format("{0}|{1}|json", _userManager.UserId, _fileName), "JNPF"))
            };
        }

        /// <summary>
        /// 导入.
        /// </summary>
        /// <param name="file">文件.</param>
        /// <returns></returns>
        public string Import(IFormFile file)
        {
            var stream = file.OpenReadStream();
            var byteList = new byte[file.Length];
            stream.Read(byteList, 0, (int)file.Length);
            stream.Position = 0;
            var sr = new StreamReader(stream, Encoding.Default);
            var json = sr.ReadToEnd();
            sr.Close();
            stream.Close();
            return json;
        }
        #endregion

        #region 分块式上传文件

        /// <summary>
        /// 分片上传附件.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<dynamic> UploadChunk([FromForm] ChunkModel input)
        {
            // 碎片临时文件存储路径
            string directoryPath = Path.Combine(App.GetConfig<AppOptions>("JNPF_App", true).SystemPath, "TemporaryFile", input.identifier);
            try
            {
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                // 碎片文件名称
                string chunkFileName = string.Format("{0}{1}{2}", input.identifier, "-", input.chunkNumber);
                string chunkFilePath = Path.Combine(directoryPath, chunkFileName);
                if (!FileHelper.Exists(chunkFilePath))
                {
                    using (var streamLocal = File.Create(chunkFilePath))
                    {
                        await input.file.OpenReadStream().CopyToAsync(streamLocal);
                    }
                }
                return new { merge = FileHelper.GetAllFiles(directoryPath).Count == input.totalChunks };
            }
            catch (AppFriendlyException ex)
            {
                FileHelper.DeleteDirectory(directoryPath);
                throw Oops.Oh(ErrorCode.D8001);
            }
        }

        /// <summary>
        /// 分片组装.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<FileControlsModel> Merge([FromForm] ChunkModel input)
        {
            try
            {
                input.fileName = DetectionSpecialStr(input.fileName);
                // 新文件名称
                var saveFileName = string.Format("{0}{1}{2}", DateTime.Now.ToString("yyyyMMdd"), RandomExtensions.NextLetterAndNumberString(new Random(), 5), Path.GetExtension(input.fileName));
                // 碎片临时文件存储路径
                string directoryPath = Path.Combine(App.GetConfig<AppOptions>("JNPF_App", true).SystemPath, "TemporaryFile", input.identifier);
                var chunkFiles = Directory.GetFiles(directoryPath);
                List<byte> byteSource = new List<byte>();
                var fs = new FileStream(Path.Combine(directoryPath, saveFileName), FileMode.Create);
                foreach (var part in chunkFiles.OrderBy(x => x.Length).ThenBy(x => x))
                {
                    var bytes = FileHelper.ReadAllBytes(part);
                    fs.Write(bytes, 0, bytes.Length);
                    bytes = null;
                    FileHelper.DeleteFile(part);
                }
                fs.Flush();
                fs.Close();
                Stream stream = new FileStream(Path.Combine(directoryPath, saveFileName), FileMode.Open, FileAccess.Read, FileShare.Read);
                GetChunkModel(input, saveFileName);
                var flag = await UploadFileByType(stream, input.folder, saveFileName);
                var fileSize = stream.Length;
                if (flag)
                {
                    stream.Flush();
                    stream.Close();
                    FileHelper.DeleteDirectory(directoryPath);
                }
                return new FileControlsModel { name = input.fileName, url = string.Format("/api/file/Image/annex/{0}", input.fileName), fileExtension = input.extension, fileSize = input.fileSize.ParseToLong(), fileName = input.fileName };
            }
            catch (AppFriendlyException ex)
            {
                throw Oops.Oh(ex.ErrorCode, ex.Args);
            }
        }
        #endregion

        /// <summary>
        /// 根据类型获取文件存储路径.
        /// </summary>
        /// <param name="type">文件类型.</param>
        /// <returns></returns>
        public string GetPathByType(string type)
        {
            switch (type)
            {
                case "userAvatar":
                    return FileVariable.UserAvatarFilePath;
                case "mail":
                    return FileVariable.EmailFilePath;
                case "IM":
                    return FileVariable.IMContentFilePath;
                case "weixin":
                    return FileVariable.MPMaterialFilePath;
                case "workFlow":
                case "annex":
                case "annexpic":
                    return FileVariable.SystemFilePath;
                case "document":
                    return FileVariable.DocumentFilePath;
                case "preview":
                    return FileVariable.DocumentPreviewFilePath;
                case "screenShot":
                case "banner":
                case "bg":
                case "border":
                case "source":
                case "background":
                    return FileVariable.BiVisualPath;
                case "template":
                    return FileVariable.TemplateFilePath;
                case "codeGenerator":
                    return FileVariable.GenerateCodePath;
                default:
                    return FileVariable.TemporaryFilePath;
            }
        }

        /// <summary>
        /// 获取文件大小.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public string GetFileSize(long size)
        {
            var fileSize = string.Empty;
            long factSize = 0;
            factSize = size;
            if (factSize < 1024.00)
                fileSize = factSize.ToString("F2") + "Byte";
            else if (factSize >= 1024.00 && factSize < 1048576)
                fileSize = (factSize / 1024.00).ToString("F2") + "KB";
            else if (factSize >= 1024.00 && factSize < 1048576)
                fileSize = (factSize / 1024.00 / 1024.00).ToString("F2") + "MB";
            else if (factSize >= 1024.00 && factSize < 1048576)
                fileSize = (factSize / 1024.00 / 1024.00 / 1024.00).ToString("F2") + "GB";
            return fileSize;
        }

        /// <summary>
        /// 文件名特殊字符检测.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string DetectionSpecialStr(string fileName)
        {
            foreach (var item in KeyVariable.SpecialString)
            {
                fileName = fileName.Replace(item, string.Empty);
            }
            return fileName;
        }

        /// <summary>
        /// 获取地址和文件名.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="saveFileName"></param>
        public void GetChunkModel(ChunkModel input, string saveFileName)
        {
            var floder = GetPathByType(input.type);
            var fileNameStr = string.Empty;
            var fileNameStr_sl = string.Empty;
            // 自定义路径
            if (input.pathType.Equals("selfPath"))
            {
                if (input.isAccount.Equals("1"))
                {
                    floder = Path.Combine(floder, _userManager.User.Account);
                    fileNameStr = Path.Combine(fileNameStr, _userManager.User.Account);
                }
                if (input.folder.IsNotEmptyOrNull())
                {
                    if (KeyVariable.SpecialString.Any(x => input.folder.Contains(x))) throw Oops.Oh(ErrorCode.D1806);
                    floder = Path.Combine(floder, input.folder.Trim('/'));
                    fileNameStr = Path.Combine(fileNameStr, input.folder);
                }
                fileNameStr_sl = Path.Combine(fileNameStr, "sl_" + saveFileName);
                fileNameStr = Path.Combine(fileNameStr, saveFileName);
                fileNameStr = fileNameStr.Replace("\\", ",").Replace("/", ",");
                fileNameStr_sl = fileNameStr_sl.Replace("\\", ",").Replace("/", ",");
            }
            else
            {
                fileNameStr = saveFileName;
                fileNameStr_sl = "sl_" + saveFileName;
            }
            input.fileName = fileNameStr;
            input.slImgName = fileNameStr_sl;
            input.folder = floder;
        }
    }
}