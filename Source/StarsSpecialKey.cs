namespace Metacraft.VcsHardware;

public enum StarsSpecialKey
{
	NoOp = 0,
	Beaconator = 1,
	Cntr = 2,
	Maps = 3,
	Wx = 4,
	Dcb = 11,
	RngRing = 12,
	Range = 13,
	GreenBlank1 = 14,

	Core = 5,
	SignOn = 6,
	PrefSet = 15,
	WhiteBlank1 = 16,

	Brite = 7,
	Ldr = 8,
	CharSize = 9,
	Shift = 10,
	GreenBlank2 = 17,
	GreenBlank3 = 18,
	Site = 19,
	Mode = 20,

	InitCntl = 25,
	TrkRpos = 26,
	TrkSusp = 27,
	TermCntl = 28,
	HndOff = 29,
	FltData = 30,
	MultiFunc = 31,
	F8 = 32,
	F9 = 35,
	F10 = 36,
	Ca = 37,
	F12 = 38,
	F13 = 39,
	F14 = 40,
	TgtGen = 41,
	F16 = 42,

	Delta = 33,

	Min = 81,
	WhiteBlank2 = 82,
	WhiteBlank3 = 83
}

internal static class StarsSpecialKeyExtensions
{
	public static StarsSpecialKey ToStarsSpecialKey(this int buttonIndex)
	{
		try {
			return (StarsSpecialKey)(buttonIndex + 1);
		}
		catch {
			return StarsSpecialKey.NoOp;
		}
	}
}
