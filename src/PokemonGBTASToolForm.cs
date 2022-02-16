using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using BizHawk.Emulation.Common;

namespace PokemonGBTASTool
{
	[ExternalTool("Pokemon Link GB TAS Tool", Description = "A tool to help with TASing Link GB Pokemon games.")]
	[ExternalToolApplicability.SingleSystem(CoreSystem.GameBoyLink)]
	[ExternalToolEmbeddedIcon("PokemonGBTASTool.res.icon.ico")]
	public partial class PokemonGBTASToolForm : ToolFormBase, IExternalToolForm
	{
		public ApiContainer? ApiContainer { get; set; }
		private ApiContainer APIs => ApiContainer ?? throw new NullReferenceException();

		private CrystalSYM CrystalSym { get; }
		private SilverSYM SilverSym { get; }
		private YellowSYM YellowSym { get; }

		private List<Callbacks> Callbacks { get; } = new(3);

		private Gen2Callbacks CrystalCBs => Callbacks[0] as Gen2Callbacks ?? throw new NullReferenceException();
		private Gen2Callbacks SilverCBs => Callbacks[1] as Gen2Callbacks ?? throw new NullReferenceException();
		private Gen1Callbacks YellowCBs => Callbacks[2] as Gen1Callbacks ?? throw new NullReferenceException();

		private PokemonData PkmnData { get; }

		protected override string WindowTitleStatic => "Pokemon GB TAS Tool";

		public PokemonGBTASToolForm()
		{
			InitializeComponent();
			CrystalSym = new CrystalSYM(ShowMessage, "P1 ");
			SilverSym = new SilverSYM(ShowMessage, "P2 ");
			YellowSym = new YellowSYM(ShowMessage, "P3 ");
			PkmnData = new PokemonData(ShowMessage);
			Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("PokemonGBTASTool.res.icon.ico"));
			Closing += (sender, args) => APIs.EmuClient.SetGameExtraPadding(0, 0, 0, 0);
		}

		/// <remarks>This is called once when the form is opened, and every time a new movie session starts.</remarks>
		public override void Restart()
		{
			if (string.IsNullOrEmpty(APIs.GameInfo.GetBoardType()))
			{
				ShowMessage("Only GambatteLink is supported with this tool");
				Close();
			}
			checkedListBox1.Items.Clear();
			checkedListBox1.Items.AddRange(Gen2Callbacks.BreakpointList);
			checkedListBox1.CheckOnClick = true;
			checkedListBox2.Items.Clear();
			checkedListBox2.Items.AddRange(Gen2Callbacks.BreakpointList);
			checkedListBox2.CheckOnClick = true;
			checkedListBox3.Items.Clear();
			checkedListBox3.Items.AddRange(Gen1Callbacks.BreakpointList);
			checkedListBox3.CheckOnClick = true;
			comboBox1.SelectedIndex = 0;
			Callbacks.Clear();
			Callbacks.Add(new Gen2Callbacks(APIs, CrystalSym, () => checkBox1.Checked && comboBox1.SelectedIndex is 0, "P1 "));
			Callbacks.Add(new Gen2Callbacks(APIs, SilverSym, () => checkBox1.Checked && comboBox1.SelectedIndex is 1, "P2 "));
			Callbacks.Add(new Gen1Callbacks(APIs, YellowSym, () => checkBox1.Checked && comboBox1.SelectedIndex is 2, "P3 "));
			APIs.EmuClient.SetGameExtraPadding(0, 0, 105, 0);
		}

		private byte CpuReadU8(string symbol, SYM GBSym) => (byte)APIs.Memory.ReadU8(GBSym.GetSYMDomAddr(symbol), GBSym.GetSYMDomain(symbol));

		private ushort CpuReadBigU16(string symbol, SYM GBSym)
		{
			APIs.Memory.SetBigEndian();
			return (ushort)APIs.Memory.ReadU16(GBSym.GetSYMDomAddr(symbol), GBSym.GetSYMDomain(symbol));
		}

		private string GetEnemyMonName(SYM GBSym) => PkmnData.GetPokemonSpeciesName(CpuReadU8("wEnemyMonSpecies", GBSym), GBSym.IsGen2);
		private string GetEnemyMonMove(SYM GBSym) => PkmnData.GetPokemonMoveName(CpuReadU8(GBSym.IsGen2 ? "wCurEnemyMove" : "wEnemySelectedMove", GBSym));
		private byte GetDSUM(SYM GBSym) => (byte)((CpuReadU8("hRandomAdd", GBSym) + CpuReadU8("hRandomSub", GBSym)) & 0xFF);

		public override void UpdateValues(ToolFormUpdateType type)
		{
			if (type is ToolFormUpdateType.PreFrame or ToolFormUpdateType.FastPreFrame)
			{
				CrystalCBs.UpdateCallbacks(checkedListBox1, checkBox2.Checked || comboBox1.SelectedIndex is not 0);
				SilverCBs.UpdateCallbacks(checkedListBox2, checkBox2.Checked || comboBox1.SelectedIndex is not 1);
				YellowCBs.UpdateCallbacks(checkedListBox3, checkBox2.Checked || comboBox1.SelectedIndex is not 2);
				switch (comboBox1.SelectedIndex)
				{
					case 0:
						{
							APIs.Gui.Text(5, 5, $"{GetEnemyMonName(CrystalSym)}'s Max HP: {CpuReadBigU16("wEnemyMonMaxHP", CrystalSym)}", Color.White, "topright");
							APIs.Gui.Text(5, 25, $"{GetEnemyMonName(CrystalSym)}'s Cur HP: {CpuReadBigU16("wEnemyMonHP", CrystalSym)}", Color.White, "topright");
							APIs.Gui.Text(5, 55, $"{GetEnemyMonName(CrystalSym)}'s Move: {GetEnemyMonMove(CrystalSym)}", Color.White, "topright");
							APIs.Gui.Text(5, 85, $"Crit Roll: {CrystalCBs.CritRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 105, $"Crit Chance: {CrystalCBs.CritRng.Chance}", Color.White, "topright");
							APIs.Gui.Text(5, 135, $"Damage Roll: {CrystalCBs.DamageRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 165, $"Accuracy Roll: {CrystalCBs.AccuracyRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 185, $"Move Accuracy: {CrystalCBs.AccuracyRng.Chance}", Color.White, "topright");
							APIs.Gui.Text(5, 215, $"Effect Roll: {CrystalCBs.EffectRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 235, $"Effect Chance: {CrystalCBs.EffectRng.Chance}", Color.White, "topright");
							APIs.Gui.Text(5, 265, $"Catch Roll: {CrystalCBs.CatchRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 285, $"Catch Chance: {CrystalCBs.CatchRng.Chance}", Color.White, "topright");
							APIs.Gui.Text(5, 315, $"Quick Claw Roll: {CrystalCBs.QuickClawRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 345, $"Random Sub: {CpuReadU8("hRandomSub", CrystalSym)}", Color.White, "topright");
							break;
						}
					case 1:
						{
							APIs.Gui.Text(5, 5, $"{GetEnemyMonName(SilverSym)}'s Max HP: {CpuReadBigU16("wEnemyMonMaxHP", SilverSym)}", Color.White, "topright");
							APIs.Gui.Text(5, 25, $"{GetEnemyMonName(SilverSym)}'s Cur HP: {CpuReadBigU16("wEnemyMonHP", SilverSym)}", Color.White, "topright");
							APIs.Gui.Text(5, 55, $"{GetEnemyMonName(SilverSym)}'s Move: {GetEnemyMonMove(SilverSym)}", Color.White, "topright");
							APIs.Gui.Text(5, 85, $"Crit Roll: {SilverCBs.CritRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 105, $"Crit Chance: {SilverCBs.CritRng.Chance}", Color.White, "topright");
							APIs.Gui.Text(5, 135, $"Damage Roll: {SilverCBs.DamageRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 165, $"Accuracy Roll: {SilverCBs.AccuracyRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 185, $"Move Accuracy: {SilverCBs.AccuracyRng.Chance}", Color.White, "topright");
							APIs.Gui.Text(5, 215, $"Effect Roll: {SilverCBs.EffectRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 235, $"Effect Chance: {SilverCBs.EffectRng.Chance}", Color.White, "topright");
							APIs.Gui.Text(5, 265, $"Catch Roll: {SilverCBs.CatchRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 285, $"Catch Chance: {SilverCBs.CatchRng.Chance}", Color.White, "topright");
							APIs.Gui.Text(5, 315, $"Quick Claw Roll: {SilverCBs.QuickClawRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 345, $"Random Sub: {CpuReadU8("hRandomSub", SilverSym)}", Color.White, "topright");
							break;
						}
					case 2:
						{
							APIs.Gui.Text(5, 5, $"Random Add: {CpuReadU8("hRandomAdd", YellowSym)}", Color.White, "bottomright");
							APIs.Gui.Text(5, 35, $"DSUM: {GetDSUM(YellowSym)}", Color.White, "bottomright");
							APIs.Gui.Text(5, 5, $"Crit Roll: {YellowCBs.CritRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 25, $"Crit Chance: {YellowCBs.CritRng.Chance}", Color.White, "topright");
							APIs.Gui.Text(5, 55, $"Damage Roll: {YellowCBs.DamageRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 85, $"Accuracy Roll: {YellowCBs.AccuracyRng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 105, $"Move Accuracy: {YellowCBs.AccuracyRng.Chance}", Color.White, "topright");
							APIs.Gui.Text(5, 135, $"Enemy Move: {GetEnemyMonMove(YellowSym)}", Color.White, "topright");
							APIs.Gui.Text(5, 165, $"1st Catch Roll: {YellowCBs.Catch1Rng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 185, $"1st Catch Chance: {YellowCBs.Catch1Rng.Chance}", Color.White, "topright");
							APIs.Gui.Text(5, 215, $"2nd Catch Roll: {YellowCBs.Catch2Rng.Roll}", Color.White, "topright");
							APIs.Gui.Text(5, 235, $"2nd Catch Chance: {YellowCBs.Catch2Rng.Chance}", Color.White, "topright");
							break;
						}
					default:
						throw new Exception();
				}
			}
		}

		private void ShowMessage(string message) => DialogController.ShowMessageBox(message);

		private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			try
			{
				linkLabel1.LinkVisited = true;
				Process.Start("https://github.com/CasualPokePlayer/Gen2TASTool");
			}
			catch (Exception ex)
			{
				ShowMessage($"Caught {ex.GetType().FullName} while trying to open link to source code");
			}
		}


		[RequiredService]
		private IEmulator? Emulator { get; set; }

		private IEmulator Emu => Emulator ?? throw new NullReferenceException();

		private (int, int) GetLinkerNums()
		{
			using var state = new MemoryStream();
			Emu.AsStatable().SaveStateBinary(new BinaryWriter(state));
			state.Seek(-4, SeekOrigin.End);
			var shifted = state.ReadByte() != 0;
			state.ReadByte(); // signal
			var spaced = state.ReadByte() != 0;
			if (spaced)
			{
				return (1, 3);
			}
			else if (shifted)
			{
				return (2, 3);
			}
			else
			{
				return (1, 2);
			}
		}
	}
}
