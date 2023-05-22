using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace PokemonGBTASTool;

public abstract class Callbacks
{
	public class RollChance
	{
		public int Roll { get; private set; }
		public int Chance { get; private set; }

		private Func<int> GetRoll { get; }
		private Func<int> GetChance { get; }

		public RollChance(Func<int> getRoll, Func<int> getChance)
		{
			GetRoll = getRoll;
			GetChance = getChance;
		}

		public void SetRollChance()
		{
			Roll = GetRoll();
			Chance = GetChance();
		}
	}

	protected ApiContainer APIs { get; }
	protected IMemoryEventsApi MemEvents => APIs.MemoryEvents!;
	protected PokemonGame PkmnGame { get; }

	protected List<MemoryCallbackDelegate> CallbackList { get; } = new();

	private bool CallbacksSet { get; set; }
	private Func<bool> BreakpointsActive { get; }

	private Dictionary<string, bool> BreakpointActive { get; } = new();

	protected Callbacks(ApiContainer apis, PokemonGame pkmnGame, Func<bool> getBreakpointsActive, string[] breakpointList)
	{
		foreach (var breakpoint in breakpointList)
		{
			BreakpointActive.Add(breakpoint, false);
		}

		APIs = apis;
		PkmnGame = pkmnGame;
		BreakpointsActive = getBreakpointsActive;
		SetCallbacks();
	}

	protected virtual void SetCallbacks()
	{
		CallbacksSet = true;
	}

	private void RemoveCallbacks()
	{
		foreach (var cb in CallbackList)
		{
			MemEvents.RemoveMemoryCallback(cb);
		}

		CallbackList.Clear();
		CallbacksSet = false;
	}

	public void UpdateCallbacks(CheckedListBox checklist, bool disableCallbacks)
	{
		for (var i = 0; i < checklist.Items.Count; i++)
		{
			BreakpointActive[checklist.Items[i].ToString()] = checklist.GetItemChecked(i);
		}

		switch (disableCallbacks)
		{
			case true when CallbacksSet:
				RemoveCallbacks();
				break;
			case false when !CallbacksSet:
				SetCallbacks();
				break;
		}
	}

	private void MaybePause(string breakpoint)
	{
		if (BreakpointsActive() && BreakpointActive[breakpoint])
		{
			APIs.EmuClient.Pause();
		}
	}

	protected MemoryCallbackDelegate MakeRollChanceCallback(Action setRollChance, string breakpoint)
	{
		return (_, _, _) =>
		{
			setRollChance();
			MaybePause(breakpoint);
		};
	}

	protected MemoryCallbackDelegate MakeGenericCallback(string breakpoint)
	{
		return (_, _, _) =>
		{
			MaybePause(breakpoint);
		};
	}
}

public sealed class Gen1Callbacks : Callbacks
{
	public static string[] BreakpointList { get; } =
	{
		"Accuracy Roll",
		"Damage Roll",
		"Effect Roll",
		"Crit Roll",
		"Metronome Roll",
		"1st Catch Roll",
		"2nd Catch Roll",
		// add ai things todo
		"Wait For Text Scroll Button Press",
		"Joypad Overworld",
	};

	public RollChance AccuracyRng { get; }
	public RollChance DamageRng { get; }
	// ReSharper disable once UnusedAutoPropertyAccessor.Global
	public RollChance EffectRng { get; }
	public RollChance CritRng { get; }
	// ReSharper disable once UnusedAutoPropertyAccessor.Global
	public RollChance MetronomeRng { get; }
	public RollChance Catch1Rng { get; }
	public RollChance Catch2Rng { get; }

	public Gen1Callbacks(ApiContainer apis, PokemonGame pkmnGame, Func<bool> getBreakpointsActive)
		: base(apis, pkmnGame, getBreakpointsActive, BreakpointList)
	{
		AccuracyRng = new(() => PkmnGame.GetReg("A"), () => PkmnGame.GetReg("B"));
		DamageRng = new(() => PkmnGame.GetReg("A"), () => 0);
		EffectRng = new(() => 0, () => 0); // todo
		CritRng = new(() => PkmnGame.GetReg("A"), () => PkmnGame.GetReg("B"));
		MetronomeRng = new(() => 0, () => 0); // todo
		Catch1Rng = new(() => PkmnGame.GetReg("B"), () => PkmnGame.GetReg("A"));
		Catch2Rng = new(() => PkmnGame.GetReg("B"), () => PkmnGame.GetReg("A"));
	}

	protected override void SetCallbacks()
	{
		var romScope = PkmnGame.Which + "ROM";

		// rng callbacks mostly just set two things, the roll and the chance.
		// for simplicity all RNG values have both of these and if they do not use one it is set to 0

		// accuracy roll
		CallbackList.Add(MakeRollChanceCallback(() => AccuracyRng.SetRollChance(), "Accuracy Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("MoveHitTest.doAccuracyCheck") + 3, romScope);
		// damage roll
		CallbackList.Add(MakeRollChanceCallback(() => DamageRng.SetRollChance(), "Damage Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("RandomizeDamage.loop") + 8, romScope);
		/*// effect roll
		CallbackList.Add(MakeRollChanceCallback(EffectRng.SetRollChance, "Effect Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_EffectChance.got_move_chance") + 4, romScope);*/
		// crit roll
		CallbackList.Add(MakeRollChanceCallback(() => CritRng.SetRollChance(), "Crit Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("CriticalHitTest.SkipHighCritical") + 9, romScope);
		/*// metronome roll
		CallbackList.Add(MakeRollChanceCallback(MetronomeRng.SetRollChance, "Metronome Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_Metronome.GetMove") + 26, romScope);*/
		// catch roll 1
		CallbackList.Add(MakeRollChanceCallback(() => Catch1Rng.SetRollChance(), "1st Catch Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("ItemUseBall.skip3") + 4, romScope);
		// catch roll 2
		CallbackList.Add(MakeRollChanceCallback(() => Catch2Rng.SetRollChance(), "2nd Catch Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("ItemUseBall.skip3") + 18, romScope);

		// non rng callbacks are typically only used for pausing, make a generic callback for them

		// wait for text scroll button press
		CallbackList.Add(MakeGenericCallback("Wait For Text Scroll Button Press"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("WaitForTextScrollButtonPress"), romScope);
		// joypad ow
		CallbackList.Add(MakeGenericCallback("Joypad Overworld"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("JoypadOverworld"), romScope);

		base.SetCallbacks();
	}
}

public sealed class Gen2Callbacks : Callbacks
{
	public static string[] BreakpointList { get; } =
	{
		"Accuracy Roll",
		"Damage Roll",
		"Effect Roll",
		"Crit Roll",
		"Metronome Roll",
		"Catch Roll",
		"Pokerus Roll",
		// add ai things todo
		"Prompt Button",
		"Wait Button",
		"Check A Press Overworld",
		//"Vblank Random",
		//"Random",
	};

	public RollChance AccuracyRng { get; }
	public RollChance DamageRng { get; }
	public RollChance EffectRng { get; }
	public RollChance CritRng { get; }
	public RollChance MetronomeRng { get; }
	public RollChance CatchRng { get; }
	public RollChance PokerusRng { get; }

	private bool OverworldLagged { get; set; }

	public Gen2Callbacks(ApiContainer apis, PokemonGame pkmnGame, Func<bool> getBreakpointsActive)
		: base(apis, pkmnGame, getBreakpointsActive, BreakpointList)
	{
		AccuracyRng = new(() => PkmnGame.GetReg("A"), () => PkmnGame.GetReg("B"));
		DamageRng = new(() => PkmnGame.GetReg("A"), () => 0);
		EffectRng = new(() => PkmnGame.GetReg("A"), () => PkmnGame.DereferenceHL());
		CritRng = new(() => PkmnGame.GetReg("A"), () => PkmnGame.DereferenceHL());
		MetronomeRng = new(() => PkmnGame.GetReg("B"), () => 0);
		CatchRng = new(() => PkmnGame.GetReg("A"), () => PkmnGame.GetReg("B"));
		PokerusRng = new(() => PkmnGame.ReadU16("hRandomAdd"), () => 0);
	}

	protected override void SetCallbacks()
	{
		var romScope = PkmnGame.Which + "ROM";

		// rng callbacks mostly just set two things, the roll and the chance.
		// for simplicity all RNG values have both of these and if they do not use one it is set to 0

		// accuracy roll
		CallbackList.Add(MakeRollChanceCallback(() => AccuracyRng.SetRollChance(), "Accuracy Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("BattleCommand_CheckHit.skip_brightpowder") + 8, romScope);
		// damage roll
		CallbackList.Add(MakeRollChanceCallback(() => DamageRng.SetRollChance(), "Damage Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("BattleCommand_DamageVariation.loop") + 8, romScope);
		// effect roll
		CallbackList.Add(MakeRollChanceCallback(() => EffectRng.SetRollChance(), "Effect Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("BattleCommand_EffectChance.got_move_chance") + 4, romScope);
		// crit roll
		CallbackList.Add(MakeRollChanceCallback(() => CritRng.SetRollChance(), "Crit Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("BattleCommand_Critical.Tally") + 9, romScope);
		// metronome roll
		CallbackList.Add(MakeRollChanceCallback(() => MetronomeRng.SetRollChance(), "Metronome Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("BattleCommand_Metronome.GetMove") + 26, romScope);
		// catch roll
		CallbackList.Add(MakeRollChanceCallback(() => CatchRng.SetRollChance(), "Catch Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("PokeBallEffect.max_2") + 7, romScope);
		// pokerus roll
		CallbackList.Add(MakeRollChanceCallback(() => PokerusRng.SetRollChance(), "Pokerus Roll"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("GivePokerusAndConvertBerries.loopMons") + 18, romScope);

		// non rng callbacks are typically only used for pausing, make a generic callback for them

		// prompt button
		CallbackList.Add(MakeGenericCallback("Prompt Button"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("PromptButton"), romScope);
		// wait button
		CallbackList.Add(MakeGenericCallback("Wait Button"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("WaitButton") + 10, romScope);
		// check a ow
		CallbackList.Add(MakeGenericCallback("Check A Press Overworld"));
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("CheckAPressOW"), romScope);

		// special callbacks for handling special overworld lag
		CallbackList.Add((_, _, _) =>
		{
			OverworldLagged = PkmnGame.ReadU8("wOverworldDelay") == 0;
		});
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("VBlank0.done_oam"), romScope);

		CallbackList.Add((_, _, _) =>
		{
			var stepFlags = PkmnGame.ReadU8("wPlayerStepFlags");
			// PLAYERSTEP_CONTINUE_F == 5
			// PLAYERSTEP_STOP_F == 6
			if (stepFlags.Bit(5) && !stepFlags.Bit(6))
			{
				APIs.Emulation.SetIsLagged(OverworldLagged);
			}
		});
		MemEvents.AddExecCallback(CallbackList.Last(), PkmnGame.GetSYMDomAddr("VBlank0.ok2") + 3, romScope);

		base.SetCallbacks();
	}
}
