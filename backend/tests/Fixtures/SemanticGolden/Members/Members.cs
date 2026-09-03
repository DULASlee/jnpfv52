namespace SemanticGolden.Members;

public interface IHasEvent
{
    event EventHandler Ticked;
}

public class MemberHolder : IHasEvent
{
    public string Name;

    public static int Counter;

    public readonly string Tag = "t";

    public event EventHandler? Changed;

    public static event EventHandler? StaticChanged;

    event EventHandler IHasEvent.Ticked
    {
        add { }
        remove { }
    }

    public string Label { get; set; } = string.Empty;

    public void Touch() { }

    public MemberHolder()
    {
        Name = string.Empty;
    }
}
