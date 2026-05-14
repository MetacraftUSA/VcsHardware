namespace Metacraft.VcsHardware;

public class KsdSpecialKeyEventArgs(KsdSpecialKey key) : EventArgs
{
	public KsdSpecialKey Key { get; } = key;
}
