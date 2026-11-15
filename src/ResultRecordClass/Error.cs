namespace ResultRecordClass;

public readonly struct Error
{
    public string Description { get; init; }

    public Error(string description)
    {
        Description = description;
    }
}
