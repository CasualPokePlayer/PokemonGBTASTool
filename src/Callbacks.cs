using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace Gen2TASTool
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

		protected readonly List<MemoryCallbackDelegate> CallbackList = new();

		private bool CallbacksSet;
		private readonly Func<bool> BreakpointsActive;
		private readonly Dictionary<string, bool> BreakpointActive = new();
		private readonly string Which;

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
				"Vblank Random",
				"Random"
		};

		public RollChance AccuracyRng = new();
		public RollChance DamageRng = new();
		public RollChance EffectRng = new();
		public RollChance CritRng = new();
		public RollChance MetronomeRng = new();
		public RollChance CatchRng = new();
		public RollChance PokerusRng = new();

		public Gen2Callbacks(ApiContainer apis, SYM sym, Func<bool> getBreakpointsActive, string which)
			: base(apis, sym, getBreakpointsActive, which, BreakpointList)
		{
		}

		protected override void SetCallbacks()
		{
			base.SetCallbacks();

			// rng callbacks mostly just set two things, the roll and the chance.
			// for simplicity all RNG values have both of these and if they do not use one it is set to 0

			// accuracy roll
			CallbackList.Add(MakeRollChanceCallback(AccuracyRng, () => GetReg("A"), () => GetReg("B"), "Accuracy Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_CheckHit.skip_brightpowder") + 8, "ROM");
			// damage roll
			CallbackList.Add(MakeRollChanceCallback(DamageRng, () => GetReg("A"), () => 0, "Damage Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_DamageVariation.loop") + 8, "ROM");
			// effect roll
			CallbackList.Add(MakeRollChanceCallback(EffectRng, () => GetReg("A"), () => DereferenceHL(), "Effect Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_EffectChance.got_move_chance") + 4, "ROM");
			// crit roll
			CallbackList.Add(MakeRollChanceCallback(CritRng, () => GetReg("A"), () => DereferenceHL(), "Crit Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_Critical.Tally") + 9, "ROM");
			// metronome roll
			CallbackList.Add(MakeRollChanceCallback(MetronomeRng, () => GetReg("B"), () => 0, "Metronome Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("BattleCommand_Metronome.GetMove") + 26, "ROM");
			// catch roll
			CallbackList.Add(MakeRollChanceCallback(CatchRng, () => GetReg("A"), () => GetReg("B"), "Catch Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("PokeBallEffect.max_2") + 7, "ROM");
			// pokerus roll
			CallbackList.Add(MakeRollChanceCallback(PokerusRng, () => GetRandomU16(), () => 0, "Pokerus Roll"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("GivePokerusAndConvertBerries.loopMons") + 18, "ROM");

			// non rng callbacks are typically only used for pausing, make a generic callback for them

			// prompt button
			CallbackList.Add(MakeGenericCallback("Prompt Button"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("PromptButton"), "ROM");
			// wait button
			CallbackList.Add(MakeGenericCallback("Wait Button"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("WaitButton") + 10, "ROM");
			// check a ow
			CallbackList.Add(MakeGenericCallback("Check A Press Overworld"));
			APIs.MemoryEvents.AddExecCallback(CallbackList.Last(), GBSym.GetSYMDomAddr("CheckAPressOW"), "ROM");
		}
	}
}
