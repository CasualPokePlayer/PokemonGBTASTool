using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using BizHawk.Client.Common;

namespace Gen2TASTool
{
	public class SYM
	{
		public enum Gen2Game
		{
			Gold,
			Silver,
			Crystal
		}

		private static readonly int[] bankSizes = new int[16]
		{
			/*ROM*/ 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000, 0x4000,
			/*VRAM*/ 0x2000, 0x2000,
			/*SRAM*/ 0x2000, 0x2000,
			/*WRAM*/ 0, 0x1000,
			/*Echo-OAM-HRAM*/ 0, 0,
		};

		private static readonly int[] domOffsets = new int[16]
		{
			/*ROM*/ 0, 0, 0, 0, 0x4000, 0x4000, 0x4000, 0x4000,
			/*VRAM*/ 0x8000, 0x8000,
			/*SRAM*/ 0xA000, 0xA000,
			/*WRAM*/ 0xC000, 0xD000,
			/*Echo-OAM-HRAM*/ 0, 0xFF80,
		};

		private static readonly string[] domains = new string[16]
		{
			/*ROM*/ "ROM", "ROM", "ROM", "ROM", "ROM", "ROM", "ROM", "ROM",
			/*VRAM*/ "VRAM", "VRAM",
			/*SRAM*/ "SRAM", "SRAM",
			/*WRAM*/ "WRAM", "WRAM",
			/*Echo-OAM-HRAM*/ "", "HRAM",
		};

		private class SYMEntry
		{
			public int Bank;
			public int SystemBusAddress;
			public string Domain;
			public int DomainAddress;

			public SYMEntry(string line)
			{
				Bank = int.Parse(line.Substring(0, 2), NumberStyles.HexNumber);
				SystemBusAddress = int.Parse(line.Substring(3, 4), NumberStyles.HexNumber);
				var index = SystemBusAddress >> 12;
				Domain = domains[index];
				DomainAddress = SystemBusAddress + Bank * bankSizes[index] - domOffsets[index];
			}
		}

		private readonly Dictionary<string, SYMEntry> SymEntries = new();
		private IDialogController DialogController { get; }

		public SYM(Gen2Game game, IDialogController dialogController)
		{
			DialogController = dialogController;

			string file = game switch
			{
				Gen2Game.Gold => "Gen2TASTool.pokegold.sym",
				Gen2Game.Silver => "Gen2TASTool.pokesilver.sym",
				Gen2Game.Crystal => "Gen2TASTool.pokecrystal.sym",
				_ => throw new Exception()
			};

			var reader = new StreamReader(typeof(SYM).Assembly.GetManifestResourceStream(file));
			reader.ReadLine(); // skip first line
			while (true)
			{
				var line = reader.ReadLine();
				if (string.IsNullOrEmpty(line))
				{
					break;
				}
				SymEntries[line.Remove(0, 8)] = new SYMEntry(line);
			}
		}

		public int GetSYMBank(string symbol)
		{
			try
			{
				return SymEntries[symbol].Bank;
			}
			catch (Exception e)
			{
				DialogController.ShowMessageBox($"Caught {e.GetType().FullName} while getting bank for symbol {symbol}");
				return -1;
			}
		}

		public int GetSYMSysBusAddr(string symbol)
		{
			try
			{
				return SymEntries[symbol].SystemBusAddress;
			}
			catch (Exception e)
			{
				DialogController.ShowMessageBox($"Caught {e.GetType().FullName} while getting system bus address for symbol {symbol}");
				return -1;
			}
		}

		public string GetSYMDomain(string symbol)
		{
			try
			{
				return SymEntries[symbol].Domain;
			}
			catch (Exception e)
			{
				DialogController.ShowMessageBox($"Caught {e.GetType().FullName} while getting domain for symbol {symbol}");
				return "";
			}
		}

		public int GetSYMDomAddr(string symbol)
		{
			try
			{
				return SymEntries[symbol].DomainAddress;
			}
			catch (Exception e)
			{
				DialogController.ShowMessageBox($"Caught {e.GetType().FullName} while getting domain address for symbol {symbol}");
				return -1;
			}
		}
	}
}
