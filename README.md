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
reader.ErrorOccurred       += (s, e) => Console.WriteLine($"Exception: {e}");
reader.SpecialKeyPressed   += (s, e) => Console.WriteLine($"Key pressed: {e.Key}");
reader.SpecialKeyReleased  += (s, e) => Console.WriteLine($"Key released: {e.Key}");

Console.WriteLine("Listening for STARS special key events. Press any normal key to exit.");
Console.ReadKey();
```

Swap `StarsKeyboardReader` for `EramKeyboardReader` or `KsdKeyboardReader` for other keyboard models.

## Requirements

- .NET 8.0
- Windows (uses DirectInput via SharpDX)

## License

[MIT](LICENSE)
