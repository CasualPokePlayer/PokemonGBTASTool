using System;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;

namespace Gen2TASTool
{
	[ExternalTool("Gen2TASTool", Description = "A tool to help with TASing Gen 2 Pokemon games.")]
	[ExternalToolApplicability.RomWhitelist
	(CoreSystem.GameBoy, "D8B8A3600A465308C9953DFA04F0081C05BDCB94", "49B163F7E57702BC939D642A18F591DE55D92DAE", "F4CD194BDEE0D04CA4EAC29E09B8E4E9D818C133")
	] // gold, silver, crystal hashes
	public partial class Gen2TASToolForm : ToolFormBase, IExternalToolForm
	{
		public ApiContainer? ApiContainer { get; set; }

		public delegate void MessageCallback(string message);

		private ApiContainer APIs => ApiContainer ?? throw new NullReferenceException();

		protected override string WindowTitleStatic => "Gen2TASTool";

		private SYM? GBSym;
		private Callbacks? CBs;
		private readonly PokemonData PkmnData;

		public Gen2TASToolForm()
		{
			InitializeComponent();
			checkedListBox1.Items.AddRange(Callbacks.BreakpointList);
			checkedListBox1.CheckOnClick = true;
			PkmnData = new PokemonData(ShowMessage);
			Closing += (sender, args) => APIs.EmuClient.SetGameExtraPadding(0, 0, 0, 0);
		}

		/// <remarks>This is called once when the form is opened, and every time a new movie session starts.</remarks>
		public override void Restart()
		{
			SYM.Gen2Game gen2Game = APIs.GameInfo.GetGameInfo()?.Hash switch
			{
				"D8B8A3600A465308C9953DFA04F0081C05BDCB94" => SYM.Gen2Game.Gold,
				"49B163F7E57702BC939D642A18F591DE55D92DAE" => SYM.Gen2Game.Silver,
				"F4CD194BDEE0D04CA4EAC29E09B8E4E9D818C133" => SYM.Gen2Game.Crystal,
				_ => throw new Exception()
			};
			GBSym = new SYM(gen2Game, ShowMessage);
			APIs.EmuClient.SetGameExtraPadding(0, 0, 105, 0);
			CBs = new Callbacks(APIs, GBSym, () => checkBox1.Checked);
		}

		private byte CpuReadU8(string symbol)
		{
			if (GBSym is null)
			{
				ShowMessage($"GBSym is null at {nameof(CpuReadU8)}??");
				return 0;
			}
			else
			{
				return (byte)APIs.Memory.ReadU8(GBSym.GetSYMDomAddr(symbol), GBSym.GetSYMDomain(symbol));
			}
		}

		private ushort CpuReadBigU16(string symbol)
		{
			if (GBSym is null)
			{
				ShowMessage($"GBSym is null at {nameof(CpuReadBigU16)}??");
				return 0;
			}
			else
			{
				APIs.Memory.SetBigEndian();
				return (ushort)APIs.Memory.ReadU16(GBSym.GetSYMDomAddr(symbol), GBSym.GetSYMDomain(symbol));
			}
		}

		private string GetEnemyMonName() => PkmnData.GetPokemonSpeciesName(CpuReadU8("wEnemyMonSpecies"));
		private string GetEnemyMonMove() => PkmnData.GetPokemonMoveName(CpuReadU8("wCurEnemyMove"));

		public override void UpdateValues(ToolFormUpdateType type)
		{
			if (CBs != null && GBSym != null)
			{
				CBs.UpdateCallbacks(checkedListBox1);
				APIs.Gui.Text(5, 5, $"{GetEnemyMonName()}'s Max HP: {CpuReadBigU16("wEnemyMonMaxHP")}", Color.White, "topright");
				APIs.Gui.Text(5, 25, $"{GetEnemyMonName()}'s Cur HP: {CpuReadBigU16("wEnemyMonHP")}", Color.White, "topright");
				APIs.Gui.Text(5, 55, $"{GetEnemyMonName()}'s Move: {GetEnemyMonMove()}", Color.White, "topright");
				APIs.Gui.Text(5, 85, $"Crit Roll: {CBs.CritRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 105, $"Crit Chance: {CBs.CritRng.Chance}", Color.White, "topright");
				APIs.Gui.Text(5, 135, $"Damage Roll: {CBs.DamageRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 165, $"Accuracy Roll: {CBs.AccuracyRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 185, $"Move Accuracy: {CBs.AccuracyRng.Chance}", Color.White, "topright");
				APIs.Gui.Text(5, 215, $"Effect Roll: {CBs.EffectRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 235, $"Effect Chance: {CBs.EffectRng.Chance}", Color.White, "topright");
				APIs.Gui.Text(5, 265, $"Catch Roll: {CBs.CatchRng.Roll}", Color.White, "topright");
				APIs.Gui.Text(5, 285, $"Catch Chance: {CBs.CatchRng.Chance}", Color.White, "topright");
				APIs.Gui.Text(5, 315, $"Random Sub: {CpuReadU8("hRandomSub")}", Color.White, "topright");
			}
		}

		private void ShowMessage(string message) => DialogController.ShowMessageBox(message);
	}
}
