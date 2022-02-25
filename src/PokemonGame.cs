using System;

using BizHawk.Client.Common;

namespace PokemonGBTASTool
{
	public abstract class PokemonGame : SYM
	{
		public ApiContainer APIs { get; }
		public Callbacks CBs { get; }
		public PokemonData PkmnData { get; }
		public bool IsGen2 { get; }

		public PokemonGame(ApiContainer apis, string sym, Action<string> messageCb, Func<bool> getBreakpointsActive, string which, bool gen2)
			: base(sym, messageCb, which)
		{
			APIs = apis;
			CBs = gen2
				? new Gen2Callbacks(apis, this, getBreakpointsActive)
				: new Gen1Callbacks(apis, this, getBreakpointsActive);
			PkmnData = new PokemonData(messageCb);
			IsGen2 = gen2;
		}

		public byte ReadU8(string symbol) => (byte)APIs.Memory.ReadU8(GetSYMDomAddr(symbol), GetSYMDomain(symbol));

		// only big endian is useful for us
		public ushort ReadU16(string symbol)
        {
			APIs.Memory.SetBigEndian();
			return (ushort)APIs.Memory.ReadU16(GetSYMDomAddr(symbol), GetSYMDomain(symbol));
		}

		public string GetEnemyMonName() => PkmnData.GetPokemonSpeciesName(ReadU8("wEnemyMonSpecies"), IsGen2);
		public string GetEnemyMonMove() => PkmnData.GetPokemonMoveName(ReadU8(IsGen2 ? "wCurEnemyMove" : "wEnemySelectedMove"));
		public byte GetDSUM() => (byte)((ReadU8("hRandomAdd") + ReadU8("hRandomSub")) & 0xFF);

		// these don't really belong here, but don't really have a better place, /shrug
		public int GetReg(string name) => (int)(APIs.Emulation.GetRegister(Which + name) ?? throw new NullReferenceException());

		public int DereferenceHL()
		{
			var hl = GetReg("H") * 0x100 | GetReg("L");
			return (int)APIs.Memory.ReadU8(hl, Which + "System Bus");
		}
	}

	public sealed class PokemonRed : PokemonGame
	{
		public PokemonRed(ApiContainer apis, Action<string> messageCb, Func<bool> getBreakpointsActive, string which)
			: base(apis, "PokemonGBTASTool.res.pokered.sym.gz", messageCb, getBreakpointsActive, which, false)
		{
		}
	}

	public sealed class PokemonBlue : PokemonGame
	{
		public PokemonBlue(ApiContainer apis, Action<string> messageCb, Func<bool> getBreakpointsActive, string which)
			: base(apis, "PokemonGBTASTool.res.pokeblue.sym.gz", messageCb, getBreakpointsActive, which, false)
		{
		}
	}

	public sealed class PokemonYellow : PokemonGame
	{
		public PokemonYellow(ApiContainer apis, Action<string> messageCb, Func<bool> getBreakpointsActive, string which)
			: base(apis, "PokemonGBTASTool.res.pokeyellow.sym.gz", messageCb, getBreakpointsActive, which, false)
		{
		}
	}

	public sealed class PokemonGold : PokemonGame
	{
		public PokemonGold(ApiContainer apis, Action<string> messageCb, Func<bool> getBreakpointsActive, string which)
			: base(apis, "PokemonGBTASTool.res.pokegold.sym.gz", messageCb, getBreakpointsActive, which, true)
		{
		}
	}

	public sealed class PokemonSilver : PokemonGame
	{
		public PokemonSilver(ApiContainer apis, Action<string> messageCb, Func<bool> getBreakpointsActive, string which)
			: base(apis, "PokemonGBTASTool.res.pokesilver.sym.gz", messageCb, getBreakpointsActive, which, true)
		{
		}
	}

	public sealed class PokemonCrystal : PokemonGame
	{
		public PokemonCrystal(ApiContainer apis, Action<string> messageCb, Func<bool> getBreakpointsActive, string which)
			: base(apis, "PokemonGBTASTool.res.pokecrystal.sym.gz", messageCb, getBreakpointsActive, which, true)
		{
		}
	}
}
