using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace PokemonGBTASTool
{
	public abstract class Callbacks
	{
		public struct RollChance
		{
			public int Roll { get; set; }
			public int Chance { get; set; }
		}

		protected ApiContainer APIs { get; }
		protected SYM GBSym { get; }
		protected string Which { get; }

		protected List<MemoryCallbackDelegate> CallbackList { get; } = new();

		private bool CallbacksSet { get; set; }
		private Func<bool> BreakpointsActive { get; }

		private Dictionary<string, bool> BreakpointActive { get; } = new();

		public Callbacks(ApiContainer apis, SYM sym, Func<bool> getBreakpointsActive, string which, string[] breakpointList)
		{
			foreach (string breakpoint in breakpointList)
			{
				BreakpointActive.Add(breakpoint, false);
			}
			APIs = apis;
			GBSym = sym;
			BreakpointsActive = getBreakpointsActive;
			Which = which;
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
				APIs.MemoryEvents.RemoveMemoryCallback(cb);
			}

			CallbackList.Clear();
			CallbacksSet = false;
		}

		public void UpdateCallbacks(CheckedListBox checklist, bool disableCallbacks)
		{
			for (int i = 0; i < checklist.Items.Count; i++)
			{
				BreakpointActive[checklist.Items[i].ToString()] = checklist.GetItemChecked(i);
			}
			if (disableCallbacks && CallbacksSet)
			{
				RemoveCallbacks();
			}
			else if (!disableCallbacks && !CallbacksSet)
			{
				SetCallbacks();
			}
		}

		private void MaybePause(string breakpoint)
		{
			if (BreakpointsActive() && BreakpointActive[breakpoint])
			{
				APIs.EmuClient.Pause();
			}
		}

		protected int GetReg(string name) => (int)(APIs.Emulation.GetRegister(Which + name) ?? throw new NullReferenceException());

		protected int DereferenceHL()
		{
			var hl = GetReg("H") * 0x100 | GetReg("L");
			return (int)APIs.Memory.ReadU8(hl, Which + "System Bus");
		}

		protected int GetRandomU16()
		{
			APIs.Memory.SetBigEndian();
			return (ushort)APIs.Memory.ReadU16(GBSym.GetSYMDomAddr("hRandomAdd"), GBSym.GetSYMDomain("hRandomAdd"));
		}

		protected MemoryCallbackDelegate MakeRollChanceCallback(RollChance rng, Func<int> getRoll, Func<int> getChance, string breakpoint)
		{
			return (uint address, uint value, uint flags) =>
			{
				rng.Roll = getRoll();
				rng.Chance = getChance();
				MaybePause(breakpoint);
			};
		}

		protected MemoryCallbackDelegate MakeGenericCallback(string breakpoint)
		{
			return (uint address, uint value, uint flags) =>
			{
				MaybePause(breakpoint);
			};
		}
	}

	public sealed class Gen1Callbacks : Callbacks
	{
		public static readonly string[] BreakpointList =
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

		public RollChance AccuracyRng { get; } = new();
		public RollChance DamageRng { get; } = new();
		public RollChance EffectRng { get; } = new();
		public RollChance CritRng { get; } = new();
		public RollChance MetronomeRng { get; } = new();
		public RollChance Catch1Rng { get; } = new();
		public RollChance Catch2Rng { get; } = new();

		public Gen1Callbacks(ApiContainer apis, SYM sym, Func<bool> getBreakpointsActive, string which)
			: base(apis, sym, getBreakpointsActive, which, BreakpointList)
		{
		}

		protected override void SetCallbacks()
		{
			string romScope = Which + "ROM";

			// rng callbacks mostly just set two things, the roll and the chance.
			// for simplicity all RNG values have both of these and if they do not use one it is set to 0

			// accuracy roll
			CallbackList.Add(MakeRollChanceCallback(AccuracyRng, () => GetReg("A"), () => GetReg("B"), "Accuracy Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("MoveHitTest.doAccuracyCheck") + 3, romScope);
			// damage roll
			CallbackList.Add(MakeRollChanceCallback(DamageRng, () => GetReg("A"), () => 0, "Damage Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("RandomizeDamage.loop") + 8, romScope);
			/*// effect roll
			CallbackList.Add(MakeRollChanceCallback(EffectRng, () => GetReg("A"), () => DereferenceHL(), "Effect Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_EffectChance.got_move_chance") + 4, romScope);*/
			// crit roll
			CallbackList.Add(MakeRollChanceCallback(CritRng, () => GetReg("A"), () => GetReg("B"), "Crit Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("CriticalHitTest.SkipHighCritical") + 9, romScope);
			/*// metronome roll
			CallbackList.Add(MakeRollChanceCallback(MetronomeRng, () => GetReg("B"), () => 0, "Metronome Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_Metronome.GetMove") + 26, romScope);*/
			// catch roll 1
			CallbackList.Add(MakeRollChanceCallback(Catch1Rng, () => GetReg("B"), () => GetReg("A"), "1st Catch Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("ItemUseBall.skip3") + 4, romScope);
			// catch roll 2
			CallbackList.Add(MakeRollChanceCallback(Catch2Rng, () => GetReg("B"), () => GetReg("A"), "2nd Catch Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("ItemUseBall.skip3") + 18, romScope);

			// non rng callbacks are typically only used for pausing, make a generic callback for them

			// wait for text scroll button press
			CallbackList.Add(MakeGenericCallback("Wait For Text Scroll Button Press"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("WaitForTextScrollButtonPress"), romScope);
			// joypad ow
			CallbackList.Add(MakeGenericCallback("Joypad Overworld"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("JoypadOverworld"), romScope);

			base.SetCallbacks();
		}
	}

	public sealed class Gen2Callbacks : Callbacks
	{
		public static readonly string[] BreakpointList =
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
				//"Random"
		};

		public RollChance AccuracyRng { get; } = new();
		public RollChance DamageRng { get; } = new();
		public RollChance EffectRng { get; } = new();
		public RollChance CritRng { get; } = new();
		public RollChance MetronomeRng { get; } = new();
		public RollChance CatchRng { get; } = new();
		public RollChance PokerusRng { get; } = new();

		public Gen2Callbacks(ApiContainer apis, SYM sym, Func<bool> getBreakpointsActive, string which)
			: base(apis, sym, getBreakpointsActive, which, BreakpointList)
		{
		}

		protected override void SetCallbacks()
		{
			string romScope = Which + "ROM";

			// rng callbacks mostly just set two things, the roll and the chance.
			// for simplicity all RNG values have both of these and if they do not use one it is set to 0

			// accuracy roll
			CallbackList.Add(MakeRollChanceCallback(AccuracyRng, () => GetReg("A"), () => GetReg("B"), "Accuracy Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_CheckHit.skip_brightpowder") + 8, romScope);
			// damage roll
			CallbackList.Add(MakeRollChanceCallback(DamageRng, () => GetReg("A"), () => 0, "Damage Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_DamageVariation.loop") + 8, romScope);
			// effect roll
			CallbackList.Add(MakeRollChanceCallback(EffectRng, () => GetReg("A"), () => DereferenceHL(), "Effect Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_EffectChance.got_move_chance") + 4, romScope);
			// crit roll
			CallbackList.Add(MakeRollChanceCallback(CritRng, () => GetReg("A"), () => DereferenceHL(), "Crit Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_Critical.Tally") + 9, romScope);
			// metronome roll
			CallbackList.Add(MakeRollChanceCallback(MetronomeRng, () => GetReg("B"), () => 0, "Metronome Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_Metronome.GetMove") + 26, romScope);
			// catch roll
			CallbackList.Add(MakeRollChanceCallback(CatchRng, () => GetReg("A"), () => GetReg("B"), "Catch Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("PokeBallEffect.max_2") + 7, romScope);
			// pokerus roll
			CallbackList.Add(MakeRollChanceCallback(PokerusRng, () => GetRandomU16(), () => 0, "Pokerus Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("GivePokerusAndConvertBerries.loopMons") + 18, romScope);

			// non rng callbacks are typically only used for pausing, make a generic callback for them

			// prompt button
			CallbackList.Add(MakeGenericCallback("Prompt Button"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("PromptButton"), romScope);
			// wait button
			CallbackList.Add(MakeGenericCallback("Wait Button"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("WaitButton") + 10, romScope);
			// check a ow
			CallbackList.Add(MakeGenericCallback("Check A Press Overworld"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("CheckAPressOW"), romScope);

			base.SetCallbacks();
		}
	}
}
