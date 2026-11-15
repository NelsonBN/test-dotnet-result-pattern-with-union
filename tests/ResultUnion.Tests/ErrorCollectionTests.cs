namespace ResultUnion.Tests;

// ErrorCollection is internal: it is exercised through the public Result.From(IEnumerable<Error>).
public sealed class ErrorCollectionTests
{
    private static readonly Error _first = new("first");
    private static readonly Error _second = new("second");

    [Fact]
    public void Given_Array_When_Created_Then_ErrorsAreCopied()
    {
        // Arrange
        Error[] array = [_first, _second];
        IEnumerable<Error> errors = array;

        // Act
        var result = Result.From(errors);

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_List_When_Created_Then_ErrorsAreCopied()
    {
        // Arrange
        List<Error> list = [_first, _second];
        IEnumerable<Error> errors = list;

        // Act
        var result = Result.From(errors);

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }

    [Fact]
    public void Given_SequenceWithoutKnownCount_When_Created_Then_ErrorsAreEnumerated()
    {
        // Arrange
        var errors = Enumerate();

        // Act
        var result = Result.From(errors);

        // Assert
        result.Errors.Should().Equal(_first, _second);

        static IEnumerable<Error> Enumerate()
        {
            yield return _first;
            yield return _second;
        }
    }

    [Fact]
    public void Given_EmptyCountedSequence_When_Created_Then_ErrorsAreEmpty()
    {
        // Arrange
        IEnumerable<Error> errors = new Queue<Error>();

        // Act
        var result = Result.From(errors);

        // Assert
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Given_CountedSequence_When_Created_Then_ErrorsAreCopiedInOrder()
    {
        // Arrange
        IEnumerable<Error> errors = new Queue<Error>([_first, _second]);

        // Act
        var result = Result.From(errors);

        // Assert
        result.Errors.Should().Equal(_first, _second);
    }
}
