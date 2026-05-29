using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.Location;

[SuppressSniffer]
public class LocationStaticmap
{
    public string key { get; set; }

    public string location { get; set; }

    public string size { get; set; }

    public int zoom { get; set; }
}