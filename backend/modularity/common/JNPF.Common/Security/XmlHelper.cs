using System.Xml.Serialization;

namespace JNPF.Common.Security;

public static class XmlHelper
{
    /// <summary>
    /// 反序列化.
    /// </summary>
    /// <param name="type">类型.</param>
    /// <param name="xml">XML字符串.</param>
    /// <returns></returns> 
    public static object Deserialize(Type type, string xml)
    {
        using (StringReader sr = new StringReader(xml))
        {
            XmlSerializer xmldes = new XmlSerializer(type);
            return xmldes.Deserialize(sr);
        }
    }

    /// <summary>
    /// 反序列化.
    /// </summary>
    /// <param name="type">类型.</param>
    /// <param name="stream">XML流.</param>
    /// <returns></returns>
    public static object Deserialize(Type type, Stream stream)
    {
        XmlSerializer xmldes = new XmlSerializer(type);
        return xmldes.Deserialize(stream);
    }
}
