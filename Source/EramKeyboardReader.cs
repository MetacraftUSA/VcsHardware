using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Metacraft.VcsHardware;

public sealed class EramKeyboardReader : IDisposable
{
	private const int VENDOR_ID = 28533;
	private const int PRODUCT_ID = 32906;

	public event EventHandler<int>? SpecialKeyPressed;
	public event EventHandler<int>? SpecialKeyReleased;
	public event EventHandler<Exception>? ErrorOccurred;

	private readonly KeyboardReader mReader;

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
			btn => SpecialKeyPressed?.Invoke(this, btn),
			btn => SpecialKeyReleased?.Invoke(this, btn),
			ex => ErrorOccurred?.Invoke(this, ex)
		);
	}

	public void Dispose()
	{
		mReader.Dispose();
	}
}
