using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

public readonly struct ResultReadonlyStruct
{
    public readonly bool IsSuccess;

    private readonly Error[]? _errors;
    public IReadOnlyList<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors ?? [];

    public ResultReadonlyStruct()
    {
        _errors = default;
        IsSuccess = true;
    }

    public ResultReadonlyStruct(Error error)
    {
        _errors = [error];
        IsSuccess = false;
    }
}
