using ResultWithUnion.Common;

namespace ResultWithUnion.VoidBasic;

public sealed record class ResultRecordClass
{
    public bool IsSuccess { get; private set; }

    private readonly Error[]? _errors;
    public IReadOnlyList<Error> Errors => IsSuccess
        ? throw new InvalidOperationException("Errors can be retrieved only when the result is an error")
        : _errors ?? [];

    public ResultRecordClass()
    {
        _errors = default;
        IsSuccess = true;
    }

    public ResultRecordClass(Error error)
    {
        _errors = [error];
        IsSuccess = false;
    }
}
