namespace SemanticGolden.FirstSlice;

public interface IUser
{
    string Name { get; }
}

public class User : IUser
{
    public string Name { get; set; } = string.Empty;

    public int Age;

    public event EventHandler? Changed;

    string IUser.Name => "explicit:" + Name;

    public void Create(string name, int age = 0)
    {
        Name = name;
        Age = age;
    }

    public User()
    {
    }
}
