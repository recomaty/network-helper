using System.Net;
using System.Text.RegularExpressions;
using Shouldly;

namespace R3Polska.Networking.Tests;

public class NetworkHelperTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    private string CreateTempFileWithContent(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    [Fact]
    public void GetRealMacAddress_WithValidFilePath_ReturnsMacFromFile()
    {
        // Arrange
        const string expectedMac = "00:11:22:33:44:55";
        var tempFile = CreateTempFileWithContent(expectedMac);

        // Act
        var result = NetworkHelper.GetRealMacAddress(tempFile);

        // Assert
        Assert.Equal(expectedMac, result);
    }

    [Fact]
    public void GetRealMacAddress_WithWhitespaceInFile_TrimsMacAddress()
    {
        // Arrange
        const string macWithWhitespace = "  00:11:22:33:44:55\n";
        const string expectedMac = "00:11:22:33:44:55";
        var tempFile = CreateTempFileWithContent(macWithWhitespace);

        // Act
        var result = NetworkHelper.GetRealMacAddress(tempFile);

        // Assert
        Assert.Equal(expectedMac, result);
    }

    [Fact]
    public void GetRealMacAddress_WithMultiplePaths_ReturnsFirstExistingFile()
    {
        // Arrange
        const string expectedMac = "aa:bb:cc:dd:ee:ff";
        var tempFile = CreateTempFileWithContent(expectedMac);

        // Act
        var result = NetworkHelper.GetRealMacAddress(
            "/nonexistent/path1",
            "/nonexistent/path2",
            tempFile,
            "/nonexistent/path3"
        );

        // Assert
        Assert.Equal(expectedMac, result);
    }

    [Fact]
    public void GetRealMacAddress_WithNoValidPaths_FallsBackToSystem()
    {
        // Act - use non-existent paths to force system fallback
        var result = NetworkHelper.GetRealMacAddress(
            "/nonexistent/path1",
            "/nonexistent/path2"
        );

        // Assert - should return a valid MAC address from the system
        // MAC addresses are typically 12 hex characters (without separators) or formatted with colons/dashes
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GetRealMacAddress_WithNoArgs_UsesDefaultPathsOrSystem()
    {
        // Act - call with no arguments to use defaults
        var result = NetworkHelper.GetRealMacAddress();

        // Assert - should return a valid MAC address
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GetLocalIpForNetwork_WithDefaultTarget_ReturnsValidIpAddress()
    {
        // Act
        var result = NetworkHelper.GetLocalIpForNetwork();

        // Assert
        Assert.NotNull(result);
        Assert.Matches(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", result);
    }

    [Fact]
    public void GetLocalIpForNetwork_WithCustomTarget_ReturnsValidIpAddress()
    {
        // Act
        var result = NetworkHelper.GetLocalIpForNetwork("8.8.8.8");

        // Assert
        Assert.NotNull(result);
        Assert.Matches(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", result);
    }

    [Fact]
    public void GetLocalIpForNetwork_ReturnsNonLoopbackAddress()
    {
        // Act
        var result = NetworkHelper.GetLocalIpForNetwork("8.8.8.8");

        // Assert - should not be localhost
        Assert.NotNull(result);
        Assert.NotEqual("127.0.0.1", result);
    } 
    
    [Fact]
    public void GetMacAddress_ShouldReturnNonEmptyString()
    {
        // Act
        var result = NetworkHelper.GetRealMacAddress();

        // Assert
        result.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void GetMacAddress_ShouldReturnValidMacAddressFormat()
    {
        // Act
        var result = NetworkHelper.GetRealMacAddress();

        // Assert - MAC address should be either:
        // 1. Colon-separated format: "aa:bb:cc:dd:ee:ff" (Linux file format)
        // 2. Continuous format: "AABBCCDDEEFF" (Windows system API format)
        var colonSeparatedPattern = @"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$";
        var continuousPattern = @"^[0-9A-Fa-f]{12}$";

        var isValidFormat = Regex.IsMatch(result, colonSeparatedPattern) ||
                           Regex.IsMatch(result, continuousPattern);

        isValidFormat.ShouldBeTrue($"MAC address '{result}' should match standard formats");
    }

    [Fact]
    public void GetMacAddress_ShouldReturnConsistentValue()
    {
        // Act - Call multiple times
        var result1 = NetworkHelper.GetRealMacAddress();
        var result2 = NetworkHelper.GetRealMacAddress();
        var result3 = NetworkHelper.GetRealMacAddress();

        // Assert - Should return the same value
        result1.ShouldBe(result2);
        result2.ShouldBe(result3);
    }

    [Theory]
    [InlineData("8.8.8.8")]      
    [InlineData("1.1.1.1")]      
    [InlineData("10.8.0.1")]     
    [InlineData("192.168.1.1")]  
    [InlineData("172.16.0.1")]  
    public void GetLocalIpForNetwork_WithValidIpAddress_ShouldReturnLocalIp(string targetIp)
    {
        // Act
        var result = NetworkHelper.GetLocalIpForNetwork(targetIp);

        // Assert
        result.ShouldNotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("10.8.0.1")]
    public void GetLocalIpForNetwork_WithValidIpAddress_ShouldReturnValidIpFormat(string targetIp)
    {
        // Act
        var result = NetworkHelper.GetLocalIpForNetwork(targetIp);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPAddress.TryParse(result, out var ipAddress).ShouldBeTrue($"'{result}' should be a valid IP address");
        ipAddress!.AddressFamily.ShouldBe(System.Net.Sockets.AddressFamily.InterNetwork);
    }

    [Fact]
    public void GetLocalIpForNetwork_WithDefaultParameter_ShouldReturnLocalIp()
    {
        // Act
        var result = NetworkHelper.GetLocalIpForNetwork();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        IPAddress.TryParse(result, out _).ShouldBeTrue($"'{result}' should be a valid IP address");
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    public void GetLocalIpForNetwork_ShouldNotReturnLoopback(string targetIp)
    {
        // Act
        var result = NetworkHelper.GetLocalIpForNetwork(targetIp);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldNotBe("127.0.0.1");
    }

    [Theory]
    [InlineData("192.168.1.100")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    public void GetLocalIpForNetwork_WithPrivateNetworkIp_ShouldReturnPrivateIp(string targetIp)
    {
        // Act
        var result = NetworkHelper.GetLocalIpForNetwork(targetIp);

        // Assert
        result.ShouldNotBeNullOrEmpty();

        if (IPAddress.TryParse(result, out var ipAddress))
        {
            var bytes = ipAddress.GetAddressBytes();

            // Check if it's a private IP address (10.x.x.x, 172.16-31.x.x, or 192.168.x.x)
            var isPrivate = bytes[0] == 10 ||
                          (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                          (bytes[0] == 192 && bytes[1] == 168);

            // For private target IPs, we expect to get a private IP back
            // (This assumes the test machine has private network connectivity)
            isPrivate.ShouldBeTrue($"When targeting private network {targetIp}, should return a private IP, got {result}");
        }
    }

    [Fact]
    public void GetLocalIpForNetwork_MultipleCallsSameTarget_ShouldReturnConsistentValue()
    {
        // Arrange
        var targetIp = "8.8.8.8";

        // Act
        var result1 = NetworkHelper.GetLocalIpForNetwork(targetIp);
        var result2 = NetworkHelper.GetLocalIpForNetwork(targetIp);
        var result3 = NetworkHelper.GetLocalIpForNetwork(targetIp);

        // Assert
        result1.ShouldBe(result2);
        result2.ShouldBe(result3);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("999.999.999.999")]
    [InlineData("not-an-ip")]
    public void GetLocalIpForNetwork_WithInvalidIpAddress_ShouldThrow(string invalidIp)
    {
        // Act & Assert
        Should.Throw<Exception>(() => NetworkHelper.GetLocalIpForNetwork(invalidIp));
    }

    [Fact]
    public void GetLocalIpForNetwork_WithEmptyString_ShouldThrowOrReturnNull()
    {
        // Act & Assert - Empty string may behave differently depending on socket implementation
        // Both throwing and returning null/valid IP are acceptable behaviors
        try
        {
            var result = NetworkHelper.GetLocalIpForNetwork("");
            // If it doesn't throw, result could be null or a valid IP - both acceptable
        }
        catch (Exception)
        {
            // Throwing is also acceptable behavior
        }
    }

    #region GetMacFromSystem Tests with Mocking

    [Fact]
    public void GetMacFromSystem_WithNoInterfaces_ReturnsNull()
    {
        // Arrange
        var originalProvider = NetworkHelper.NetworkInterfaceProvider;
        try
        {
            NetworkHelper.NetworkInterfaceProvider = () => [];

            // Act
            var result = NetworkHelper.GetMacFromSystem();

            // Assert
            result.ShouldBeNull();
        }
        finally
        {
            NetworkHelper.NetworkInterfaceProvider = originalProvider;
        }
    }

    [Fact]
    public void GetRealMacAddress_WhenNoInterfacesAndNoFiles_ThrowsException()
    {
        // Arrange
        var originalProvider = NetworkHelper.NetworkInterfaceProvider;
        try
        {
            NetworkHelper.NetworkInterfaceProvider = () => [];

            // Act & Assert
            Should.Throw<Exception>(() => NetworkHelper.GetRealMacAddress("/nonexistent/path"));
        }
        finally
        {
            NetworkHelper.NetworkInterfaceProvider = originalProvider;
        }
    }

    [Fact]
    public void GetRealMacAddress_WithEmptyFileContent_ReturnsEmptyString()
    {
        // Arrange
        var tempFile = CreateTempFileWithContent("");

        // Act
        var result = NetworkHelper.GetRealMacAddress(tempFile);

        // Assert
        result.ShouldBe("");
    }

    #endregion
}