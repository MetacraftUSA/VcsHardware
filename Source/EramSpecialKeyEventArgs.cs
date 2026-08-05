namespace Metacraft.VcsHardware;

public class EramSpecialKeyEventArgs(EramSpecialKey key) : EventArgs
{
	public EramSpecialKey Key { get; } = key;
}
