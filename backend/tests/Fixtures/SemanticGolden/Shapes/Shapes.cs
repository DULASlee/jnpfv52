namespace SemanticGolden.Shapes;

public class ShapeUser
{
    public string Name { get; set; } = string.Empty;
}

public class ShapeHolder
{
    public string Name { get; set; } = string.Empty;

    public string? MaybeName { get; set; }

    public System.Collections.Generic.List<ShapeUser> Users { get; } = new();

    public System.Collections.Generic.List<ShapeUser?> MaybeUsers { get; } = new();

    public int[,] Matrix { get; } = new int[2, 2];
}

public class ShapeRepository<T>
    where T : class, new()
{
    public T Create() => new T();
}
