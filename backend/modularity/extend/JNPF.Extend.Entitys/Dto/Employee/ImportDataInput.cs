using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.Employee;

[SuppressSniffer]
public class ImportDataInput
{
    /// <summary>
    /// 导入数据.
    /// </summary>
    public List<EmployeeListOutput>? list { get; set; }
}
