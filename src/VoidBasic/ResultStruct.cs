using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

public struct ResultStruct
{
    public bool IsSuccess { get; private set; }

    private readonly Error[]? _errors;
    public IReadOnlyList<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors ?? [];

    public ResultStruct()
    {
        _errors = default;
        IsSuccess = true;
    }

    public ResultStruct(Error error)
    {
        _errors = [error];
        IsSuccess = false;
    }
}
