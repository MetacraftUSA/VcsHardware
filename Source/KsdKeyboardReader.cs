using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Metacraft.VcsHardware;

public sealed class KsdKeyboardReader : IDisposable
{
	private const int VENDOR_ID = 0x6F75;
	private const int PRODUCT_ID = 0x0003;

	public event EventHandler<KsdSpecialKeyEventArgs>? SpecialKeyPressed;
	public event EventHandler<KsdSpecialKeyEventArgs>? SpecialKeyReleased;
	public event EventHandler<Exception>? ErrorOccurred;
	public event EventHandler? KeyboardConnected;
	public event EventHandler? KeyboardDisconnected;

	private readonly KeyboardReader mReader;

	public bool IsKeyboardPresent => mReader.IsKeyboardPresent;

	public KsdKeyboardReader(
		int vendorId = VENDOR_ID,
		int productId = PRODUCT_ID,
		ILogger<KsdKeyboardReader>? logger = null
	)
	{
		mReader = new(
			"VCS KSD keyboard",
			vendorId,
			productId,
			logger ?? NullLogger<KsdKeyboardReader>.Instance,
			btn => SpecialKeyPressed?.Invoke(this, new KsdSpecialKeyEventArgs(btn.ToKsdSpecialKey())),
			btn => SpecialKeyReleased?.Invoke(this, new KsdSpecialKeyEventArgs(btn.ToKsdSpecialKey())),
			ex => ErrorOccurred?.Invoke(this, ex),
			() => KeyboardConnected?.Invoke(this, EventArgs.Empty),
			() => KeyboardDisconnected?.Invoke(this, EventArgs.Empty)
		);
	}

	public void Dispose()
	{
		mReader.Dispose();
	}
}
