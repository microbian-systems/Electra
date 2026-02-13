# TickerQ.RavenDB

RavenDB persistence provider for TickerQ background job scheduling library.

## Installation

```bash
dotnet add package TickerQ.RavenDB
```

## Usage

```csharp
services.AddTickerQ(options =>
{
    options.AddRavenDbOperationalStore(ravenDb =>
    {
        ravenDb
            .WithUrls("http://localhost:8080")
            .WithDatabase("TickerQ")
            .WithCertificate("/path/to/cert.pfx", "password");
    });
});
```

## Features

- Full RavenDB document database support for TickerQ
- Optimistic concurrency using RavenDB change vectors
- Distributed locking via LockHolder/LockedAt fields
- Hierarchical TimeTicker support (parent/child relationships)
- Redis caching integration for cron expressions
- Support for RavenDB clusters

## Requirements

- .NET 10.0 or higher
- RavenDB 7.2 or higher

## License

MIT OR Apache-2.0
