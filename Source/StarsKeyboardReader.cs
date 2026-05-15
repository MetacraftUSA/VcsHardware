using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Metacraft.VcsHardware;

public sealed class StarsKeyboardReader : IDisposable
{
	private const int VENDOR_ID = 0x6F75;
	private const int PRODUCT_ID = 0x808A;

	public event EventHandler? KeyboardConnected;
	public event EventHandler? KeyboardDisconnected;
	public event EventHandler<StarsSpecialKeyEventArgs>? SpecialKeyPressed;
	public event EventHandler<StarsSpecialKeyEventArgs>? SpecialKeyReleased;
	public event EventHandler<Exception>? ErrorOccurred;

	private readonly KeyboardReader mReader;

	public bool IsKeyboardPresent => mReader.IsKeyboardPresent;

	public StarsKeyboardReader(
		int vendorId = VENDOR_ID,
		int productId = PRODUCT_ID,
		ILogger<StarsKeyboardReader>? logger = null
	)
	{
		mReader = new(
			"VCS STARS keyboard",
			vendorId,
			productId,
			logger ?? NullLogger<StarsKeyboardReader>.Instance,
			() => KeyboardConnected?.Invoke(this, EventArgs.Empty),
			() => KeyboardDisconnected?.Invoke(this, EventArgs.Empty),
			btn => SpecialKeyPressed?.Invoke(this, new StarsSpecialKeyEventArgs(btn.ToStarsSpecialKey())),
			btn => SpecialKeyReleased?.Invoke(this, new StarsSpecialKeyEventArgs(btn.ToStarsSpecialKey())),
			ex => ErrorOccurred?.Invoke(this, ex)
		);
	}

	public void Dispose()
	{
		mReader.Dispose();
	}
}
