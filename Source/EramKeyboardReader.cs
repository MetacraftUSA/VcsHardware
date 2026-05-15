using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Metacraft.VcsHardware;

public sealed class EramKeyboardReader : IDisposable
{
	private const int VENDOR_ID = 0x6F75;
	private const int PRODUCT_ID = 0x0002;

	public event EventHandler<EramSpecialKeyEventArgs>? SpecialKeyPressed;
	public event EventHandler<EramSpecialKeyEventArgs>? SpecialKeyReleased;
	public event EventHandler<Exception>? ErrorOccurred;

	private readonly KeyboardReader mReader;

	public bool IsKeyboardPresent => mReader.IsKeyboardPresent;

	public EramKeyboardReader(
		int vendorId = VENDOR_ID,
		int productId = PRODUCT_ID,
		ILogger<EramKeyboardReader>? logger = null
	)
	{
		mReader = new(
			"VCS ERAM keyboard",
			vendorId,
			productId,
			logger ?? NullLogger<EramKeyboardReader>.Instance,
			btn => SpecialKeyPressed?.Invoke(this, new EramSpecialKeyEventArgs(btn)),
			btn => SpecialKeyReleased?.Invoke(this, new EramSpecialKeyEventArgs(btn)),
			ex => ErrorOccurred?.Invoke(this, ex)
		);
	}

	public void Dispose()
	{
		mReader.Dispose();
	}
}
