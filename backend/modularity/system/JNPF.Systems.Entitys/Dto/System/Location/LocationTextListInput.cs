using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.Location;

[SuppressSniffer]
public class LocationTextListInput
{
    public string key { get; set; }

    public string keywords { get; set; }

    public int radius { get; set; }

    public int offset { get; set; }

    public int page { get; set; }
}