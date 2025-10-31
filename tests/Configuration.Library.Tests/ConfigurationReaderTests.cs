using System;
using System.Threading.Tasks;
using Configuration.Core.Models;
using Configuration.Library;
using Configuration.Library.Tests.Mocks;
using Xunit;

namespace Configuration.Library.Tests;

public class ConfigurationReaderTests : IAsyncDisposable
{
    private ConfigurationReader? _reader;

    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() =>
        {
            // Note: ConfigurationReader constructor creates its own repository
            // This test just validates constructor signature
            var _ = new ConfigurationReader("TEST-APP", "mongodb://localhost:27017", 1000);
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithEmptyApplicationName_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            var _ = new ConfigurationReader("", "mongodb://localhost:27017", 1000);
        });
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            var _ = new ConfigurationReader("TEST-APP", "", 1000);
        });
    }

    [Fact]
    public async Task GetValue_WhenKeyNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _reader = new ConfigurationReader("TEST-APP", "mongodb://localhost:27017", 1000);
        await Task.Delay(2000);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() =>
        {
            _reader!.GetValue<string>("NonExistentKey");
        });

        await DisposeAsync();
    }

    [Fact]
    public async Task TryGetValue_WhenKeyNotFound_ShouldReturnFalse()
    {
        // Arrange
        _reader = new ConfigurationReader("TEST-APP", "mongodb://localhost:27017", 1000);
        await Task.Delay(2000);

        // Act
        var result = _reader!.TryGetValue<string>("NonExistentKey", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);

        await DisposeAsync();
    }

    [Fact]
    public async Task TryGetValue_WithEmptyKey_ShouldReturnFalse()
    {
        // Arrange
        _reader = new ConfigurationReader("TEST-APP", "mongodb://localhost:27017", 1000);
        await Task.Delay(2000);

        // Act
        var result = _reader!.TryGetValue<string>("", out var value);

        // Assert
        Assert.False(result);
    }

    private async Task<bool> IsMongoAvailable()
    {
        try
        {
            var client = new MongoDB.Driver.MongoClient("mongodb://localhost:27017");
            await client.StartSessionAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_reader != null)
        {
            await _reader.DisposeAsync();
        }
    }
}
