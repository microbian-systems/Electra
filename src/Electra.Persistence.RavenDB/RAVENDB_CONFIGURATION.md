# RavenDB Configuration Guide

This document explains how to configure RavenDB persistence in your application using the `RavenDbExtensions` class.

## Configuration Overview

The RavenDB configuration is read from `appsettings.json` under the `RavenDb` section. You can configure whether to use a RavenDB server or embedded mode.

## Configuration Settings

### RavenDb Section

```json
{
  "RavenDb": {
    "UseEmbedded": false,
    "EmbeddedPath": null,
    "Urls": "http://localhost:8080",
    "DatabaseName": "ElectraDb"
  }
}
```

#### Properties:

- **UseEmbedded** (boolean): Whether to use embedded RavenDB mode
  - Default: `false`
  - Note: Requires `RavenDB.Embedded` NuGet package to be installed
  
- **EmbeddedPath** (string): Path where embedded database files are stored
  - Default: `null` (uses default RavenDB embedded data directory)
  - Only used when `UseEmbedded` is `true`
  - Example: `"./Data/RavenDB"`
  
- **Urls** (string): RavenDB server URL(s)
  - Default: `"http://localhost:8080"`
  - Supports multiple URLs separated by commas for cluster setups
  - Example: `"http://localhost:8080,http://localhost:8081,http://localhost:8082"`
  - Ignored when `UseEmbedded` is `true`
  
- **DatabaseName** (string): Name of the database to use
  - Default: `"ElectraDb"`
  - Must be a valid RavenDB database name

## Usage Examples

### Example 1: Local RavenDB Server (Default)

```json
{
  "RavenDb": {
    "UseEmbedded": false,
    "Urls": "http://localhost:8080",
    "DatabaseName": "MyAppDb"
  }
}
```

### Example 2: RavenDB Cloud (Free Tier)

```json
{
  "RavenDb": {
    "UseEmbedded": false,
    "Urls": "https://a.free.trinity.ravendb.cloud",
    "DatabaseName": "MyAppDb"
  }
}
```

### Example 3: RavenDB Cluster

```json
{
  "RavenDb": {
    "UseEmbedded": false,
    "Urls": "http://node1:8080,http://node2:8080,http://node3:8080",
    "DatabaseName": "MyAppDb"
  }
}
```

### Example 4: Embedded Mode (Development)

To use embedded mode, first install the RavenDB.Embedded NuGet package:

```bash
dotnet add package RavenDB.Embedded
```

Then configure:

```json
{
  "RavenDb": {
    "UseEmbedded": true,
    "EmbeddedPath": "./Data/RavenDB",
    "DatabaseName": "MyAppDb"
  }
}
```

## Registering in Dependency Injection

In your `Program.cs`, register the RavenDB persistence:

```csharp
var builder = WebApplication.CreateBuilder();

var services = builder.Services;
var config = builder.Configuration;

// Register RavenDB persistence with configuration
services.RegisterRavenPersistence(config);

// ... rest of your configuration
```

## Environment-Specific Configuration

Use different `appsettings` files for different environments:

### appsettings.json (Production)
```json
{
  "RavenDb": {
    "UseEmbedded": false,
    "Urls": "https://your-ravendb-cloud-instance.ravendb.cloud",
    "DatabaseName": "ProductionDb"
  }
}
```

### appsettings.Development.json (Development)
```json
{
  "RavenDb": {
    "UseEmbedded": false,
    "Urls": "http://localhost:8080",
    "DatabaseName": "DevelopmentDb"
  }
}
```

### appsettings.Test.json (Testing)
```json
{
  "RavenDb": {
    "UseEmbedded": true,
    "EmbeddedPath": "./TestData/RavenDB",
    "DatabaseName": "TestDb"
  }
}
```

## Features

- **Configuration-Driven**: All settings come from `appsettings.json`
- **Environment-Aware**: Different configurations for different environments
- **Multiple URLs**: Supports RavenDB clusters with comma-separated URLs
- **Embedded Support**: Ready for embedded mode (when package is installed)
- **Automatic Initialization**: Document store is initialized automatically
- **Scoped Sessions**: New session per HTTP request for proper resource management

## Notes

- The document store is registered as a **SINGLETON** because it's expensive to create and should exist once for the lifetime of the application
- Sessions are registered as **SCOPED** to ensure proper disposal per HTTP request
- When using embedded mode, make sure the `EmbeddedPath` directory exists or is writable by the application
- For production use with RavenDB Cloud, ensure proper authentication credentials are configured

## Troubleshooting

### "Could not connect to RavenDB server"
- Verify the `Urls` setting points to a running RavenDB instance
- Check network connectivity and firewall rules
- Ensure the database name matches an existing database on the server

### "Database not found"
- Verify the `DatabaseName` exists on the RavenDB server
- Create the database if it doesn't exist
- Check user permissions if using authentication

### Embedded mode not working
- Ensure `RavenDB.Embedded` NuGet package is installed
- Uncomment the embedded code section in the extension
- Verify the `EmbeddedPath` directory is writable
