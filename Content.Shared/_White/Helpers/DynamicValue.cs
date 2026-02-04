namespace Content.Shared._White.Helpers;

public sealed class DynamicValue(string type, object value)
{
    [ViewVariables]
    public string Type { get; } = type;

    [ViewVariables]
    public object Value { get; } = value;
}
