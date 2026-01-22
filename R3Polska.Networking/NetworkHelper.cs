using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace R3Polska.Networking;
/// <summary>
/// Provides methods to retrieve device hardware identifiers for identification purposes.
/// Note: Prioritizes `/hw` folder paths to enable mapping/assignment through Docker volumes,
/// enabling consistent hardware ID retrieval in containerized environments.
/// </summary>
public static class NetworkHelper
{
    /// <summary>
    /// Provider for network interfaces. Can be replaced in tests for mocking.
    /// </summary>
    internal static Func<NetworkInterface[]> NetworkInterfaceProvider { get; set; } = 
        NetworkInterface.GetAllNetworkInterfaces;

    /// <summary>
    /// Provides a fallback mechanism to retrieve the MAC address using the system API.
    /// This method is particularly useful on Windows systems or when file-based retrieval fails.
    /// It scans all network interfaces and returns the MAC address of the first active physical interface.
    /// </summary>
    /// <returns>A string containing the MAC address if found; null if no suitable interface is available.</returns>
    internal static string? GetMacFromSystem()
    {
        // Iterate through all network interfaces available on the system
        foreach (NetworkInterface nic in NetworkInterfaceProvider())
        {
            // Consider only interfaces that are currently operational
            // Exclude virtual and pseudo interfaces to get genuine hardware MAC addresses
            if (nic.OperationalStatus != OperationalStatus.Up ||
                (nic.Description.Contains("Virtual") || nic.Description.Contains("Pseudo"))) continue;
            // Check if the interface has a valid physical address
            if (nic.GetPhysicalAddress().ToString() != "")
            {
                return nic.GetPhysicalAddress().ToString();
            }
        }

        // Return null if no suitable interface was found
        return null;
    }


    /// <summary>
    /// Retrieves the MAC address from the system using a hierarchical approach.
    /// First attempts to read from predefined file paths (prioritizing Docker-mapped volumes),
    /// then falls back to the system API if file-based retrieval fails.
    /// </summary>
    /// <param name="overridePaths">Optional custom file paths to check for MAC address. If not provided, uses default paths.</param>
    /// <returns>A string containing the MAC address.</returns>
    /// <exception cref="Exception">Thrown when the MAC address cannot be retrieved from any source.</exception>
    public static string GetRealMacAddress(params string[] overridePaths)
    {
        // Define paths ordered by priority for MAC address retrieval
        // /hw/ paths are intended for Docker volume mapping scenarios
        // /sys/ paths are standard Linux network interface locations
        var paths = overridePaths.Length > 0 ? overridePaths :
        [
            "/hw/class/net/enp1s0/address",    // Docker-mapped Ethernet interface 1, new naming format
            "/hw/class/net/eno1/address",      // Docker-mapped Ethernet interface 1
            "/hw/class/net/eth0/address",      // Docker-mapped Ethernet interface 0
            "/hw/class/net/eth1/address",      // Docker-mapped Ethernet interface 1 (alternative)
            "/sys/class/net/enp1s0/address",   // System Ethernet interface 1, new naming format
            "/sys/class/net/eno1/address",     // System Ethernet interface 1
            "/sys/class/net/eth0/address",     // System Ethernet interface 0
            "/sys/class/net/eth1/address"      // System Ethernet interface 1 (alternative)
        ];

        // Try reading the MAC address from each file path in sequence
        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            using var reader = new StreamReader(path);
            // Read the MAC address and remove any whitespace/newlines
            var macAddress = reader.ReadToEnd().Trim();

            return macAddress;
        }

        // If file-based retrieval fails, attempt fallback system API
        var mac = GetMacFromSystem();
        return mac ?? throw
            // Throw an exception if all retrieval methods fail
            new Exception("Unable to retrieve MAC address identifier. Cannot continue operation.");
    }
    
    /// <summary>
    /// Emulates 'ip route get' to determine the local IP address that would be used to reach the target IP.
    /// Does not send any data over the network - uses socket connection to determine routing.
    /// </summary>
    /// <param name="targetIp">The target IP address to check routing for. Defaults to a private network address.</param>
    /// <returns>The local IP address string that would be used to reach the target, or null if unable to determine.</returns>
    public static string? GetLocalIpForNetwork(string targetIp = "10.8.0.1")
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect(targetIp, 1);
        var localEndPoint = socket.LocalEndPoint as IPEndPoint;
        return localEndPoint?.Address.ToString();
    }
}