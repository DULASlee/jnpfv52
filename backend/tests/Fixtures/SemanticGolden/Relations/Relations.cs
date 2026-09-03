namespace SemanticGolden.Relations;

public interface IUser
{
    string Name { get; }
}

public class BaseAccount
{
    public virtual string Describe() => "base";
}

public class AdminUser : BaseAccount, IUser
{
    public string Name { get; set; } = string.Empty;

    string IUser.Name => "explicit:" + Name;

    public override string Describe() => "admin";
}
