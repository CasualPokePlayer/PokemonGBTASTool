using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using BizHawk.Emulation.Common;

namespace PokemonGBTASTool;

[ExternalTool("Pokemon GB TAS Tool", Description = "A tool to help with TASing GB Pokemon games.")]
[ExternalToolApplicability.RomList(VSystemID.Raw.GB,
"EA9BCAE617FDF159B045185467AE58B2E4A48B9A", "D7037C83E1AE5B39BDE3C30787637BA1D4C48CE2", "CC7D03262EBFAF2F06772C1A480C7D9D5F4A38E1", // red, blue, yellow hashes
"D8B8A3600A465308C9953DFA04F0081C05BDCB94", "49B163F7E57702BC939D642A18F591DE55D92DAE", "F4CD194BDEE0D04CA4EAC29E09B8E4E9D818C133")] // gold, silver, crystal hashes
[ExternalToolEmbeddedIcon("PokemonGBTASTool.res.icon.ico")]
public partial class PokemonGBTASToolForm : ToolFormBase, IExternalToolForm
{
	// ReSharper disable once UnusedAutoPropertyAccessor.Global
	public ApiContainer? ApiContainer { get; set; }
	private ApiContainer APIs => ApiContainer!;

	private PokemonGame? PokemonGame { get; set; }
	private PokemonGame PkmnGame => PokemonGame!;

	protected override string WindowTitleStatic => "Pokemon GB TAS Tool";

	public PokemonGBTASToolForm()
	{
		InitializeComponent();
		using var iconStream = typeof(PokemonGBTASToolForm).Assembly.GetManifestResourceStream("PokemonGBTASTool.res.icon.ico")!;
		Icon = new(iconStream);
		Closing += (_, _) =>
		{
			PokemonGame?.CBs.UpdateCallbacks(checkedListBox1, true);
			APIs.EmuClient.SetGameExtraPadding(0);
		};
	}

	/// <remarks>This is called once when the form is opened, and every time a new movie session starts.</remarks>
	public override void Restart()
	{
		if (string.IsNullOrEmpty(APIs.Emulation.GetBoardName()))
		{
			ShowMessage("Only Gambatte is supported with this tool");
			Close();
		}

		PokemonGame = APIs.Emulation.GetGameInfo()?.Hash switch
		{
			"EA9BCAE617FDF159B045185467AE58B2E4A48B9A" => new PokemonRed(APIs, ShowMessage, () => checkBox1.Checked, ""),
			"D7037C83E1AE5B39BDE3C30787637BA1D4C48CE2" => new PokemonBlue(APIs, ShowMessage, () => checkBox1.Checked, ""),
			"CC7D03262EBFAF2F06772C1A480C7D9D5F4A38E1" => new PokemonYellow(APIs, ShowMessage, () => checkBox1.Checked, ""),
			"D8B8A3600A465308C9953DFA04F0081C05BDCB94" => new PokemonGold(APIs, ShowMessage, () => checkBox1.Checked, ""),
			"49B163F7E57702BC939D642A18F591DE55D92DAE" => new PokemonSilver(APIs, ShowMessage, () => checkBox1.Checked, ""),
			"F4CD194BDEE0D04CA4EAC29E09B8E4E9D818C133" => new PokemonCrystal(APIs, ShowMessage, () => checkBox1.Checked, ""),
			_ => throw new()
		};

		checkedListBox1.Items.Clear();
		checkedListBox1.Items.AddRange((PkmnGame.IsGen2 ? Gen2Callbacks.BreakpointList : Gen1Callbacks.BreakpointList).Cast<object>().ToArray());
		checkedListBox1.CheckOnClick = true;
		APIs.EmuClient.SetGameExtraPadding(0, 0, 105);
	}

	public override void UpdateValues(ToolFormUpdateType type)
	{
		if (type is ToolFormUpdateType.PreFrame or ToolFormUpdateType.FastPreFrame)
		{
			switch (PkmnGame.CBs)
			{
				case Gen2Callbacks gen2Cbs:
					gen2Cbs.UpdateCallbacks(checkedListBox1, checkBox2.Checked);
					APIs.Gui.Text(5, 5, $"{PkmnGame.GetEnemyMonName()}'s Max HP: {PkmnGame.ReadU16("wEnemyMonMaxHP")}", Color.White, "topright");
					APIs.Gui.Text(5, 25, $"{PkmnGame.GetEnemyMonName()}'s Cur HP: {PkmnGame.ReadU16("wEnemyMonHP")}", Color.White, "topright");
					APIs.Gui.Text(5, 55, $"{PkmnGame.GetEnemyMonName()}'s Move: {PkmnGame.GetEnemyMonMove()}", Color.White, "topright");
					APIs.Gui.Text(5, 85, $"Crit Roll: {gen2Cbs.CritRng.Roll}", Color.White, "topright");
					APIs.Gui.Text(5, 105, $"Crit Chance: {gen2Cbs.CritRng.Chance}", Color.White, "topright");
					APIs.Gui.Text(5, 135, $"Damage Roll: {gen2Cbs.DamageRng.Roll}", Color.White, "topright");
					APIs.Gui.Text(5, 165, $"Accuracy Roll: {gen2Cbs.AccuracyRng.Roll}", Color.White, "topright");
					APIs.Gui.Text(5, 185, $"Move Accuracy: {gen2Cbs.AccuracyRng.Chance}", Color.White, "topright");
					APIs.Gui.Text(5, 215, $"Effect Roll: {gen2Cbs.EffectRng.Roll}", Color.White, "topright");
					APIs.Gui.Text(5, 235, $"Effect Chance: {gen2Cbs.EffectRng.Chance}", Color.White, "topright");
					APIs.Gui.Text(5, 265, $"Catch Roll: {gen2Cbs.CatchRng.Roll}", Color.White, "topright");
					APIs.Gui.Text(5, 285, $"Catch Chance: {gen2Cbs.CatchRng.Chance}", Color.White, "topright");
					APIs.Gui.Text(5, 315, $"Random Sub: {PkmnGame.ReadU8("hRandomSub")}", Color.White, "topright");
					break;
				case Gen1Callbacks gen1Cbs:
					gen1Cbs.UpdateCallbacks(checkedListBox1, checkBox2.Checked);
					APIs.Gui.Text(5, 5, $"Random Add: {PkmnGame.ReadU8("hRandomAdd")}", Color.White, "bottomright");
					APIs.Gui.Text(5, 35, $"DSUM: {PkmnGame.GetDSUM()}", Color.White, "bottomright");
					APIs.Gui.Text(5, 5, $"Crit Roll: {gen1Cbs.CritRng.Roll}", Color.White, "topright");
					APIs.Gui.Text(5, 25, $"Crit Chance: {gen1Cbs.CritRng.Chance}", Color.White, "topright");
					APIs.Gui.Text(5, 55, $"Damage Roll: {gen1Cbs.DamageRng.Roll}", Color.White, "topright");
					APIs.Gui.Text(5, 85, $"Accuracy Roll: {gen1Cbs.AccuracyRng.Roll}", Color.White, "topright");
					APIs.Gui.Text(5, 105, $"Move Accuracy: {gen1Cbs.AccuracyRng.Chance}", Color.White, "topright");
					APIs.Gui.Text(5, 135, $"Enemy Move: {PkmnGame.GetEnemyMonMove()}", Color.White, "topright");
					APIs.Gui.Text(5, 165, $"1st Catch Roll: {gen1Cbs.Catch1Rng.Roll}", Color.White, "topright");
					APIs.Gui.Text(5, 185, $"1st Catch Chance: {gen1Cbs.Catch1Rng.Chance}", Color.White, "topright");
					APIs.Gui.Text(5, 215, $"2nd Catch Roll: {gen1Cbs.Catch2Rng.Roll}", Color.White, "topright");
					APIs.Gui.Text(5, 235, $"2nd Catch Chance: {gen1Cbs.Catch2Rng.Chance}", Color.White, "topright");
					break;
			}
		}
	}

	private void ShowMessage(string message)
		=> DialogController.ShowMessageBox(message);

	private void LinkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
	{
		try
		{
			linkLabel1.LinkVisited = true;
			Process.Start("https://github.com/CasualPokePlayer/PokemonGBTASTool");
		}
		catch (Exception ex)
		{
			ShowMessage($"Caught {ex.GetType().FullName} while trying to open link to source code");
		}
	}
}

