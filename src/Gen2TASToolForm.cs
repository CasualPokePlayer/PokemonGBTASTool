using System;

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

		private ApiContainer APIs => ApiContainer ?? throw new NullReferenceException();

		protected override string WindowTitleStatic => "Gen2TASTool";

		private SYM? GBSym;
		private bool BreakpointsActive => checkBox1.Checked;

		public Gen2TASToolForm()
		{
			InitializeComponent();
			string[] breakpoints =
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
			checkedListBox1.Items.AddRange(breakpoints);
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
			GBSym = new SYM(gen2Game, DialogController);
			APIs.EmuClient.SetGameExtraPadding(0, 0, 105, 0);
		}

		public override void UpdateValues(ToolFormUpdateType type)
		{

		}

	}
}
