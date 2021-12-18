using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace Gen2TASTool
{
	public class Callbacks
	{
		public struct RollChance
		{
			public int Roll { get; set; }
			public int Chance { get; set; }
		}

		public RollChance AccuracyRng = new();
		public RollChance DamageRng = new();
		public RollChance EffectRng = new();
		public RollChance CritRng = new();
		public RollChance MetronomeRng = new();
		public RollChance CatchRng = new();
		public RollChance PokerusRng = new();

		private ApiContainer APIs { get; }

		private SYM GBSym { get; }

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

		private readonly Func<bool> BreakpointsActive;
		private readonly Dictionary<string, bool> BreakpointActive = new();
		private readonly List<MemoryCallbackDelegate> CallbackList = new();

		public Callbacks(ApiContainer apis, SYM sym, Func<bool> getBreakpointsActive)
		{
			foreach (string breakpoint in BreakpointList)
			{
				BreakpointActive.Add(breakpoint, false);
			}
			APIs = apis;
			GBSym = sym;
			BreakpointsActive = getBreakpointsActive;
			InitCallbacks();
		}

		private void InitCallbacks()
		{
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

		public void UpdateCallbacks(CheckedListBox checklist)
		{
			for (int i = 0; i < checklist.Items.Count; i++)
			{
				BreakpointActive[checklist.Items[i].ToString()] = checklist.GetItemChecked(i);
			}
		}

		private void MaybePause(string breakpoint)
		{
			if (BreakpointsActive() && BreakpointActive[breakpoint])
			{
				APIs.EmuClient.Pause();
			}
		}

		private int GetReg(string name) => (int)(APIs.Emulation.GetRegister(name) ?? 0);

		private int DereferenceHL()
		{
			var hl = GetReg("H") * 0x100 | GetReg("L");
			return (int)APIs.Memory.ReadU8(hl, "System Bus");
		}

		private int GetRandomU16()
		{
			APIs.Memory.SetBigEndian();
			return (ushort)APIs.Memory.ReadU16(GBSym.GetSYMDomAddr("hRandomAdd"), GBSym.GetSYMDomain("hRandomAdd"));
		}

		private MemoryCallbackDelegate MakeRollChanceCallback(RollChance rng, Func<int> getRoll, Func<int> getChance, string breakpoint)
		{
			return (uint address, uint value, uint flags) =>
			{
				rng.Roll = getRoll();
				rng.Chance = getChance();
				MaybePause(breakpoint);
			};
		}

		private MemoryCallbackDelegate MakeGenericCallback(string breakpoint)
		{
			return (uint address, uint value, uint flags) =>
			{
				MaybePause(breakpoint);
			};
		}
	}
}
