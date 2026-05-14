namespace Metacraft.VcsHardware;

public enum KsdSpecialKey
{
	K1 = 0,
	K2 = 1,
	K3 = 2,
	K4 = 3,
	K5 = 4,
	K6 = 5,
	K7 = 6,
	K8 = 7,
	K9 = 8,
	RangeInc = 9,
	RangeDec = 11,
	VectorInc = 10,
	VectorDec = 12
}

internal static class KsdSpecialKeyExtensions
{
	public static KsdSpecialKey ToKsdSpecialKey(this int buttonIndex)
	{
		return (KsdSpecialKey)buttonIndex;
	}
}
