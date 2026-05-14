namespace Metacraft.VcsHardware;

public class EramSpecialKeyEventArgs(int buttonIndex) : EventArgs
{
	public int ButtonIndex { get; } = buttonIndex;
}
