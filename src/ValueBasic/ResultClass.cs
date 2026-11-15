using ResultWithUnion.Common;

namespace ResultWithUnion.ValueBasic;

public sealed class ResultClass<TValue>
{
    public bool IsSuccess { get; private set; }

    private readonly TValue? _value;
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Value can be retrieved only when the result is not an error");

    private readonly Error[]? _errors;
    public IReadOnlyList<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors ?? [];

    public ResultClass(TValue value)
    {
        _value = value;
        _errors = default;
        IsSuccess = true;
    }

    public ResultClass(Error error)
    {
        _value = default;
        _errors = [error];
        IsSuccess = false;
    }
}
