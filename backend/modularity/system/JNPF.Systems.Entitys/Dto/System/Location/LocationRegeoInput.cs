using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.Location;

[SuppressSniffer]
public class LocationRegeoInput
{
    public string key { get; set; }

    public string location { get; set; }
}