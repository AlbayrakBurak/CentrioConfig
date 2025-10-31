using Configuration.Core.Typing;
using Configuration.Library;
using Xunit;

namespace Configuration.Library.Tests;

public class TypeConverterTests
{
    private readonly ITypeConverter _converter = new InvariantTypeConverter();

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("123", "123")]
    [InlineData("", "")]
    public void TryConvert_ToString_ShouldSucceed(string input, string expected)
    {
        // Act
        var result = _converter.TryConvert(input, typeof(string), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("42", 42)]
    [InlineData("0", 0)]
    [InlineData("-100", -100)]
    public void TryConvert_ToInt_ShouldSucceed(string input, int expected)
    {
        // Act
        var result = _converter.TryConvert(input, typeof(int), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.5")]
    [InlineData("")]
    public void TryConvert_InvalidInt_ShouldReturnFalse(string input)
    {
        // Act
        var result = _converter.TryConvert(input, typeof(int), out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Theory]
    [InlineData("3.14", 3.14)]
    [InlineData("0.5", 0.5)]
    [InlineData("-10.25", -10.25)]
    public void TryConvert_ToDouble_ShouldSucceed(string input, double expected)
    {
        // Act
        var result = _converter.TryConvert(input, typeof(double), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void TryConvert_ToBool_ShouldSucceed(string input, bool expected)
    {
        // Act
        var result = _converter.TryConvert(input, typeof(bool), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("2")]
    [InlineData("")]
    public void TryConvert_InvalidBool_ShouldReturnFalse(string input)
    {
        // Act
        var result = _converter.TryConvert(input, typeof(bool), out var value);

        // Assert
        // "abc", "2", "" are invalid boolean values
        if (input != "0" && input != "1")
        {
            Assert.False(result);
        }
    }
}
