using Metacraft.VcsHardware;

namespace ConsoleSample;

internal class Program
{
	static void Main()
	{
		StarsKeyboardReader starsReader = new();
		starsReader.ErrorOccurred += (s, e) => Console.WriteLine($"Exception: {e}");
		starsReader.SpecialKeyPressed += (s, e) => Console.WriteLine($"Key pressed: {e.Key}");
		starsReader.SpecialKeyReleased += (s, e) => Console.WriteLine($"Key released: {e.Key}");
		starsReader.KeyboardConnected += (s, e) => Console.WriteLine("Keyboard connected.");
		starsReader.KeyboardDisconnected += (s, e) => Console.WriteLine("Keyboard disconnected.");
		Console.WriteLine("Listening for STARS special key events. Press any normal key to exit.");
		Console.ReadKey();
	}
}
