// Q2 violation probe — AG-001 REFERENCES Controller in generated backend tree
namespace JNPF.OaLeave.Entitys;

public static class ViolationProbe
{
    public const string BadDdl = "FOREIGN KEY (x) REFERENCES dbo.LeaveController (id)";
}
