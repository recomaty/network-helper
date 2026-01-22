using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace R3.Networking;
/// <summary>
/// Próbuje pobrać adres MAC urządzenia w celu identyfikacji sprzętowej.
/// Uwaga: Priorytetyzuje ścieżki folderu `/hw`, aby umożliwić mapowanie/przypisanie przez woluminy Dockera.
/// Umożliwia to spójne pobieranie ID sprzętowego w środowiskach kontenerowych.
/// </summary>
public static class NetworkHelper
{
    /// <summary>
    /// Zapewnia mechanizm zastępczy do pobierania adresu MAC przy użyciu API systemu.
    /// Ta metoda jest szczególnie przydatna w systemach Windows lub gdy pobieranie
    /// z pliku nie powiedzie się. Przeszukuje wszystkie interfejsy sieciowe i zwraca
    /// adres MAC pierwszego aktywnego interfejsu fizycznego.
    /// </summary>
    /// <returns>Ciąg znaków z adresem MAC jeśli znaleziono, null jeśli brak odpowiedniego interfejsu</returns>
    private static string? GetMacFromSystem()
    {
        // Iteruj przez wszystkie interfejsy sieciowe dostępne w systemie
        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Rozważ tylko interfejsy, które są obecnie operacyjne
            // Wyklucz interfejsy wirtualne i pseudo, aby uzyskać prawdziwe adresy MAC sprzętowe
            if (nic.OperationalStatus != OperationalStatus.Up ||
                (nic.Description.Contains("Virtual") || nic.Description.Contains("Pseudo"))) continue;
            // Sprawdź, czy interfejs ma prawidłowy adres fizyczny
            if (nic.GetPhysicalAddress().ToString() != "")
            {
                return nic.GetPhysicalAddress().ToString();
            }
        }

        // Zwróć null jeśli nie znaleziono odpowiedniego interfejsu
        return null;
    }


    /// <summary>
    /// Pobiera adres MAC z systemu przy użyciu podejścia hierarchicznego.
    /// Najpierw próbuje odczytać z predefiniowanych ścieżek plików (priorytetyzując woluminy mapowane przez Dockera),
    /// następnie wraca do API systemu, jeśli pobieranie z pliku nie powiedzie się.
    /// </summary>
    /// <returns>Ciąg znaków z adresem MAC</returns>
    /// <exception cref="Exception">Zgłaszany, gdy adres MAC nie może zostać pobrany z żadnego źródła</exception>
    public static string GetRealMacAddress(params string[] overridePaths)
    {
        // Zdefiniuj ścieżki uporządkowane według priorytetu do pobierania adresu MAC
        // Ścieżki /hw/ są przeznaczone do scenariuszy mapowania woluminów Dockera
        // Ścieżki /sys/ to standardowe lokalizacje interfejsów sieciowych Linuksa
        var paths = overridePaths.Length > 0 ? overridePaths :
        [
            "/hw/class/net/enp1s0/address",    // Interfejs Ethernet 1 mapowany przez Dockera, nowy format
            "/hw/class/net/eno1/address",    // Interfejs Ethernet 1 mapowany przez Dockera
            "/hw/class/net/eth0/address",    // Interfejs Ethernet 0 mapowany przez Dockera
            "/hw/class/net/eth1/address",    // Interfejs Ethernet 1 mapowany przez Dockera (alternatywny)
            "/sys/class/net/enp1s0/address",   // Systemowy interfejs Ethernet 1, nowy format
            "/sys/class/net/eno1/address",   // Systemowy interfejs Ethernet 1
            "/sys/class/net/eth0/address",   // Systemowy interfejs Ethernet 0
            "/sys/class/net/eth1/address" // Systemowy interfejs Ethernet 1 (alternatywny)
        ];

        // Próbuj odczytać adres MAC z każdej ścieżki pliku po kolei
        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            using var reader = new StreamReader(path);
            // Odczytaj adres MAC i usuń wszelkie białe znaki/nowe linie
            var macAddress = reader.ReadToEnd().Trim();

            return macAddress;
        }

        // Jeśli pobieranie z pliku nie powiedzie się, spróbuj zastępczego API systemu
        var mac = GetMacFromSystem();
        return mac ?? throw
            // Zgłoś wyjątek, jeśli wszystkie metody pobierania zawiodą
            new Exception("Nie można pobrać identyfikatora adresu MAC. Nie można kontynuować operacji.");
    }
    
    /// <summary>
    /// Emuluje ip route get w celu uzyskania adresu IP interfejsu docelowego. Nie wysyła żadnych danych przez sieć.
    /// </summary>
    /// <param name="targetIp">Adres IP, z którego sprawdzić</param>
    /// <returns></returns>
    public static string? GetLocalIpForNetwork(string targetIp = "10.8.0.1")
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect(targetIp, 1);
        var localEndPoint = socket.LocalEndPoint as IPEndPoint;
        return localEndPoint?.Address.ToString();
    }
}