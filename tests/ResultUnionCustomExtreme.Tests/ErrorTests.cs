namespace ResultUnionCustomExtreme.Tests;

public sealed class ErrorTests
{
    [Fact]
    public void Given_Description_When_Constructed_Then_DescriptionIsSet()
    {
        // Act
        var error = new Error("boom");

        // Assert
        error.Description.Should().Be("boom");
    }

    [Fact]
    public void Given_ObjectInitializer_When_Constructed_Then_DescriptionIsReplaced()
    {
        // Act
        var error = new Error("boom") { Description = "other" };

        // Assert
        error.Description.Should().Be("other");
    }

    [Fact]
    public void Given_EqualDescriptions_When_Compared_Then_ErrorsAreEqual()
    {
        // Arrange
        var left = new Error("boom");
        var right = new Error("boom");

        // Act
        var equal = left.Equals(right);

        // Assert
        equal.Should().BeTrue();
    }
}
