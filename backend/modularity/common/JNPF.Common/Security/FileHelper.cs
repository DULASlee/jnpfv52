using JNPF.Common.Configuration;
using JNPF.Common.Extension;
using JNPF.Common.Models;
using JNPF.DependencyInjection;
using System.Text;
using System.Web;

namespace JNPF.Common.Security;

/// <summary>
/// FileHelper
/// 版 本：V3.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组.
/// </summary>
[SuppressSniffer]
public class FileHelper
{
    #region 返回绝对路径

    /// <summary>
    /// 返回绝对路径.
    /// </summary>
    /// <param name="filePath">相对路径.</param>
    /// <returns></returns>
    public static string GetAbsolutePath(string filePath)
    {
        return Directory.GetCurrentDirectory() + filePath;
    }

    #endregion

    #region 检测指定目录是否存在

    /// <summary>
    /// 检测指定目录是否存在.
    /// </summary>
    /// <param name="directoryPath">目录的绝对路径</param>
    /// <returns></returns>
    public static bool IsExistDirectory(string directoryPath)
    {
        return Directory.Exists(directoryPath);
    }

    #endregion

    #region 检测指定文件是否存在,如果存在返回true

    /// <summary>
    /// 检测指定文件是否存在,如果存在则返回true.
    /// </summary>
    /// <param name="filePath">文件的绝对路径.</param>
    public static bool IsExistFile(string filePath)
    {
        return File.Exists(filePath);
    }

    /// <summary>
    /// 检测指定文件是否存在,如果存在则返回true.
    /// </summary>
    /// <param name="filePath">文件的绝对路径.</param>
    public static bool Exists(string filePath)
    {
        return File.Exists(filePath);
    }

    #endregion

    #region 获取指定目录中的文件列表

    /// <summary>
    /// 获取指定目录中所有文件列表.
    /// </summary>
    /// <param name="directoryPath">指定目录的绝对路径.</param>
    public static string[] GetFileNames(string directoryPath)
    {
        // 如果目录不存在，则抛出异常
        if (!IsExistDirectory(directoryPath))
            throw new FileNotFoundException();
        return Directory.GetFiles(directoryPath);
    }

    /// <summary>
    /// 获取指定目录中所有文件列表.
    /// </summary>
    /// <param name="directoryPath">指定目录的绝对路径.</param>
    /// <param name="data">返回文件.</param>
    /// <returns></returns>
    public static List<FileInfo> GetAllFiles(string directoryPath, List<FileInfo>? data = null)
    {
        if (!IsExistDirectory(directoryPath))
            return new List<FileInfo>();
        List<FileInfo> listFiles = data == null ? new List<FileInfo>() : data;
        DirectoryInfo directory = new DirectoryInfo(directoryPath);
        DirectoryInfo[] directorys = directory.GetDirectories();
        FileInfo[] fileInfos = directory.GetFiles();

        if (fileInfos.Length > 0)
            listFiles.AddRange(fileInfos);

        foreach (DirectoryInfo itemDirectory in directorys)
        {
            GetAllFiles(itemDirectory.FullName, listFiles);
        }

        return listFiles;
    }

    #endregion

    #region 获取指定目录中所有子目录列表,若要搜索嵌套的子目录列表,请使用重载方法.

    /// <summary>
    /// 获取指定目录中所有子目录列表,若要搜索嵌套的子目录列表,请使用重载方法.
    /// </summary>
    /// <param name="directoryPath">指定目录的绝对路径.</param>
    public static string[] GetDirectories(string directoryPath)
    {
        try
        {
            return Directory.GetDirectories(directoryPath);
        }
        catch (IOException)
        {
            throw;
        }
    }

    #endregion

    #region 获取指定目录及子目录中所有文件列表

    /// <summary>
    /// 获取指定目录及子目录中所有文件列表.
    /// </summary>
    /// <param name="directoryPath">指定目录的绝对路径.</param>
    /// <param name="searchPattern">模式字符串，"*"代表0或N个字符，"?"代表1个字符
    /// 范例："Log*.xml"表示搜索所有以Log开头的Xml文件.</param>
    /// <param name="isSearchChild">是否搜索子目录.</param>
    public static string[] GetFileNames(string directoryPath, string searchPattern, bool isSearchChild)
    {
        // 如果目录不存在，则抛出异常
        if (!IsExistDirectory(directoryPath))
            throw new FileNotFoundException();

        try
        {
            if (isSearchChild)
                return Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
            else
                return Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
        }
        catch (IOException)
        {
            throw;
        }
    }

    #endregion

    #region 创建目录

    /// <summary>
    /// 创建目录.
    /// </summary>
    /// <param name="dir">要创建的目录路径包括目录名.</param>
    public static void CreateDir(string dir)
    {
        if (dir.Length == 0) return;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    #endregion

    #region 删除目录

    /// <summary>
    /// 删除指定目录及其所有子目录.
    /// </summary>
    /// <param name="dir">要删除的目录路径和名称.</param>
    public static void DeleteDirectory(string dir)
    {
        if (dir.Length == 0) return;
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
    }

    #endregion

    #region 删除文件

    /// <summary>
    /// 删除文件.
    /// </summary>
    /// <param name="file">要删除的文件路径和名称.</param>
    public static void DeleteFile(string file)
    {
        if (File.Exists(file))
            File.Delete(file);
    }

    /// <summary>
    /// 删除文件.
    /// </summary>
    /// <param name="file">要删除的文件路径和名称.</param>
    public static void Delete(string file)
    {
        if (File.Exists(file))
            File.Delete(file);
    }

    #endregion

    #region 创建文件

    /// <summary>
    /// 创建文件.
    /// </summary>
    /// <param name="dir">带后缀的文件名.</param>
    /// <param name="content">文件内容.</param>
    public static void CreateFile(string dir, string content)
    {
        dir = dir.Replace("/", "\\");
        if (dir.IndexOf("\\") > -1)
            CreateDir(dir.Substring(0, dir.LastIndexOf("\\")));
        StreamWriter sw = new StreamWriter(dir, false);
        sw.Write(content);
        sw.Close();
        sw.Dispose();
    }

    /// <summary>
    /// 创建文件.
    /// </summary>
    /// <param name="filePath">文件的绝对路径.</param>
    public static void CreateFile(string filePath)
    {
        if (!IsExistFile(filePath))
        {
            FileInfo file = new FileInfo(filePath);
            FileStream fs = file.Create();
            fs.Close();
        }
    }

    /// <summary>
    /// 创建文件,并将字节流写入文件.
    /// </summary>
    /// <param name="filePath">文件的绝对路径.</param>
    /// <param name="buffer">二进制流数据.</param>
    public static void CreateFile(string filePath, byte[] buffer)
    {
        if (!IsExistFile(filePath))
        {
            FileInfo file = new FileInfo(filePath);
            FileStream fs = file.Create();
            fs.Write(buffer, 0, buffer.Length);
            fs.Close();
        }
    }

    #endregion

    #region 移动文件

    /// <summary>
    /// 移动文件(剪贴--粘贴).
    /// </summary>
    /// <param name="dir1">要移动的文件的路径及全名(包括后缀).</param>
    /// <param name="dir2">文件移动到新的位置,并指定新的文件名.</param>
    public static void MoveFile(string dir1, string dir2)
    {
        if (File.Exists(dir1))
            File.Move(dir1, dir2);
    }

    #endregion

    #region 复制文件

    /// <summary>
    /// 复制文件.
    /// </summary>
    /// <param name="dir1">要复制的文件的路径已经全名(包括后缀).</param>
    /// <param name="dir2">目标位置,并指定新的文件名.</param>
    public static void CopyFile(string dir1, string dir2)
    {
        if (File.Exists(dir1))
            File.Copy(dir1, dir2);
    }

    #endregion

    #region 复制文件夹

    /// <summary>
    /// 复制文件夹(递归).
    /// </summary>
    /// <param name="varFromDirectory">源文件夹路径.</param>
    /// <param name="varToDirectory">目标文件夹路径.</param>
    public static void CopyFolder(string varFromDirectory, string varToDirectory)
    {
        Directory.CreateDirectory(varToDirectory);

        if (!Directory.Exists(varFromDirectory)) return;

        string[] directories = Directory.GetDirectories(varFromDirectory);

        if (directories.Length > 0)
        {
            foreach (string d in directories)
            {
                CopyFolder(d, varToDirectory + d.Substring(d.LastIndexOf("\\")));
            }
        }

        string[] files = Directory.GetFiles(varFromDirectory);
        if (files.Length > 0)
        {
            foreach (string s in files)
            {
                File.Copy(s, varToDirectory + s.Substring(s.LastIndexOf("\\")), true);
            }
        }
    }

    #endregion

    #region 删除指定文件夹对应其他文件夹里的文件

    /// <summary>
    /// 删除指定文件夹对应其他文件夹里的文件.
    /// </summary>
    /// <param name="varFromDirectory">指定文件夹路径.</param>
    /// <param name="varToDirectory">对应其他文件夹路径.</param>
    public static void DeleteFolderFiles(string varFromDirectory, string varToDirectory)
    {
        Directory.CreateDirectory(varToDirectory);
        if (!Directory.Exists(varFromDirectory)) return;
        string[] directories = Directory.GetDirectories(varFromDirectory);
        if (directories.Length > 0)
        {
            foreach (string d in directories)
            {
                DeleteFolderFiles(d, varToDirectory + d.Substring(d.LastIndexOf("\\")));
            }
        }

        string[] files = Directory.GetFiles(varFromDirectory);
        if (files.Length > 0)
        {
            foreach (string s in files)
            {
                File.Delete(varToDirectory + s.Substring(s.LastIndexOf("\\")));
            }
        }
    }

    #endregion

    #region 从文件的绝对路径中获取文件名( 包含扩展名 )

    /// <summary>
    /// 从文件的绝对路径中获取文件名( 包含扩展名 ).
    /// </summary>
    /// <param name="filePath">文件的绝对路径.</param>
    public static string GetFileName(string filePath)
    {
        // 获取文件的名称
        FileInfo fi = new FileInfo(filePath);
        return fi.Name;
    }

    #endregion

    #region 获取一个文件的长度

    /// <summary>
    /// 获取一个文件的长度,单位为Byte.
    /// </summary>
    /// <param name="filePath">文件的绝对路径.</param>
    public static long GetFileSize(string filePath)
    {
        FileInfo fi = new FileInfo(filePath);
        return fi.Length;
    }

    #endregion

    #region 获取文件大小并以B，KB，GB，TB

    /// <summary>
    /// 计算文件大小函数(保留两位小数),Size为字节大小.
    /// </summary>
    /// <param name="size">初始文件大小.</param>
    /// <returns></returns>
    public static string ToFileSize(long size)
    {
        string m_strSize = string.Empty;
        long factSize = 0;
        factSize = size;
        if (factSize < 1024.00)
            m_strSize = factSize.ToString("F2") + " 字节";
        else if (factSize >= 1024.00 && factSize < 1048576)
            m_strSize = (factSize / 1024.00).ToString("F2") + " KB";
        else if (factSize >= 1048576 && factSize < 1073741824)
            m_strSize = (factSize / 1024.00 / 1024.00).ToString("F2") + " MB";
        else if (factSize >= 1073741824)
            m_strSize = (factSize / 1024.00 / 1024.00 / 1024.00).ToString("F2") + " GB";
        return m_strSize;
    }

    #endregion

    #region 将现有文件的内容复制到新文件中

    /// <summary>
    /// 将源文件的内容复制到目标文件中.
    /// </summary>
    /// <param name="sourceFilePath">源文件的绝对路径.</param>
    /// <param name="destFilePath">目标文件的绝对路径.</param>
    public static void Copy(string sourceFilePath, string destFilePath)
    {
        File.Copy(sourceFilePath, destFilePath, true);
    }

    #endregion

    #region ReadAllBytes

    /// <summary>
    /// ReadAllBytes.
    /// </summary>
    /// <param name="path">path.</param>
    /// <returns></returns>
    public static byte[]? ReadAllBytes(string path)
    {
        try
        {
            return File.ReadAllBytes(path);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 读取全部字符串.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string? ReadAllStr(string path)
    {
        string line = string.Empty;
        FileStream fileStream = new FileStream(path, FileMode.Open);
        using (StreamReader reader = new StreamReader(fileStream))
        {
            line = reader.ReadLine();
        }

        return line;
    }

    #endregion

    #region 将文件读取到字符串中

    /// <summary>
    /// 将文件读取到字符串中.
    /// </summary>
    /// <param name="filePath">文件的绝对路径.</param>
    public static string FileToString(string filePath)
    {
        return FileToString(filePath, Encoding.UTF8);
    }

    /// <summary>
    /// 将文件读取到字符串中.
    /// </summary>
    /// <param name="filePath">文件的绝对路径.</param>
    /// <param name="encoding">字符编码</param>
    public static string FileToString(string filePath, Encoding encoding)
    {
        // 创建流读取器
        StreamReader reader = new StreamReader(filePath, encoding);
        try
        {
            // 读取流
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            // 关闭流读取器
            reader.Close();
        }
    }

    #endregion

    #region 生成高清晰缩略图

    /// <summary>
    /// 根据源图片生成高清晰缩略图.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="type"></param>
    /// <param name="ImgPath"></param>
    /// <returns></returns>
    public static bool MakeThumbnail(Stream stream, int width, int height, string type, string ImgPath)
    {
        if (stream.IsNotEmptyOrNull())
        {
            Image img = Image.Load(stream);
            int towidth = width;
            int toheight = height;
            int x = 0;
            int y = 0;
            int ow = img.Width;
            int oh = img.Height;
            if (ow <= towidth && oh <= height) return false;
            switch (type)
            {
                // 指定高宽压缩
                case "HW":
                    // 判断图形是什么形状
                    if ((double)img.Width / (double)img.Height > (double)width / (double)height)
                    {
                        towidth = width;
                        toheight = img.Height * width / img.Width;
                    }
                    else if ((double)img.Width / (double)img.Height == (double)width / (double)height)
                    {
                        towidth = width;
                        toheight = height;
                    }
                    else
                    {
                        toheight = height;
                        towidth = img.Width * height / img.Height;
                    }

                    break;
                case "W":
                    // 指定宽，高按比例
                    toheight = img.Height * width / img.Width;
                    break;
                case "H":
                    // 指定高，宽按比例
                    towidth = img.Width * height / img.Height;
                    break;
                case "Cut":
                    // 指定高宽裁减（不变形）
                    if ((double)img.Width / (double)img.Height > (double)towidth / (double)toheight)
                    {
                        oh = img.Height;
                        ow = img.Height * towidth / toheight;
                        x = (img.Width - ow) / 2;
                    }
                    else
                    {
                        ow = img.Width;
                        oh = img.Width * height / towidth;
                        y = (img.Height - oh) / 2;
                    }

                    break;
                case "DB":
                    // 按值较大的进行等比缩放（不变形）
                    if ((double)img.Width / (double)towidth < (double)img.Height / (double)toheight)
                    {
                        toheight = height;
                        towidth = img.Width * height / img.Height;
                    }
                    else
                    {
                        towidth = width;
                        toheight = img.Height * width / img.Width;
                    }
                    break;
            }

            img.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(towidth, toheight), Mode = ResizeMode.Max }));
            img.Save(ImgPath);
            return true;
        }
        return false;
    }

    #endregion

    #region 获取文件类型

    /// <summary>
    /// 获取文件类型.
    /// </summary>
    /// <param name="file">文件</param>
    /// <returns></returns>
    public static string? GetFileType(FileInfo file)
    {
        if (file.Exists)
        {
            string fileName = file.Name;
            return fileName.Substring(fileName.LastIndexOf(".") + 1);
        }

        return string.Empty;
    }

    #endregion

    #region  将文件路径转为内存流

    /// <summary>
    /// 将文件路径转为内存流.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static MemoryStream FileToStream(string fileName)
    {
        // 打开文件
        FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

        // 读取文件的 byte[]
        byte[] bytes = new byte[fileStream.Length];

        fileStream.Read(bytes, 0, bytes.Length);

        fileStream.Close();

        // 把 byte[] 转换成 Stream
        return new MemoryStream(bytes);
    }

    #endregion

    #region 根据文件大小获取指定前缀的可用文件名

    /// <summary>
    /// 根据文件大小获取指定前缀的可用文件名.
    /// </summary>
    /// <param name="folderPath">文件夹.</param>
    /// <param name="prefix">文件前缀.</param>
    /// <param name="size">文件大小(1m).</param>
    /// <param name="ext">文件后缀(.log).</param>
    /// <returns>可用文件名.</returns>
    public static string GetAvailableFileWithPrefixOrderSize(string folderPath, string prefix, int size = 1 * 1024 * 1024, string ext = ".log")
    {
        DirectoryInfo allFiles = new DirectoryInfo(folderPath);
        List<FileInfo> selectFiles = allFiles.GetFiles().Where(fi => fi.Name.ToLower().Contains(prefix.ToLower()) && fi.Extension.ToLower() == ext.ToLower() && fi.Length < size).OrderByDescending(d => d.Name).ToList();

        if (selectFiles.Count > 0)
        {
            return selectFiles.FirstOrDefault().FullName;
        }

        return Path.Combine(folderPath, $@"{prefix}_{DateTime.Now.ParseToUnixTime()}.log");
    }

    #endregion

    #region 文件下载

    /// <summary>
    /// 普通下载.
    /// </summary>
    /// <param name="filePath">路径.</param>
    /// <param name="fileName">文件名.</param>
    public static void DownloadFile(string filePath, string fileName)
    {
        try
        {
            if (File.Exists(filePath))
            {
                byte[]? buff = ReadAllBytes(filePath);
                Microsoft.AspNetCore.Http.HttpContext? httpContext = App.HttpContext;
                httpContext.Response.ContentType = "application/octet-stream";
                httpContext.Response.Headers.Add("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(fileName, Encoding.UTF8));
                httpContext.Response.Headers.Add("Content-Length", buff.Length.ToString());
                httpContext.Response.Body.WriteAsync(buff);
                httpContext.Response.Body.Flush();
                httpContext.Response.Body.Close();
            }
        }
        catch (Exception)
        {

        }
    }

    /// <summary>
    /// 普通下载.
    /// </summary>
    /// <param name="buffer">文件流.</param>
    /// <param name="fileName">文件名.</param>
    public static void DownloadFile(byte[] buffer, string fileName)
    {
        Microsoft.AspNetCore.Http.HttpContext? httpContext = App.HttpContext;
        httpContext.Response.ContentType = "application/octet-stream";
        httpContext.Response.Headers.Add("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(fileName, Encoding.UTF8));
        httpContext.Response.Body.Write(buffer);
        httpContext.Response.Body.Flush();
        httpContext.Response.Body.Close();
    }

    #endregion

    #region 附件处理

    /// <summary>
    /// 添加附件：将临时文件夹的文件拷贝到正式文件夹里面.
    /// </summary>
    /// <param name="data"></param>
    public static void CreateFile(List<AnnexModel> data)
    {
        if (data != null && data.Count > 0)
        {
            string temporaryFilePath = Path.Combine(KeyVariable.SystemPath, "TemporaryFile");
            string systemFilePath = KeyVariable.SystemPath;
            foreach (AnnexModel item in data)
            {
                MoveFile(temporaryFilePath + item.FileId, systemFilePath + item.FileId);
            }
        }
    }

    /// <summary>
    /// 更新附件.
    /// </summary>
    /// <param name="data"></param>
    public static void UpdateFile(List<AnnexModel> data)
    {
        if (data != null)
        {
            string temporaryFilePath = Path.Combine(KeyVariable.SystemPath, "TemporaryFile");
            string systemFilePath = KeyVariable.SystemPath;
            foreach (AnnexModel item in data)
            {
                if (item.FileType == "add")
                {
                    MoveFile(temporaryFilePath + item.FileId, systemFilePath + item.FileId);
                }
                else if (item.FileType == "delete")
                {
                    DeleteFile(systemFilePath + item.FileId);
                }
            }
        }
    }

    /// <summary>
    /// 删除附件.
    /// </summary>
    /// <param name="data"></param>
    public static void DeleteFile(List<AnnexModel> data)
    {
        if (data != null && data.Count > 0)
        {
            string systemFilePath = KeyVariable.SystemPath;
            foreach (AnnexModel item in data)
            {
                DeleteFile(systemFilePath + item.FileId);
            }
        }
    }

    #endregion
}