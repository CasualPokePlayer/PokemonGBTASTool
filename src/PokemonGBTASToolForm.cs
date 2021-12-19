using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;

namespace PokemonGBTASTool
{
	[ExternalTool("Pokemon GB TAS Tool", Description = "A tool to help with TASing GB Pokemon games.")]
	[ExternalToolApplicability.RomWhitelist(CoreSystem.GameBoy,
	"EA9BCAE617FDF159B045185467AE58B2E4A48B9A", "D7037C83E1AE5B39BDE3C30787637BA1D4C48CE2", "CC7D03262EBFAF2F06772C1A480C7D9D5F4A38E1", // red, blue, yellow hashes
	"D8B8A3600A465308C9953DFA04F0081C05BDCB94", "49B163F7E57702BC939D642A18F591DE55D92DAE", "F4CD194BDEE0D04CA4EAC29E09B8E4E9D818C133") // gold, silver, crystal hashes
	]
	[ExternalToolEmbeddedIcon("PokemonGBTASTool.res.icon.ico")]
	public partial class PokemonGBTASToolForm : ToolFormBase, IExternalToolForm
	{
		public ApiContainer? ApiContainer { get; set; }
		private ApiContainer APIs => ApiContainer ?? throw new NullReferenceException();

		private SYM? SYM { get; set; }
		private SYM GBSym => SYM ?? throw new NullReferenceException();

		private Callbacks? Callbacks { get; set; }

		private Callbacks CBs => Callbacks ?? throw new NullReferenceException();

		private PokemonData PkmnData { get; }

		public delegate void MessageCallback(string message);

		protected override string WindowTitleStatic => "Pokemon GB TAS Tool";

		public PokemonGBTASToolForm()
		{
			InitializeComponent();
			PkmnData = new PokemonData(ShowMessage);
			Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("PokemonGBTASTool.res.icon.ico"));
			Closing += (sender, args) => APIs.EmuClient.SetGameExtraPadding(0, 0, 0, 0);
		}

		/// <remarks>This is called once when the form is opened, and every time a new movie session starts.</remarks>
		public override void Restart()
		{
			if (string.IsNullOrEmpty(APIs.GameInfo.GetBoardType()))
			{
				ShowMessage("Only Gambatte is supported with this tool");
				Close();
			}
			SYM = APIs.GameInfo.GetGameInfo()?.Hash switch
			{
				"EA9BCAE617FDF159B045185467AE58B2E4A48B9A" => new RedSYM(ShowMessage, ""),
				"D7037C83E1AE5B39BDE3C30787637BA1D4C48CE2" => new BlueSYM(ShowMessage, ""),
				"CC7D03262EBFAF2F06772C1A480C7D9D5F4A38E1" => new YellowSYM(ShowMessage, ""),
				"D8B8A3600A465308C9953DFA04F0081C05BDCB94" => new GoldSYM(ShowMessage, ""),
				"49B163F7E57702BC939D642A18F591DE55D92DAE" => new SilverSYM(ShowMessage, ""),
				"F4CD194BDEE0D04CA4EAC29E09B8E4E9D818C133" => new CrystalSYM(ShowMessage, ""),
				_ => throw new Exception()
			};
			Callbacks = GBSym.IsGen2
				? new Gen2Callbacks(APIs, GBSym, () => checkBox1.Checked, "")
				: new Gen1Callbacks(APIs, GBSym, () => checkBox1.Checked, "");
			checkedListBox1.Items.Clear();
			checkedListBox1.Items.AddRange(GBSym.IsGen2 ? Gen2Callbacks.BreakpointList : Gen1Callbacks.BreakpointList);
			checkedListBox1.CheckOnClick = true;
			APIs.EmuClient.SetGameExtraPadding(0, 0, 105, 0);
		}

		private byte CpuReadU8(string symbol) => (byte)APIs.Memory.ReadU8(GBSym.GetSYMDomAddr(symbol), GBSym.GetSYMDomain(symbol));

		private ushort CpuReadBigU16(string symbol)
		{
			APIs.Memory.SetBigEndian();
			return (ushort)APIs.Memory.ReadU16(GBSym.GetSYMDomAddr(symbol), GBSym.GetSYMDomain(symbol));
		}

		private string GetEnemyMonName() => PkmnData.GetPokemonSpeciesName(CpuReadU8("wEnemyMonSpecies"), GBSym.IsGen2);
		private string GetEnemyMonMove() => PkmnData.GetPokemonMoveName(CpuReadU8(GBSym.IsGen2 ? "wCurEnemyMove" : "wEnemySelectedMove"));
		private byte GetDSUM() => (byte)((CpuReadU8("hRandomAdd") + CpuReadU8("hRandomSub")) & 0xFF);

		public override void UpdateValues(ToolFormUpdateType type)
		{
			if (CBs is Gen2Callbacks gen2Cbs)
			{
				gen2Cbs.UpdateCallbacks(checkedListBox1, checkBox2.Checked);
				APIs.Gui.Text(5, 5, $"{GetEnemyMonName()}'s Max HP: {CpuReadBigU16("wEnemyMonMaxHP")}", Color.White, "topright");
				APIs.Gui.Text(5, 25, $"{GetEnemyMonName()}'s Cur HP: {CpuReadBigU16("wEnemyMonHP")}", Color.White, "topright");
				APIs.Gui.Text(5, 55, $"{GetEnemyMonName()}'s Move: {GetEnemyMonMove()}", Color.White, "topright");
				APIs.Gui.Text(5, 85, $"Crit Roll: {gen2Cbs.CritRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 105, $"Crit Chance: {gen2Cbs.CritRng.Chance}", Color.White, "topright");
				APIs.Gui.Text(5, 135, $"Damage Roll: {gen2Cbs.DamageRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 165, $"Accuracy Roll: {gen2Cbs.AccuracyRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 185, $"Move Accuracy: {gen2Cbs.AccuracyRng.Chance}", Color.White, "topright");
				APIs.Gui.Text(5, 215, $"Effect Roll: {gen2Cbs.EffectRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 235, $"Effect Chance: {gen2Cbs.EffectRng.Chance}", Color.White, "topright");
				APIs.Gui.Text(5, 265, $"Catch Roll: {gen2Cbs.CatchRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 285, $"Catch Chance: {gen2Cbs.CatchRng.Chance}", Color.White, "topright");
				APIs.Gui.Text(5, 315, $"Random Sub: {CpuReadU8("hRandomSub")}", Color.White, "topright");
			}
			else if (CBs is Gen1Callbacks gen1Cbs)
			{
				APIs.Gui.Text(5, 5, $"Random Add: {CpuReadU8("hRandomAdd")}", Color.White, "bottomright");
				APIs.Gui.Text(5, 35, $"DSUM: {GetDSUM()}", Color.White, "bottomright");
				APIs.Gui.Text(5, 5, $"Crit Roll: {gen1Cbs.CritRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 25, $"Crit Chance: {gen1Cbs.CritRng.Chance}", Color.White, "topright");
				APIs.Gui.Text(5, 55, $"Damage Roll: {gen1Cbs.DamageRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 85, $"Accuracy Roll: {gen1Cbs.AccuracyRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 105, $"Move Accuracy: {gen1Cbs.AccuracyRng.Chance}", Color.White, "topright");
				APIs.Gui.Text(5, 135, $"Enemy Move: {GetEnemyMonMove()}", Color.White, "topright");
				APIs.Gui.Text(5, 165, $"1st Catch Roll: {gen1Cbs.Catch1Rng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 185, $"1st Catch Chance: {gen1Cbs.Catch1Rng.Chance}", Color.White, "topright");
				APIs.Gui.Text(5, 215, $"2nd Catch Roll: {gen1Cbs.Catch2Rng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 235, $"2nd Catch Chance: {gen1Cbs.Catch2Rng.Chance}", Color.White, "topright");
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
	}
}
