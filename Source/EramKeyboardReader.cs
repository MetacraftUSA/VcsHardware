using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Metacraft.VcsHardware;

public sealed class EramKeyboardReader(ILogger<EramKeyboardReader>? logger = null)
	: KeyboardReader<EramSpecialKeyEventArgs>(logger ?? NullLogger<EramKeyboardReader>.Instance)
{
	protected override string KeyboardName => "ERAM";
	protected override int ProductId => 0x0002;
	protected override EramSpecialKeyEventArgs ToEventArgs(int buttonIndex) => new(buttonIndex);
}
