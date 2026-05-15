using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Metacraft.VcsHardware;

public sealed class KsdKeyboardReader(ILogger<KsdKeyboardReader>? logger = null)
	: KeyboardReader<KsdSpecialKeyEventArgs>(logger ?? NullLogger<KsdKeyboardReader>.Instance)
{
	protected override string KeyboardName => "KSD";
	protected override int ProductId => 0x0003;
	protected override KsdSpecialKeyEventArgs ToEventArgs(int buttonIndex) => new(buttonIndex.ToKsdSpecialKey());
}
