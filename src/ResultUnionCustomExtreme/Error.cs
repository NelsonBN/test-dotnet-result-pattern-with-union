namespace ResultUnionCustomExtreme;

public readonly struct Error(string description)
{
    public string Description { get; init; } = description;
}
