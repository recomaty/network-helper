# R3Polska.Networking

[![CI](https://github.com/recomaty/network-helper/actions/workflows/ci.yml/badge.svg)](https://github.com/recomaty/network-helper/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/recomaty/network-helper/graph/badge.svg?token=NVX42DED6L)](https://codecov.io/gh/recomaty/network-helper)
[![NuGet](https://img.shields.io/nuget/v/R3Polska.Networking.svg)](https://www.nuget.org/packages/R3Polska.Networking/)
[![License](https://img.shields.io/badge/License-BSD_3--Clause-blue.svg)](./LICENSE)

Network helper utilities for retrieving MAC addresses and local IP addresses with support for Docker containerized environments.

## Features

- **MAC Address Retrieval**: Get hardware MAC addresses from system or file-based sources
- **Docker Volume Support**: Prioritizes `/hw` folder paths for consistent hardware identification in containers
- **Local IP Detection**: Determine which local IP address would be used to reach a target network
- **Cross-Platform**: Works on Linux and Windows systems

## Installation

```bash
dotnet add package R3Polska.Networking
```

## Usage

### Get MAC Address

```csharp
using R3Polska.Networking;

// Get MAC address (checks Docker volumes first, then system)
string macAddress = NetworkHelper.GetRealMacAddress();

// Use custom paths
string macAddress = NetworkHelper.GetRealMacAddress("/custom/path/to/mac");
```

The method searches for MAC addresses in the following order:
1. Docker-mapped volumes under `/hw/class/net/*/address`
2. System network interfaces under `/sys/class/net/*/address`
3. System API fallback (Windows or when files don't exist)

### Get Local IP for Network

```csharp
using R3Polska.Networking;

// Get local IP that would be used to reach Google DNS
string localIp = NetworkHelper.GetLocalIpForNetwork("8.8.8.8");

// Use default target (10.8.0.1)
string localIp = NetworkHelper.GetLocalIpForNetwork();
```

This emulates `ip route get` to determine routing without sending network traffic.

## Docker Support

To ensure consistent MAC address retrieval in Docker containers, mount the host's network interface info:

```yaml
volumes:
  - /sys/class/net:/hw/class/net:ro
```

The library will prioritize reading from `/hw/class/net` paths when available.

## Building

```bash
# Run tests
make test

# Generate coverage report
make coverage-html

# Create NuGet package
make pack
```

## License

MIT
