# VcsHardware

Class library for monitoring Virtual Controller Supply (VCS) custom keyboards via DirectInput.

## Install

```
dotnet add package Metacraft.VcsHardware
```

## Supported keyboards

- ERAM (`EramKeyboardReader`)
- STARS (`StarsKeyboardReader`)
- KSD (`KsdKeyboardReader`)

## Quick start

```csharp
using Metacraft.VcsHardware;

using var reader = new StarsKeyboardReader();
reader.ErrorOccurred         += (s, e) => Console.WriteLine($"Exception: {e}");
reader.KeyboardConnected     += (s, e) => Console.WriteLine("Keyboard connected.");
reader.KeyboardDisconnected  += (s, e) => Console.WriteLine("Keyboard disconnected.");
reader.SpecialKeyPressed     += (s, e) => Console.WriteLine($"Key pressed: {e.Key}");
reader.SpecialKeyReleased    += (s, e) => Console.WriteLine($"Key released: {e.Key}");

Console.WriteLine("Listening for STARS special key events. Press any normal key to exit.");
Console.ReadKey();
```

Swap `StarsKeyboardReader` for `EramKeyboardReader` or `KsdKeyboardReader` for other keyboard models.

## Logging

Each reader accepts an optional `ILogger<T>` (from `Microsoft.Extensions.Logging`). If none is supplied, a `NullLogger` is used and nothing is logged.

Pass one explicitly:

```csharp
using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    builder.AddSimpleConsole().SetMinimumLevel(LogLevel.Debug));

using var reader = new StarsKeyboardReader(
    logger: loggerFactory.CreateLogger<StarsKeyboardReader>());
```

Or resolve one from a DI container:

```csharp
using var reader = new StarsKeyboardReader(
    logger: serviceProvider.GetRequiredService<ILogger<StarsKeyboardReader>>());
```

Internally the readers log device-found / not-found at `Debug`, disconnects at `Information`, and unexpected failures at `Error`.

## Requirements

- .NET 8.0
- Windows (uses DirectInput via SharpDX)

## License

[MIT](LICENSE)
