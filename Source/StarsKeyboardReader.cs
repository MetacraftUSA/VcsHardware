using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Metacraft.VcsHardware;

public sealed class StarsKeyboardReader(ILogger<StarsKeyboardReader>? logger = null)
	: KeyboardReader<StarsSpecialKeyEventArgs>(logger ?? NullLogger<StarsKeyboardReader>.Instance)
{
	protected override string KeyboardName => "STARS";
	protected override int ProductId => 0x808A;
	protected override StarsSpecialKeyEventArgs ToEventArgs(int buttonIndex) => new(buttonIndex.ToStarsSpecialKey());
}
