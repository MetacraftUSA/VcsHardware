namespace Metacraft.VcsHardware;

public enum KsdSpecialKey
{
	NoOp = 0,

	K1 = 1,
	K2 = 2,
	K3 = 3,
	K4 = 4,
	K5 = 5,
	K6 = 6,
	K7 = 7,
	K8 = 8,
	K9 = 9,

	RangeInc = 10,
	RangeDec = 12,

	VectorInc = 11,
	VectorDec = 13
}

internal static class KsdSpecialKeyExtensions
{
	public static KsdSpecialKey ToKsdSpecialKey(this int buttonIndex)
	{
		try {
			return (KsdSpecialKey)(buttonIndex + 1);
		}
		catch {
			return KsdSpecialKey.NoOp;
		}
	}
}
