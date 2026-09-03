namespace SemanticGolden.Operations;

public class OpService
{
    public string Name { get; set; } = string.Empty;

    public string this[int index] => Name;

    public OpService() { }

    public OpService(string name)
    {
        Name = name;
    }

    public string Combine(string other) => Name + other;

    public string Combine(string other, string separator) => Name + separator + other;

    public string Join(params string[] parts) => string.Concat(parts);

    public string Describe(string name = "default") => name;

    public bool TryGet(int index, out string value)
    {
        value = Name;
        return true;
    }

    public void Add(ref int counter)
    {
        counter++;
    }

    public T Echo<T>(T value) => value;
}

public static class OpExtensions
{
    public static int WordCount(this string text) => text.Split(' ').Length;
}
