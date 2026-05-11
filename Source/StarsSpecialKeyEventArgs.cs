namespace Metacraft.VcsHardware;

public class StarsSpecialKeyEventArgs(StarsSpecialKey key) : EventArgs
{
	public StarsSpecialKey Key { get; } = key;
}
